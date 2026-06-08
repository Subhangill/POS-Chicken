using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;
        public ProductController(ApplicationDbContext context, UserLog userlog)
        {
            _userId = UserHelper.GetCurrentUserId();
            _context = context;
            _userLog = userlog;
        }
        public IActionResult Index()
        {
            var list = _context.Product.ToList();
            return View(list);
        }

        public IActionResult Create()
        {
            var product = new Product();
            var vm = new ProductVM()
            {
                product = product,
                categorylist = _context.Category.ToList(),
                arealist = _context.Area.ToList(),

            };
            return View(vm);
        }
        public string GenrateCodeId()
        {
            string currentYear = AppDate.Now.ToString("yy");
            var Code = _context.Product.Where(x => x.CodeId != null).OrderByDescending(x => x.Id).Select(x => x.CodeId).FirstOrDefault();

            if (string.IsNullOrEmpty(Code)) return $"PR-{currentYear}01";

            string lastpart = Code.Substring(5);
            int number = int.Parse(lastpart);
            int newNumber = number + 1;
            return $"PR-{currentYear}{newNumber}";

        }


        [HttpPost]
        public IActionResult Save(ProductVM productvm)
        {
            try
            {
                string ms = "";


                if (productvm.product.Id == 0)
                {
                    productvm.product.IsDelete = 0;
                    productvm.product.CodeId = GenrateCodeId();
                    productvm.product.BrandId = 1;
                    productvm.product.CreatedAt = AppDate.Now;
                    productvm.product.CreatedBy = _userId;
                    _context.Product.Add(productvm.product);
                    ms = "Data saved Successfully";
                }
                else
                {
                    ms = "Data update Successfully";
                    var db = _context.Product.Find(productvm.product.Id);
                    if (db != null)
                    {
                        db.Name = productvm.product.Name;
                        db.UrduName = productvm.product.UrduName;
                        db.Price = productvm.product.Price;
                        db.CategoryId = productvm.product.CategoryId;
                        db.BrandId = productvm.product.BrandId;
                        db.UpdatedBy = UserHelper.GetCurrentUserId();
                        db.UpdatedAt = AppDate.Now;
                    }
                }
                string detail, Action;
                if (productvm.product.Id == 0)
                {
                    detail = $"ID:{productvm.product.Id},  Name:{productvm.product.Name},  BrandId:{productvm.product.BrandId},  CategoryId:{productvm.product.CategoryId},  Price:{productvm.product.Price}";
                    Action = "New";

                }
                else
                {
                    detail = $"ID:{productvm.product.Id},  Name:{productvm.product.Name},  BrandId:{productvm.product.BrandId},  CategoryId:{productvm.product.CategoryId},  Price:{productvm.product.Price}";
                    Action = "Edit";
                }
                TempData["save"] = ms;
                _context.SaveChanges();
                _userLog.SaveHistory("Product", Action, detail);

                return RedirectToAction("Create");
            }
            catch
            {
                return RedirectToAction("Create");

            }
        }
        public ActionResult Edit(int id)
        {
            
            var vm = new ProductVM()
            {
                product = _context.Product.Find(id),
                categorylist = _context.Category.ToList(),
                arealist = _context.Area.ToList()
            };

            return View("Create", vm);
        }
        public ActionResult Delete(int id)
        {
            var db = _context.Product.Find(id);

            if (db == null) return NotFound();


            string detail = $"ID:{db.Id},  Name:{db.Name},  BrandId:{db.BrandId},  CategoryId:{db.CategoryId},  Price:{db.Price}";


            db.IsDelete = 1;
            _context.SaveChanges();

            _userLog.SaveHistory("Product", "Delete", detail);

            return RedirectToAction("Index");
        }

    }
}
