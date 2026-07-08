using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Service;
using POS.Models;

namespace POS.Controllers
{
	public class AreaController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserLog _userLog;
		private readonly int _userId;
		public AreaController(ApplicationDbContext context, UserLog userLog)
		{
			_userId = UserHelper.GetCurrentUserId();
			_context = context;
			_userLog = userLog;
		}
		public IActionResult Index()
		{
			var list = _context.Area.ToList();
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
			return $"AR-{currentYear}{newNumber}";

		}



		[HttpPost]
		public IActionResult Save(Area area)
		{
			try
			{
				string ms = "";


				_context.Area.Find(area.Id);
				if (area.Id == 0)
				{
					area.CreatedBy = _userId;
					area.CreatedAt = AppDate.Now;
					area.CodeId = GenrateCodeId();

					_context.Area.Add(area);
					ms = "Data Saved Successfully";
					//string detail = $"Name:{area.Name},Urdu:{area.UrduName}";
					//_userLog.SaveHistory("Area", "New", detail);
				}
				else
				{
					ms = "Data Updated successfully";
					var db = _context.Area.Find(area.Id);


					if (db != null)
					{
						db.Name = area.Name;
						db.UrduName = area.UrduName;
						db.UpdatedBy = _userId;
						db.IsDelete = 0;
						db.UpdatedAt = AppDate.Now;
					}
				}
				string detail;
				string Action;
				if (area.Id == 0)
				{
					detail = $"ID:{area.Id},  Name:{area.Name},  Urdu Name: {area.UrduName}";
					Action = "New";
				}
				else
				{
					detail = $"ID:{area.Id},  Name:{area.Name},  UrduName:{area.UrduName}";
					Action = "Edit";
				}
				TempData["save"] = ms;
				_context.SaveChanges();
				_userLog.SaveHistory("Area", Action, detail);
				return RedirectToAction("Create");
			}
			catch (Exception er)
			{

				return RedirectToAction("Create");

			}
		}


		public ActionResult Edit(int Id)
		{
			var db = _context.Area.Find(Id);
				if (db == null) return NotFound();

				return View("Create", db);
		}

		public ActionResult Delete(int id)
		{
			var db = _context.Area.Find(id);
			if (db == null) return NotFound();


			string detail = $"ID:{db.Id},  Name:{db.Name},  Urdu Name:{db.UrduName}";
			db.IsDelete = 1;
			_context.SaveChanges();
			_userLog.SaveHistory("Area", "Delete", detail);
			return RedirectToAction("Index");


		}
	}
}
