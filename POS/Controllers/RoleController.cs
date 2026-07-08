using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Service;
using POS.Models;

namespace POS.Controllers
{
    public class RoleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;

        public RoleController(ApplicationDbContext context, UserLog userLog)
        {
            _context = context;
            _userLog = userLog;
            _userId = UserHelper.GetCurrentUserId();
        }

        // GET: Role (list)
        public IActionResult Index()
        {
            var list = _context.Role.ToList();
            return View(list);
        }

        // GET: Role/Create (adds new role)
        public IActionResult Create()
        {
            return View(new Role());
        }

        // POST: Role/Save (handles both create and edit)
        [HttpPost]
        public IActionResult Save(Role role)
        {
            if (ModelState.IsValid)
            {
                if (role.Id > 0)
                {
                    var existing = _context.Role.Find(role.Id);
                    if (existing == null) return NotFound();
                    existing.Name = role.Name;
                    existing.UpdatedAt = AppDate.Now;
                    existing.UpdatedBy = _userId;
                    _userLog.SaveHistory("Role", "Edit", $"ID:{role.Id}, Name:{role.Name}");
                    TempData["save"] = "Role updated successfully";
                }
                else
                {
                    role.CreatedAt = AppDate.Now;
                    role.CreatedBy = _userId;
                    _context.Role.Add(role);
                    _userLog.SaveHistory("Role", "New", $"Name:{role.Name}");
                    TempData["save"] = "Role created successfully";
                }
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            // Return to same view (Create) with validation errors
            return View("Create", role);
        }

        // GET: Role/Edit/5 (reuse Create view for editing)
        public IActionResult Edit(int id)
        {
            var role = _context.Role.Find(id);
            if (role == null) return NotFound();
            return View("Create", role);
        }

        // GET: Role/Details/5
        public IActionResult Details(int id)
        {
            var role = _context.Role.Find(id);
            if (role == null) return NotFound();
            return View(role);
        }

        // GET: Role/Delete/5 (hidden action)
        public IActionResult Delete(int id)
        {
            var role = _context.Role.Find(id);
            if (role == null) return NotFound();
            _context.Role.Remove(role);
            _context.SaveChanges();
            _userLog.SaveHistory("Role", "Delete", $"ID:{role.Id}, Name:{role.Name}");
            TempData["save"] = "Role deleted";
            return RedirectToAction("Index");
        }
    }
}
