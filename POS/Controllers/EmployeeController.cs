using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;
        public EmployeeController(ApplicationDbContext context, UserLog userLog)
        {
            _userId = UserHelper.GetCurrentUserId();
            _context = context;
            _userLog = userLog;
        }
        public IActionResult Index()
        {
            var list = _context.Employee.ToList();
            return View(list);
        }
        public IActionResult Create()
        {
            var employee = new Employee();
            employee.JoiningDate = AppDate.Now;
            employee.DateOfBirth = AppDate.Now;
            var vm = new EmployeeVM
            {
                employee = employee,
                arealist = _context.Area.Where(x => x.IsDelete == 0).ToList(),
                SelectedAreaIds = new List<int>()
            };
            return View(vm);
        }

        public string GenrateCodeId()
        {
            string currentYear = AppDate.Now.ToString("yy");
            var Code = _context.Customer.Where(x => x.CodeId != null).OrderByDescending(x => x.Id).Select(x => x.CodeId).FirstOrDefault();

            if (string.IsNullOrEmpty(Code)) return $"PR-{currentYear}01";

            string lastpart = Code.Substring(5);
            int number = int.Parse(lastpart);
            int newNumber = number + 1;
            return $"EP-{currentYear}{newNumber}";

        }
        public string GenrateCodeIdEmployeeArea()
        {
            string currentYear = AppDate.Now.ToString("yy");
            var Code = _context.Customer.Where(x => x.CodeId != null).OrderByDescending(x => x.Id).Select(x => x.CodeId).FirstOrDefault();

            if (string.IsNullOrEmpty(Code)) return $"PR-{currentYear}01";

            string lastpart = Code.Substring(5);
            int number = int.Parse(lastpart);
            int newNumber = number + 1;
            return $"EPAR-{currentYear}{newNumber}";

        }

        [HttpPost]
        public IActionResult Save(EmployeeVM vm)
        {
            try
            {
                string ms = "";
                if (vm.employee.Id == 0)
                {
                    vm.employee.IsDelete = 0;
                    vm.employee.CreatedBy = _userId;
                    vm.employee.CreatedAt = AppDate.Now;
                    vm.employee.CodeId = GenrateCodeId();
                    _context.Employee.Add(vm.employee);
                    _context.SaveChanges();

                    // FOREACH loop to save each selected area
                    if (vm.SelectedAreaIds != null && vm.SelectedAreaIds.Any())
                    {
                        foreach (var areaId in vm.SelectedAreaIds)
                        {
                            var employeeArea = new EmployeeArea
                            {
                                EmployeeId = vm.employee.Id,
                                AreaId = areaId
                            };
                            _context.EmployeeArea.Add(employeeArea);
                            _context.SaveChanges();
                        }
                        //_context.SaveChanges();
                        ms = "Data Saved Successfully";
                    }

                }
                else
                {
                    var db = _context.Employee.Find(vm.employee.Id);
                    if (db != null)
                    {
                        db.Name = vm.employee.Name;
                        db.UrduName = vm.employee.UrduName;
                        db.Salary = vm.employee.Salary;
                        db.DateOfBirth = vm.employee.DateOfBirth;
                        db.JoiningDate = vm.employee.JoiningDate;
                        db.UpdatedBy = UserHelper.GetCurrentUserId();
                        db.UpdatedAt = AppDate.Now;
                    }

                    var oldrecord = _context.EmployeeArea.Where(x => x.EmployeeId == vm.employee.Id).ToList();
                    _context.EmployeeArea.RemoveRange(oldrecord);

                    if (vm.SelectedAreaIds != null && vm.SelectedAreaIds.Any())
                    {
                        foreach (var areaId in vm.SelectedAreaIds)
                        {
                            var employeeArea = new EmployeeArea
                            {
                                EmployeeId = vm.employee.Id,
                                AreaId = areaId
                            };
                            _context.EmployeeArea.Add(employeeArea);
                            _context.SaveChanges();
                        }
                        ms = "Data Updated Successfully";
                    }

                }

                string detail, Action;
                if (vm.employee.Id == 0)
                {
                    detail = $"ID:{vm.employee.Id},  Name:{vm.employee.Name} ,  Date Of Birth:{vm.employee.DateOfBirth},  Joining Date:{vm.employee.JoiningDate},  Urdu Name:{vm.employee.UrduName},  Salary:{vm.employee.Salary},  Area:{vm.employeeArea?.AreaId}";
                    Action = "New";

                }
                else
                {
                    detail = $"ID:{vm.employee.Id},  Name:{vm.employee.Name} ,  Date Of Birth:{vm.employee.DateOfBirth},  Joining Date:{vm.employee.JoiningDate},  Urdu Name:{vm.employee.UrduName},  Salary:{vm.employee.Salary},  Area:{vm.employeeArea?.AreaId}";
                    Action = "Edit";
                }
                TempData["save"] = ms;
                _context.SaveChanges();
                _userLog.SaveHistory("Customer", Action, detail);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Create");
            }

        }


        public IActionResult Edit(int id)
        {
            // Get employee with their selected areas
            var employee = _context.Employee.Find(id);

            // Get current area IDs for this employee
            var existingAreaIds = _context.EmployeeArea
                .Where(x => x.EmployeeId == id)
                .Select(x => x.AreaId)
                .ToList();

            var vm = new EmployeeVM
            {
                employee = employee,
                arealist = _context.Area.Where(x => x.IsDelete == 0).ToList(),
                SelectedAreaIds = existingAreaIds  // Pre-select these in dropdown
            };

            return View("Create", vm);
        }

        public IActionResult Delete(int id)
        {
            try
            {
                // 1. Find the employee
                var db = _context.Employee.Find(id);

                if (db == null) return NotFound();


                var employeeAreas = _context.EmployeeArea
                    .Where(x => x.EmployeeId == id)
                    .ToList();

                foreach (var area in employeeAreas)
                {
                    area.IsDelete = 1;
                }
                string detail = $"ID:{db.Id},  Name:{db.Name} ,  Date Of Birth:{db.DateOfBirth},  Joining Date:{db.JoiningDate},  Urdu Name:{db.UrduName},  Salary:{db.Salary},  Area:{employeeAreas}";


                db.IsDelete = 1;


                // 4. Save changes
                _context.SaveChanges();
                _userLog.SaveHistory("Customer", "Delete", detail);

                TempData["save"] = "Employee deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}