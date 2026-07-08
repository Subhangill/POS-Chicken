using Humanizer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
    public class SupplierController : Controller
	{
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public SupplierController(IInterface @interface, IWebHostEnvironment webHostEnvironment ,ApplicationDbContext context, UserLog userLog)
        {
			_webHostEnvironment = webHostEnvironment;
			_repo = @interface;
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
        private  string GenrateCodeId()
        {
			string currentYear = AppDate.Now.ToString("yy");

			var code = _context.Supplier
				.Where(x => !string.IsNullOrEmpty(x.CodeId))
				.OrderByDescending(x => x.Id)
				.Select(x => x.CodeId)
				.FirstOrDefault();

			if (string.IsNullOrEmpty(code))
				return $"SP-{currentYear}01";

			if (code.Length <= 5)
				return $"SP-{currentYear}01";

			string lastPart = code.Substring(5);

			if (!int.TryParse(lastPart, out int number))
				return $"SP-{currentYear}01";

			return $"SP-{currentYear}{number + 1}";
		}
		[HttpPost]
		public async Task<IActionResult> SaveAsync(SupplierVM vm)
		{
			try
			{
				string message, redirectAction, action;

				if (vm.supplier.Id == 0)
				{
					vm.supplier.CodeId = GenrateCodeId();
					vm.supplier.CreatedBy = _userId;
					vm.supplier.CreatedAt = AppDate.Now;
					vm.supplier.IsDelete = 0;

					_context.Supplier.Add(vm.supplier);
					_context.SaveChanges();

					int accountNo = _repo.GetSingleValue<int>(
						"SELECT ISNULL(MAX(AccountNo),0)+1 FROM Account WHERE HeadId = 2");

					_context.Account.Add(new Account
					{
						Cid = vm.supplier.Id,
						AccountNo = accountNo,
						HeadId = 2,
						CodeId = "",
						SubHead = 7,
						Name = vm.supplier.Name,
						CreatedBy = _userId,
						CreatedAt = AppDate.Now,
						UpdatedBy = _userId,
						UpdatedAt = AppDate.Now,
						IsDelete = 0
					});
					if (vm.supplier.ImageFile != null && vm.supplier.ImageFile.Length > 0)
					{
						string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);

						string extension = Path.GetExtension(vm.supplier.ImageFile.FileName);
						string fileName = $"Supplier_{vm.supplier.Id}{extension}";
						string filePath = Path.Combine(folder, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await vm.supplier.ImageFile.CopyToAsync(stream);
						}

						_context.ImagesDetail.Add(new ImagesDetail
						{
							ImagePath = "/Images/" + fileName,
							Recordid = vm.supplier.Id,
							Invtype = "Supplier"
						});

						await _context.SaveChangesAsync();
					}
					_context.SaveChanges();

					message = "Data Saved Successfully";
					redirectAction = "Create";
					action = "New";
				}
				else
				{
					var db = _context.Supplier.Find(vm.supplier.Id);

					if (db == null)
					{
						TempData["error"] = "Supplier not found!";
						return RedirectToAction("Index");
					}

					db.Name = vm.supplier.Name;
					db.UrduName = vm.supplier.UrduName;
					db.AreaId = vm.supplier.AreaId;
					db.Phone = vm.supplier.Phone;
					db.Email = vm.supplier.Email;
					db.BusinessName = vm.supplier.BusinessName;
					db.UpdatedBy = _userId;
					db.UpdatedAt = AppDate.Now;

					// Update Account Name also
					var acc = _context.Account.FirstOrDefault(x => x.Cid == vm.supplier.Id && x.IsDelete == 0&&x.SubHead==7);

					if (acc != null)
					{
						acc.Name = vm.supplier.Name;
						acc.UpdatedBy = _userId;
						acc.UpdatedAt = AppDate.Now;
					}

					_context.SaveChanges();

					if (vm.supplier.ImageFile != null && vm.supplier.ImageFile.Length > 0)
					{
						string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);

						var existingImage = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == vm.supplier.Id && x.Invtype == "Supplier");
						if (existingImage != null)
						{
							string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingImage.ImagePath.TrimStart('/'));
							if (System.IO.File.Exists(oldFilePath))
							{
								System.IO.File.Delete(oldFilePath);
							}
							_context.ImagesDetail.Remove(existingImage);
						}

						string extension = Path.GetExtension(vm.supplier.ImageFile.FileName);
						string fileName = $"Supplier_{vm.supplier.Id}{extension}";
						string filePath = Path.Combine(folder, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await vm.supplier.ImageFile.CopyToAsync(stream);
						}

						_context.ImagesDetail.Add(new ImagesDetail
						{
							ImagePath = "/Images/" + fileName,
							Recordid = vm.supplier.Id,
							Invtype = "Supplier"
						});

						await _context.SaveChangesAsync();
					}

					message = "Data Updated Successfully";
					redirectAction = "Index";
					action = "Edit";
				}

				string detail = $"ID:{vm.supplier.Id}, Name:{vm.supplier.Name}, Business:{vm.supplier.BusinessName}, Phone:{vm.supplier.Phone}";

				TempData["save"] = message;

				_userLog.SaveHistory("Supplier", action, detail);

				return RedirectToAction(redirectAction);
			}
			catch (Exception ex)
			{
				string errorMessage = ex.InnerException?.Message ?? ex.Message;
				_repo.LogErrorToFile(ex, $"Error saving supplier: {vm?.supplier?.Name}");
				_userLog.SaveHistory("Supplier", "Error", $"Supplier:{vm?.supplier?.Name}, Error:{errorMessage}");
				TempData["error"] = errorMessage;
				return RedirectToAction(vm.supplier.Id > 0 ? "Edit" : "Create", new { id = vm.supplier?.Id });
			}
		}

		public ActionResult Edit(int id)
        {
            var db = _context.Supplier.Find(id);
            if (db == null) return NotFound();

			var imageDetail = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == id && x.Invtype == "Supplier");
			if (db != null)
			{
				db.ImagePath = imageDetail?.ImagePath;
			}

			var vm = new SupplierVM
			{
				supplier = db,
				arealist = _context.Area.Where(x => x.IsDelete == 0).ToList()
			};
			return View("Create", vm);
        }
        public ActionResult Delete(int id)
        {
            var db = _context.Supplier.Find(id);
            if (db == null) return NotFound();

            string detail = $"ID:{db.Id},  Name:{db.Name} ,  Email:{db.Email},  Phone:{db.Phone},  Urdu Name:{db.UrduName},  Business Name:{db.BusinessName}";

            db.IsDelete = 1; 
			var acc = _context.Account.FirstOrDefault(x => x.Cid == id && x.IsDelete == 0 && x.SubHead == 7);

			if (acc != null)
			{
				acc.IsDelete = 1;
			}
			_context.SaveChanges();
            _userLog.SaveHistory("Supplier", "Delete", detail);

            return RedirectToAction("Index");

        }

    }
}
