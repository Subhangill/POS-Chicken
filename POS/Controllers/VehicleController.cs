using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Service;
using POS.Models;

namespace POS.Controllers
{
    public class VehicleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;
        
        public VehicleController(ApplicationDbContext context, UserLog userLog)
        {
            _context = context;
            _userLog = userLog;
            _userId = UserHelper.GetCurrentUserId();
        }
        public IActionResult Index()
        {
            var list = _context.Vehicle.ToList();
            return View(list);
        }



        public IActionResult Create()
        {
            return View();
        }

        public string GenrateCodeId()
        {
            string currentYear = AppDate.Now.ToString("yy");
            var Code = _context.Area.Where(x => x.CodeId != null).OrderByDescending(x => x.Id).Select(x => x.CodeId).FirstOrDefault();

            if (string.IsNullOrEmpty(Code)) return $"AR-{currentYear}01";

            string lastpart = Code.Substring(5);
            int number = int.Parse(lastpart);
            int newNumber = number + 1;
            return $"VH-{currentYear}{newNumber}";

        }



        [HttpPost]
        public IActionResult Save(Vehicle vehicle)
        {
            try
            {
                string ms = "";


                _context.Area.Find(vehicle.Id);
                if (vehicle.Id == 0)
                {
                    vehicle.CreatedBy = _userId;
                    vehicle.CreatedAt = AppDate.Now;
                    vehicle.CodeId = GenrateCodeId();

                    _context.Vehicle.Add(vehicle);
                    ms = "Data Saved Successfully";
                  
                }
                else
                {
                    ms = "Data update successfully";
                    var db = _context.Vehicle.Find(vehicle.Id);


                    if (db != null)
                    {
                        db.VehicleNo = vehicle.VehicleNo;
                        db.UpdatedBy = UserHelper.GetCurrentUserId();
                        db.IsDelete = 0;
                        db.UpdatedAt = AppDate.Now;

                    }
                }
                string detail;
                string Action;
                if (vehicle.Id == 0)
                {
                    detail = $"ID:{vehicle.Id},  Vehicle No:{vehicle.VehicleNo}";
                    Action = "New";
                }
                else
                {
                    detail = $"ID:{vehicle.Id},  Vehicle No:{vehicle.VehicleNo}";
                    Action = "Edit";

                }
                TempData["save"] = ms;
                _context.SaveChanges();
                _userLog.SaveHistory("Vehicle", Action, detail);
                return RedirectToAction("Create");
            }
            catch (Exception er)
            {

                return RedirectToAction("Create");

            }
        }
        public ActionResult Edit(int Id)
        {
            var db = _context.Vehicle.Find(Id);
            {
                if (db == null) return NotFound();

                return View("Create", db);
            }
        }
        public ActionResult Delete(int id)
        {
            var db = _context.Vehicle.Find(id);
            if (db == null) return NotFound();


            string detail = $"ID:{db.Id},  Vehicle No:{db.VehicleNo}";
            db.IsDelete = 1;
            _context.SaveChanges();
            _userLog.SaveHistory("Vehicle", "Delete", detail);
            return RedirectToAction("Index");


        }

    }
}
