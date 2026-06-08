using Humanizer;
using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;
        public SupplierController(ApplicationDbContext context, UserLog userLog)
        {
            _userId = UserHelper.GetCurrentUserId();
            _context = context;
            _userLog = userLog;
        }
        public IActionResult Index()
        {
            var list = _context.Supplier.Where(x=>x.IsDelete == 0).ToList();
            return View(list);
        }
        public IActionResult Create(Supplier supplier)
        {
            var vm = new SupplierVM
            {
                supplier = supplier,
                arealist = _context.Area.Where(x=>x.IsDelete == 0).ToList()
            };
           
            return View(vm);
        }
        public string GenrateCodeId()
        {
            string currentYear = AppDate.Now.ToString("yy");
            var Code = _context.Supplier.Where(x => x.CodeId != null).OrderByDescending(x => x.Id).Select(x => x.CodeId).FirstOrDefault();

            if (string.IsNullOrEmpty(Code)) return $"SP-{currentYear}01";

            string lastpart = Code.Substring(5);
            int number = int.Parse(lastpart);
            int newNumber = number + 1;
            return $"CU-{currentYear}{newNumber}";

        }


        [HttpPost]
        public IActionResult Save(Supplier supplier)
        {
            try
            {
                string ms = "";
                if (supplier.Id == 0)
                {
                    supplier.IsDelete = 0;
                    supplier.CodeId = GenrateCodeId();
                    supplier.CreatedBy = _userId;
                    supplier.CreatedAt = AppDate.Now;
                    ms = "Data Saved Successfully";
                    _context.Supplier.Add(supplier);
                    ms = "Data Saved Successfully";

                }
                else
                {
                    var db = _context.Supplier.Find(supplier.Id);
                    if (db != null)
                    {
                        db.Name = supplier.Name;
                        db.UrduName = supplier.UrduName;
                        db.Phone = supplier.Phone;
                        db.Email = supplier.Email;
                        
                        db.BusinessName = supplier.BusinessName;
                        db.UpdatedBy = UserHelper.GetCurrentUserId();
                        db.UpdatedAt = AppDate.Now;
                    }
                }
                string detail, Action;
                if (supplier.Id == 0)
                {
                    detail = $"ID:{supplier.Id},  Name:{supplier.Name} ,  Email:{supplier.Email},  Phone:{supplier.Phone},  Urdu Name:{supplier.UrduName},  Business Name:{supplier.BusinessName}";
                    Action = "New";

                }
                else
                {
                    detail = $"ID:{supplier.Id},  Name:{supplier.Name} ,  Email:{supplier.Email},  Phone:{supplier.Phone},  Urdu Name:{supplier.UrduName},  Business Name:{supplier.BusinessName}";
                    Action = "Edit";
                }
                TempData["save"] = ms;
                _context.SaveChanges();
                _userLog.SaveHistory("Supplier", Action, detail);

                return RedirectToAction("Create");
            }
            catch
            {
                return RedirectToAction("Create");
            }

        }
        public ActionResult Edit(int id)
        {
            var db = _context.Supplier.Find(id);
            if (db == null) return NotFound();


            return View("Create", db);
        }
        public ActionResult Delete(int id)
        {
            var db = _context.Supplier.Find(id);
            if (db == null) return NotFound();

            string detail = $"ID:{db.Id},  Name:{db.Name} ,  Email:{db.Email},  Phone:{db.Phone},  Urdu Name:{db.UrduName},  Business Name:{db.BusinessName}";

            db.IsDelete = 1;
            _context.SaveChanges();
            _userLog.SaveHistory("Supplier", "Delete", detail);

            return RedirectToAction("Index");

        }

    }
}
