using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
    public class UserManagementController : Controller
    {
        private readonly IInterface _repo;
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;
        private readonly IPasswordHasher<UserLogin> _passwordHasher;

        public UserManagementController(ApplicationDbContext context, IInterface @interface, UserLog userLog)
        {
            _context = context;
            _repo = @interface;
            _userLog = userLog;
            _userId = UserHelper.GetCurrentUserId();
            // Use default PasswordHasher implementation
            _passwordHasher = new PasswordHasher<UserLogin>();
        }

        public IActionResult Index()
        {
            var list = _repo.GetList<UserLogin>("Select ul.*,e.name as Empname,r.name as Rolename  FROM UserLogin as ul LEFT JOIN EMployee as e on e.id=ul.EmpId LEFT JOIN Role as r on r.id=ul.RoleId WHERE ul.IsDelete=0 ORDER BY ul.id desc").ToList();
            return View(list);
        }

        public IActionResult Create()
        {
            var vm = new UserVm
            {
                UserLogin = new UserLogin(),
                Rolelist = _context.Role.Where(e => e.IsDelete == 0).ToList(),
                Employeelist = _context.Employee.Where(e => e.IsDelete == 0).ToList()
            };
            return View("Create", vm);
        }

        [HttpPost]
        public IActionResult Save(UserVm vm)
        {
            var user = vm.UserLogin;
            if (user.Id > 0)
            {
                var existing = _context.UserLogin.Find(user.Id);
                if (existing == null) return NotFound();
                existing.Username = user.Username;
                existing.CodeId = "";
                existing.RoleId = user.RoleId;
                existing.RoleId = user.RoleId;
                existing.EmpId = user.EmpId;
                if (!string.IsNullOrWhiteSpace(user.Password))
                {
                    existing.Password = _passwordHasher.HashPassword(existing, user.Password);
                }
                existing.UpdatedAt = AppDate.Now;
                existing.UpdatedBy = _userId;

                var emp = _context.Employee.Find(existing.EmpId);
                var role = _context.Role.Find(existing.RoleId);
                string empName = emp?.Name ?? "";
                string roleName = role?.Name ?? "";
                _userLog.SaveHistory("UserLogin", "Edit", $"ID:{existing.Id}, Username:{existing.Username}, EmpId:{existing.EmpId}, EmpName:{empName}, RoleId:{existing.RoleId}, RoleName:{roleName}");
                TempData["save"] = "User updated successfully";
            }
            else
            {
                user.CreatedAt = AppDate.Now;
                user.CreatedBy = _userId;
                user.Password = _passwordHasher.HashPassword(user, user.Password);
                _context.UserLogin.Add(user);

                var emp = _context.Employee.Find(user.EmpId);
                var role = _context.Role.Find(user.RoleId);
                string empName = emp?.Name ?? "";
                string roleName = role?.Name ?? "";
                _userLog.SaveHistory("UserLogin", "New", $"Username:{user.Username}, EmpId:{user.EmpId}, EmpName:{empName}, RoleId:{user.RoleId}, RoleName:{roleName}");
                TempData["save"] = "User created successfully";
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var data = _context.UserLogin.Find(id);
            if (data == null) return NotFound();
           
            var vm = new UserVm
            {
                UserLogin = data,
                Rolelist = _context.Role.Where(e => e.IsDelete == 0).ToList(),
                Employeelist = _context.Employee.Where(e => e.IsDelete == 0).ToList()
            };
            return View("Create", vm);
        }

        public IActionResult Details(int id)
        {
            var user = _context.UserLogin.Find(id);
            if (user == null) return NotFound();
            return View(user);
        }

        public IActionResult Delete(int id)
        {
            var user = _context.UserLogin.Find(id);
            if (user == null) return NotFound();
            var emp = _context.Employee.Find(user.EmpId);
            var role = _context.Role.Find(user.RoleId);
            string empName = emp?.Name ?? "";
            string roleName = role?.Name ?? "";
            _context.UserLogin.Remove(user);
            _context.SaveChanges();
            _userLog.SaveHistory("UserLogin", "Delete", $"ID:{user.Id}, Username:{user.Username}, EmpId:{user.EmpId}, EmpName:{empName}, RoleId:{user.RoleId}, RoleName:{roleName}");
            TempData["save"] = "User deleted";
            return RedirectToAction("Index");
        }
    }
}
