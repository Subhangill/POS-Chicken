using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Data;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
    public class VehicleInController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;

        public VehicleInController(ApplicationDbContext context, UserLog userLog)
        {
            _userId = UserHelper.GetCurrentUserId();
            _context = context;
            _userLog = userLog;
        }

        public IActionResult Index()
        {
            var list = _context.Database.SqlQuery<VehicleInView>(
                $"SELECT vin.Id as id, e.Name as EmployeeName, v.VehicleNo as VehicleName, vin.totalWeight as weightTotal, vin.date as date From VehicleIn as vin Left Join Employee as e On vin.employeeId = e.Id Left Join Vehicle as v On vin.vehicleId = v.Id Where vin.IsDelete = 0 ORDER BY vin.Id DESC"
            ).ToList();

            return View(list);
        }

        public IActionResult Create()
        {



            var list = _context.Database.SqlQuery<VehicleInCreateView>($"Select vo.Id as Id, vo.employeeId as EmployeeId, vo.vehicleId as VehicleId, ISNULL(e.Name, 'N/A') + '-' + ISNULL(v.VehicleNo, 'N/A') as Text From VehicleOut as vo Left join Employee as e on vo.employeeId = e.ID Left Join Vehicle as v on vo.vehicleId = v.Id Where vo.IsDelete = 0 and vo.Status = 1").ToList();
            var rawProducts = _context.Product.Where(c => c.CategoryId == 3 && c.IsDelete == 0).ToList();
            var finalProducts = _context.Product.Where(c => c.CategoryId == 4 && c.IsDelete == 0).ToList();
            var vm = new VehicleInVM
            {
                vehicleIn = new VehicleIn
                {
                    date = AppDate.Now
                },
                EmployeeVehicle = list,
                productList = rawProducts,
                RawProduct = rawProducts,
                FinalProduct = finalProducts,
                SupplierList = _context.Supplier.Where(s => s.IsDelete == 0).OrderBy(s => s.Name).ToList(),
                setting = _context.Setting.FirstOrDefault() ?? new Setting()
            };
            return View(vm);
        }

        [HttpGet]
        public IActionResult GetSuppliersByEmployee(int? employeeId, int? vehicleOutId)
        {
            int targetEmployeeId = 0;
            if (employeeId.HasValue && employeeId.Value > 0)
            {
                targetEmployeeId = employeeId.Value;
            }
            else if (vehicleOutId.HasValue && vehicleOutId.Value > 0)
            {
                var vehicleOut = _context.VehicleOut.Find(vehicleOutId.Value);
                if (vehicleOut != null)
                {
                    targetEmployeeId = vehicleOut.employeeId;
                }
            }

            if (targetEmployeeId == 0)
            {
                return Json(new List<object>());
            }

            // Query suppliers belonging to the areas of this employee using raw SQL exactly like requested
            var suppliers = _context.Supplier
                .FromSqlInterpolated($@"
                    SELECT s.*
                    FROM Supplier AS s 
                    INNER JOIN EmployeeArea AS ea ON s.AreaId = ea.AreaId
                    WHERE ea.EmployeeId = {targetEmployeeId} AND s.IsDelete = 0 AND ea.IsDelete = 0")
                .ToList()
                .Select(x => new { x.Id, x.Name })
                .ToList();

            return Json(suppliers);
        }

        [HttpPost]
        public IActionResult Save(VehicleInVM vm, string submitButton)
        {
            try
            {
                string ms = "";
                string action = "";
                string detailLog = "";
                int targetPrintId = 0;

                if (vm.vehicleIn.Id > 0)
                {
                    // EDIT EXISTING VEHICLE IN
                    var vehicleIn = _context.VehicleIn.Find(vm.vehicleIn.Id);
                    if (vehicleIn == null) return NotFound();

                    var existingDetails = _context.VehicleInDetail.Where(d => d.Id == vehicleIn.Id && d.IsDelete == 0).ToList();

                    var activeSubmittedDetails = vm.vehicleInDetailList?
                        .Where(x => x.Kg > 0 && x.productId > 0)
                        .ToList() ?? new List<VehicleInDetail>();

                    if (!activeSubmittedDetails.Any())
                    {
                        TempData["error"] = "Please select product and enter weight for at least one supplier.";
                        return RedirectToAction("Edit", new { id = vehicleIn.Id });
                    }

                    // Process active submitted details
                    foreach (var submittedDetail in activeSubmittedDetails)
                    {
                        var dbDetail = existingDetails.FirstOrDefault(d => d.Id == submittedDetail.Id)
                                       ?? existingDetails.FirstOrDefault(d => d.SupplierId == submittedDetail.SupplierId);

                        if (dbDetail != null)
                        {
                            // Update existing detail record
                            dbDetail.productId = submittedDetail.productId;
                            dbDetail.Kg = submittedDetail.Kg;
                            dbDetail.SupplierId = submittedDetail.SupplierId;
                            dbDetail.UpdatedAt = AppDate.Now;
                            dbDetail.UpdatedBy = UserHelper.GetCurrentUserId();
                            _context.VehicleInDetail.Update(dbDetail);
                        }
                        else
                        {
                            // Insert a new detail record (e.g. area supplier that was previously blank or new manual supplier)
                            var newDetail = new VehicleInDetail
                            {
                                CodeId = vehicleIn.CodeId,
                                productId = submittedDetail.productId,
                                Kg = submittedDetail.Kg,
                                SupplierId = submittedDetail.SupplierId,
                                IsDelete = 0,
                                CreatedAt = AppDate.Now,
                                CreatedBy = _userId
                            };
                            _context.VehicleInDetail.Add(newDetail);
                        }
                    }

                    // Soft-delete database details not included in the active submitted list
                    foreach (var existingDetail in existingDetails)
                    {
                        var stillExists = activeSubmittedDetails.Any(d => d.Id == existingDetail.Id || d.SupplierId == existingDetail.SupplierId);
                        if (!stillExists)
                        {
                            existingDetail.IsDelete = 1;
                            existingDetail.UpdatedAt = AppDate.Now;
                            existingDetail.UpdatedBy = UserHelper.GetCurrentUserId();
                            _context.VehicleInDetail.Update(existingDetail);
                        }
                    }

                    // Update header record
                    vehicleIn.date = vm.vehicleIn.date;
                    vehicleIn.totalWeight = activeSubmittedDetails.Sum(x => x.Kg);
                    vehicleIn.UpdatedAt = AppDate.Now;
                    vehicleIn.UpdatedBy = UserHelper.GetCurrentUserId();
                    _context.VehicleIn.Update(vehicleIn);

                    ms = "Data Updated Successfully";
                    action = "Edit";

                    List<string> logsList = activeSubmittedDetails.Select(d => $"SupplierID:{d.SupplierId}, ProductID:{d.productId}, Kg:{d.Kg}").ToList();
                    detailLog = $"ID:{vehicleIn.Id}, CodeId:{vehicleIn.CodeId}, EmployeeID:{vehicleIn.employeeId}, VehicleID:{vehicleIn.vehicleId}, Details:[{string.Join("; ", logsList)}]";
                    targetPrintId = vehicleIn.Id;
                }
                else
                {
                    // CREATE NEW VEHICLE IN
                    if (vm.vehicleInDetailList != null && vm.vehicleInDetailList.Any())
                    {
                        var activeDetails = vm.vehicleInDetailList
                            .Where(x => x.Kg > 0 && x.productId > 0)
                            .ToList();

                        if (!activeDetails.Any())
                        {
                            TempData["error"] = "Please select product and enter weight for at least one supplier.";
                            return RedirectToAction("Create");
                        }

                        string codeId = GenerateVehicleInCodeId();
                        decimal totalWeight = activeDetails.Sum(x => x.Kg);

                        var newVehicleIn = new VehicleIn
                        {
                            date = vm.vehicleIn.date,
                            employeeId = vm.vehicleIn.employeeId,
                            vehicleId = vm.vehicleIn.vehicleId,
                            totalWeight = totalWeight,
                            CodeId = codeId,
                            IsDelete = 0,
                            CreatedBy = _userId,
                            CreatedAt = AppDate.Now
                        };

                        _context.VehicleIn.Add(newVehicleIn);

                        List<string> logsList = new List<string>();

                        foreach (var detail in activeDetails)
                        {
                            var vehicleInDetail = new VehicleInDetail
                            {
                                productId = detail.productId,
                                Kg = detail.Kg,
                                SupplierId = detail.SupplierId,
                                CodeId = codeId,
                                IsDelete = 0,
                                CreatedBy = _userId,
                                CreatedAt = AppDate.Now
                            };

                            _context.VehicleInDetail.Add(vehicleInDetail);

                            logsList.Add($"SupplierID:{detail.SupplierId}, ProductID:{detail.productId}, Kg:{detail.Kg}");
                        }

                        // Mark Employee and Vehicle status as False (Available/Returned)
                        var employee = _context.Employee.Find(vm.vehicleIn.employeeId);
                        if (employee != null)
                        {
                            employee.status = false;
                            _context.Employee.Update(employee);
                        }

                        var vehicle = _context.Vehicle.Find(vm.vehicleIn.vehicleId);
                        if (vehicle != null)
                        {
                            vehicle.status = false;
                            _context.Vehicle.Update(vehicle);
                        }

                        // Mark matching VehicleOut status as False (Completed/Returned)
                        var vehicleOut = _context.VehicleOut
                            .FirstOrDefault(vo => vo.employeeId == vm.vehicleIn.employeeId &&
                                                  vo.vehicleId == vm.vehicleIn.vehicleId &&
                                                  vo.status == true &&
                                                  vo.IsDelete == 0);
                        if (vehicleOut != null)
                        {
                            vehicleOut.status = false;
                            _context.VehicleOut.Update(vehicleOut);
                        }

                        ms = "Data Saved Successfully";
                        action = "New";
                        detailLog = $"CodeId:{codeId}, EmployeeID:{vm.vehicleIn.employeeId}, VehicleID:{vm.vehicleIn.vehicleId}, Details:[{string.Join("; ", logsList)}]";

                        vm.vehicleIn = newVehicleIn; // Store reference to retrieve DB-generated ID after SaveChanges()
                    }
                    else
                    {
                        TempData["error"] = "No transaction details were submitted.";
                        return RedirectToAction("Create");
                    }
                }

                TempData["save"] = ms;
                _context.SaveChanges();

                if (vm.vehicleIn != null && vm.vehicleIn.Id > 0 && targetPrintId == 0)
                {
                    targetPrintId = vm.vehicleIn.Id;
                }

                _userLog.SaveHistory("VehicleIn", action, detailLog);

                if (submitButton == "Print" && targetPrintId > 0)
                {
                    TempData["PrintUrl"] = Url.Action("Print", new { id = targetPrintId });
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            if (submitButton == "Print")
            {
                return RedirectToAction("Create");
            }
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var vehicleIn = _context.VehicleIn.Find(id);
            if (vehicleIn == null) return NotFound();

            var savedDetails = _context.VehicleInDetail.Where(x => x.CodeId == vehicleIn.CodeId && x.IsDelete == 0).ToList();

            var areaSuppliers = _context.Supplier
                .FromSqlInterpolated($@"
                    SELECT s.*
                    FROM Supplier AS s 
                    INNER JOIN EmployeeArea AS ea ON s.AreaId = ea.AreaId
                    WHERE ea.EmployeeId = {vehicleIn.employeeId} AND s.IsDelete = 0 AND ea.IsDelete = 0")
                .ToList();

            var mergedDetails = new List<VehicleInDetail>();

            // First, add all area-based suppliers
            foreach (var supplier in areaSuppliers)
            {
                var existingDetail = savedDetails.FirstOrDefault(d => d.SupplierId == supplier.Id);
                if (existingDetail != null)
                {
                    mergedDetails.Add(existingDetail);
                }
                else
                {
                    // No saved detail, add a blank one
                    mergedDetails.Add(new VehicleInDetail
                    {
                        SupplierId = supplier.Id,
                        productId = 0,
                        Kg = 0,
                        CodeId = vehicleIn.CodeId
                    });
                }
            }

            // Then, add any manual saved details that are NOT in the area suppliers list
            foreach (var savedDetail in savedDetails)
            {
                if (!areaSuppliers.Any(s => s.Id == savedDetail.SupplierId))
                {
                    mergedDetails.Add(savedDetail);
                }
            }

            ViewBag.AreaSupplierIds = areaSuppliers.Select(s => s.Id).ToList();

            var employee = _context.Employee.Find(vehicleIn.employeeId) ?? new Employee();
            var vehicle = _context.Vehicle.Find(vehicleIn.vehicleId) ?? new Vehicle();

            //var activeVehicleOuts = new List<VehicleOutSelectVM>
            //{
            //    new VehicleOutSelectVM
            //    {
            //        Id = -1,
            //        EmployeeId = vehicleIn.employeeId,
            //        VehicleId = vehicleIn.vehicleId,
            //        Text = $"{employee.Name} - {vehicle.VehicleNo}"
            //    }
            //};

            var rawProducts = _context.Product.Where(c => c.CategoryId == 3 && c.IsDelete == 0).ToList();
            var finalProducts = _context.Product.Where(c => c.CategoryId == 4 && c.IsDelete == 0).ToList();

            var list = _context.Database.SqlQuery<VehicleInCreateView>($"Select vo.Id as Id, vo.employeeId as EmployeeId, vo.vehicleId as VehicleId, ISNULL(e.Name, 'N/A') + '-' + ISNULL(v.VehicleNo, 'N/A') as Text From VehicleOut as vo Left join Employee as e on vo.employeeId = e.ID Left Join Vehicle as v on vo.vehicleId = v.Id Where vo.IsDelete = 0 and vo.Status = 1").ToList();

            var hasCurrent = list.Any(x => x.EmployeeId == vehicleIn.employeeId && x.VehicleId == vehicleIn.vehicleId);
            if (!hasCurrent)
            {
                var currentVo = _context.VehicleOut
                    .FirstOrDefault(vo => vo.employeeId == vehicleIn.employeeId && vo.vehicleId == vehicleIn.vehicleId && vo.IsDelete == 0);

                list.Insert(0, new VehicleInCreateView
                {
                    Id = currentVo?.Id ?? -1,
                    EmployeeId = vehicleIn.employeeId,
                    VehicleId = vehicleIn.vehicleId,
                    Text = $"{employee.Name}-{vehicle.VehicleNo}"
                });
            }

            var vm = new VehicleInVM
            {
                vehicleIn = vehicleIn,
                vehicleInDetailList = mergedDetails,
                productList = rawProducts,
                RawProduct = rawProducts,
                FinalProduct = finalProducts,
                EmployeeVehicle = list,
                SupplierList = _context.Supplier.Where(s => s.IsDelete == 0).OrderBy(s => s.Name).ToList(),
                employee = employee,
                setting = _context.Setting.FirstOrDefault() ?? new Setting()
            };

            return View("Create", vm);
        }

        public IActionResult Delete(int id)
        {
            try
            {
                var vehicleIn = _context.VehicleIn.Find(id);
                if (vehicleIn != null)
                {
                    vehicleIn.IsDelete = 1;
                    _context.VehicleIn.Update(vehicleIn);

                    var details = _context.VehicleInDetail.Where(x => x.CodeId == vehicleIn.CodeId && x.IsDelete == 0).ToList();
                    foreach (var detail in details)
                    {
                        detail.IsDelete = 1;
                        _context.VehicleInDetail.Update(detail);
                    }

                    _context.SaveChanges();
                    TempData["save"] = "Data Deleted Successfully";

                    string logText = $"Deleted VehicleIn ID:{vehicleIn.Id}";
                    _userLog.SaveHistory("VehicleIn", "Delete", logText);
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        private string GenerateVehicleInCodeId()
        {
            string currentYear = AppDate.Now.ToString("yy");
            var Code = _context.VehicleIn
                .Where(x => x.CodeId != null)
                .OrderByDescending(x => x.Id)
                .Select(x => x.CodeId)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(Code)) return $"PR-{currentYear}01";

            string lastpart = Code.Substring(5);
            if (int.TryParse(lastpart, out int number))
            {
                int newNumber = number + 1;
                return $"PR-{currentYear}{newNumber:D2}";
            }
            return $"PR-{currentYear}01";
        }

        [HttpGet]
        public IActionResult Print(int id)
        {
            var vehicleIn = _context.VehicleIn.Find(id);
            if (vehicleIn == null)
            {
                return NotFound();
            }

            var vehicleOut = _context.VehicleOut
                .FirstOrDefault(vo => vo.employeeId == vehicleIn.employeeId &&
                                      vo.vehicleId == vehicleIn.vehicleId &&
                                      vo.IsDelete == 0) ?? new VehicleOut();

            var vm = new VehicleInVM
            {
                vehicleIn = vehicleIn,
                vehicleOut = vehicleOut,
                employee = _context.Employee.Find(vehicleIn.employeeId) ?? new Employee(),
                vehicle = _context.Vehicle.Find(vehicleIn.vehicleId) ?? new Vehicle(),
                setting = _context.Setting.FirstOrDefault() ?? new Setting()
            };

            var savedDetails = _context.VehicleInDetail.Where(pd => pd.CodeId == vehicleIn.CodeId && pd.IsDelete == 0).ToList();

            var areaSuppliers = _context.Supplier
                .FromSqlInterpolated($@"
                    SELECT s.*
                    FROM Supplier AS s 
                    INNER JOIN EmployeeArea AS ea ON s.AreaId = ea.AreaId
                    WHERE ea.EmployeeId = {vehicleIn.employeeId} AND s.IsDelete = 0 AND ea.IsDelete = 0")
                .ToList();

            var suppliers = new List<Supplier>(areaSuppliers);
            foreach (var detail in savedDetails)
            {
                if (!suppliers.Any(s => s.Id == detail.SupplierId))
                {
                    var manualSupplier = _context.Supplier.Find(detail.SupplierId);
                    if (manualSupplier != null)
                    {
                        suppliers.Add(manualSupplier);
                    }
                }
            }

            vm.Items = new List<VehicleOutItemVM>();
            foreach (var supplier in suppliers)
            {
                var pDetail = savedDetails.FirstOrDefault(pd => pd.SupplierId == supplier.Id);

                vm.Items.Add(new VehicleOutItemVM
                {
                    SupplierName = supplier.Name ?? "N/A",
                    ProductName = pDetail != null ? (_context.Product.Where(p => p.Id == pDetail.productId).Select(p => p.Name).FirstOrDefault() ?? "") : "",
                    Kg = pDetail?.Kg ?? 0
                });
            }

            return View(vm);
        }
    }
}
