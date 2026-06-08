using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Data;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
    public class VehicleOutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;
        public VehicleOutController(ApplicationDbContext context, UserLog userLog)
        {
            _context = context; 
            _userLog = userLog;
            _userId = UserHelper.GetCurrentUserId();
        }
        public IActionResult Index()
        {
            var list = _context.Database.SqlQuery<VehicleOutView> (
                $"Select vo.Id as id, vo.DateAndTime, vo.status as Status , e.Name as EmployeeName , v.VehicleNo as VehicleNo From VehicleOut vo left join Employee e on vo.employeeId = e.ID left join Vehicle v  on vo.vehicleId = v.ID Where vo.IsDelete =0 ORDER BY vo.Id DESC"
                ).ToList();
            //var list = _context.VehicleOut
            //    .Where(x => x.IsDelete == 0)
            //    .OrderByDescending(x => x.Id)
            //    .Select(vo => new VehicleOutVM
            //    {
            //        vehicleOut = vo,
            //        employee = _context.Employee.FirstOrDefault(e => e.Id == vo.employeeId) ?? new Employee(),
            //        vehicle = _context.Vehicle.FirstOrDefault(v => v.Id == vo.vehicleId) ?? new Vehicle()
            //    })
            //    .ToList();

            return View(list);
        }
        public IActionResult Create()
        {
            var vm = new VehicleOutVM
            {
                vehicleOut = new VehicleOut { DateAndTime = AppDate.Now },
                EmployeeList = _context.Employee.Where(e => e.IsDelete == 0 && e.status == false).ToList(),
                VehicleList = _context.Vehicle.Where(v => v.IsDelete == 0 && v.status == false).ToList()
            };
            // No need to create a separate VehicleOut instance here; it will be created in Save action.
            return View(vm);
        }

        [HttpPost]
        public IActionResult Save(VehicleOutVM vm)
        {
            try
            {
                var vehicleOut = new VehicleOut
                {
                    DateAndTime = vm.vehicleOut.DateAndTime,
                    employeeId = vm.vehicleOut.employeeId,
                    vehicleId = vm.vehicleOut.vehicleId,
                    status = true
                };
                _context.VehicleOut.Add(vehicleOut);
                _context.SaveChanges();
                TempData["save"] = "Vehicle Out saved successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Create");
            }
        }
        [HttpGet]
        public IActionResult Print(int id)
        {
            var vehicleOut = _context.VehicleOut.Find(id);
            if (vehicleOut == null)
            {
                return NotFound();
            }

            var vm = new VehicleOutVM
            {
                vehicleOut = vehicleOut,
                employee = _context.Employee.Find(vehicleOut.employeeId) ?? new Employee(),
                vehicle = _context.Vehicle.Find(vehicleOut.vehicleId)??new Vehicle(),
                setting = _context.Setting.FirstOrDefault()??new Setting()
            };

            // Query suppliers belonging to the areas of this employee using raw SQL
            var suppliers = _context.Supplier
                .FromSqlInterpolated($@"
                    SELECT s.*
                    FROM Supplier AS s 
                    INNER JOIN EmployeeArea AS ea ON s.AreaId = ea.AreaId
                    WHERE ea.EmployeeId = {vehicleOut.employeeId} AND s.IsDelete = 0 AND ea.IsDelete = 0")
                .ToList();  

            vm.Items = new List<VehicleOutItemVM>();
            foreach (var supplier in suppliers)
            {
                vm.Items.Add(new VehicleOutItemVM
                {
                    SupplierName = supplier.Name ?? "N/A",
                    ProductName = "",
                    Kg = 0
                });
            }

            return View(vm);
        }



        public string GenrateCodeIdVehicle()
        {
            string currentYear = AppDate.Now.ToString("yy");
            var Code = _context.Customer.Where(x => x.CodeId != null).OrderByDescending(x => x.Id).Select(x => x.CodeId).FirstOrDefault();

            if (string.IsNullOrEmpty(Code)) return $"PR-{currentYear}01";

            string lastpart = Code.Substring(5);
            int number = int.Parse(lastpart);
            int newNumber = number + 1;
            return $"VH-{currentYear}{newNumber}";

        }
        // New action to save and then open print view in a new tab
        [HttpPost]
        public IActionResult PrintAndSave(VehicleOutVM vm)
        {
            try
            {
                if (vm.vehicleOut.Id == 0) // Create New
                {
                    var vehicleOut = new VehicleOut
                    {
                        DateAndTime = vm.vehicleOut.DateAndTime,
                        employeeId = vm.vehicleOut.employeeId,
                        vehicleId = vm.vehicleOut.vehicleId,
                        CodeId = GenrateCodeIdVehicle(),
                        status = true,
                        CreatedAt = AppDate.Now,
                        CreatedBy = _userId
                    };

                    _context.VehicleOut.Add(vehicleOut);
                    var employee = _context.Employee.Find(vm.vehicleOut.employeeId);
                    if (employee != null)
                    {
                        employee.status = true;
                        _context.Employee.Update(employee);
                    }
                    var vehicle = _context.Vehicle.Find(vm.vehicleOut.vehicleId);
                    if (vehicle != null)
                    {
                        vehicle.status = true;
                        _context.Vehicle.Update(vehicle);
                    }
                    _context.SaveChanges();

                    TempData["save"] = "Vehicle Out saved successfully";
                    TempData["PrintUrl"] = Url.Action("Print", new { id = vehicleOut.Id });
                }
                else // Edit Existing
                {
                    var db = _context.VehicleOut.Find(vm.vehicleOut.Id);
                    if (db == null)
                    {
                        return NotFound();
                    }

                    // Check and update Employee status
                    if (db.employeeId != vm.vehicleOut.employeeId)
                    {
                        var oldEmployee = _context.Employee.Find(db.employeeId);
                        if (oldEmployee != null)
                        {
                            oldEmployee.status = false;
                            _context.Employee.Update(oldEmployee);
                        }
                        var newEmployee = _context.Employee.Find(vm.vehicleOut.employeeId);
                        if (newEmployee != null)
                        {
                            newEmployee.status = true;
                            _context.Employee.Update(newEmployee);
                        }
                    }

                    // Check and update Vehicle status
                    if (db.vehicleId != vm.vehicleOut.vehicleId)
                    {
                        var oldVehicle = _context.Vehicle.Find(db.vehicleId);
                        if (oldVehicle != null)
                        {
                            oldVehicle.status = false;
                            _context.Vehicle.Update(oldVehicle);
                        }
                        var newVehicle = _context.Vehicle.Find(vm.vehicleOut.vehicleId);
                        if (newVehicle != null)
                        {
                            newVehicle.status = true;
                            _context.Vehicle.Update(newVehicle);
                        }
                    }

                    db.DateAndTime = vm.vehicleOut.DateAndTime;
                    db.employeeId = vm.vehicleOut.employeeId;
                    db.vehicleId = vm.vehicleOut.vehicleId;
                    db.UpdatedBy = UserHelper.GetCurrentUserId();
                    db.UpdatedAt = AppDate.Now;

                    _context.VehicleOut.Update(db);
                    _context.SaveChanges();

                    TempData["save"] = "Vehicle Out updated successfully";
                    TempData["PrintUrl"] = Url.Action("Print", new { id = db.Id });
                }

                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Create");
            }
        }

        public IActionResult Edit(int id)
        {
            var vehicleOut = _context.VehicleOut.Find(id);
            if (vehicleOut == null)
            {
                return NotFound();
            }

            var employeeList = _context.Employee
                .Where(e => e.IsDelete == 0 && (e.status == false || e.Id == vehicleOut.employeeId))
                .ToList();

            var vehicleList = _context.Vehicle
                .Where(v => v.IsDelete == 0 && (v.status == false || v.Id == vehicleOut.vehicleId))
                .ToList();

            var vm = new VehicleOutVM
            {
                vehicleOut = vehicleOut,
                EmployeeList = employeeList,
                VehicleList = vehicleList
            };

            return View("Create", vm);
        }

        public IActionResult Delete(int id)
        {
            try
            {
                var db = _context.VehicleOut.Find(id);
                if (db == null)
                {
                    return NotFound();
                }

                var employee = _context.Employee.Find(db.employeeId);
                if (employee != null)
                {
                    employee.status = false;
                    _context.Employee.Update(employee);
                }

                var vehicle = _context.Vehicle.Find(db.vehicleId);
                if (vehicle != null)
                {
                    vehicle.status = false;
                    _context.Vehicle.Update(vehicle);
                }

                db.IsDelete = 1;
                _context.VehicleOut.Update(db);
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

    }
}
