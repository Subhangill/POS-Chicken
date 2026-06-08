using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{


    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;
        public CustomerController(ApplicationDbContext context, UserLog userLog)
        {
            _userId = UserHelper.GetCurrentUserId();
            _context = context;
            _userLog = userLog;
        }
        public IActionResult Index()
        {
            var list = _context.Customer.ToList();
            return View(list);
        }

        public IActionResult Create()
        {
            var customer = new Customer();
            var vm = new CustomerVM()
            {
                customer = customer,
                arealist = _context.Area.ToList()
            };

            return View("Create", vm);
        }

        public string GenrateCodeId()
        {
            string currentYear = AppDate.Now.ToString("yy");
            var Code = _context.Customer.Where(x => x.CodeId != null).OrderByDescending(x => x.Id).Select(x => x.CodeId).FirstOrDefault();

            if (string.IsNullOrEmpty(Code)) return $"PR-{currentYear}01";

            string lastpart = Code.Substring(5);
            int number = int.Parse(lastpart);
            int newNumber = number + 1;
            return $"CU-{currentYear}{newNumber}";

        }

        [HttpPost]

        public IActionResult Save(CustomerVM vm)
        {
            try
            {
                string ms = "";
                if (vm.customer.Id == 0)
                {
                    vm.customer.CodeId = GenrateCodeId();
                    vm.customer.CreatedBy = _userId;
                    vm.customer.CreatedAt = AppDate.Now;
                    vm.customer.IsDelete = 0;
                    _context.Add(vm.customer);
                    ms = "Data Saved Successfully";
                }
                else
                {
                    ms = "Data update Successfully";
                    var db = _context.Customer.Find(vm.customer.Id);
                    if (db != null)
                    {
                        db.Name = vm.customer.Name;
                        db.UrduName = vm.customer.UrduName;
                        db.AreaId = vm.customer.AreaId;
                        db.Phone = vm.customer.Phone;
                        db.Email = vm.customer.Email;
                        db.BusinessName = vm.customer.BusinessName;
                        db.UpdatedBy = UserHelper.GetCurrentUserId();
                        db.UpdatedAt = AppDate.Now;
                    }
                }
                string detail, Action;
                if (vm.customer.Id == 0)
                {
                    detail = $"ID:{vm.customer.Id},  Name:{vm.customer.Name} ,  Email:{vm.customer.Email},  Phone:{vm.customer.Phone},  Urdu Name:{vm.customer.UrduName},  Business Name:{vm.customer.BusinessName},  Area:{vm.customer.AreaId}";
                    Action = "New";

                }
                else
                {
                    detail = $"ID:{vm.customer.Id},  Name:{vm.customer.Name} ,  Email:{vm.customer.Email},  Phone:{vm.customer.Phone},  Urdu Name:{vm.customer.UrduName},  Business Name:{vm.customer.BusinessName},  Area:{vm.customer.AreaId}";
                    Action = "Edit";
                }
                TempData["save"] = ms;
                _context.SaveChanges();
                _userLog.SaveHistory("Customer", Action, detail);

                return RedirectToAction("Create");
            }
            catch (Exception er)
            {
                return RedirectToAction("Create");
            }
        }
        public ActionResult Edit(int id)
        {
            var vm = new CustomerVM()
            {
                arealist = _context.Area.ToList(),
                customer = _context.Customer.Find(id)
            };

            return View("Create", vm);
        }
        public ActionResult Delete(int id)
        {
            var db = _context.Customer.Find(id);
            if (db == null) return NotFound();

            string detail = $"ID:{db.Id},  Name:{db.Name} ,  Email:{db.Email},  Phone:{db.Phone},  Urdu Name:{db.UrduName},  Business Name:{db.BusinessName},  Area:{db.AreaId}";
            db.IsDelete = 1;
            _context.SaveChanges();
            _userLog.SaveHistory("Customer", "Delete", detail);

            return RedirectToAction("Index");

        }
    }
}
