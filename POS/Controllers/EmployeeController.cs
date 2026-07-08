using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
    public class EmployeeController : Controller
	{
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;
		private readonly IWebHostEnvironment _webHostEnvironment;
        public EmployeeController(IInterface @interface, IWebHostEnvironment webHostEnvironment, ApplicationDbContext context, UserLog userLog)
        {
			_webHostEnvironment = webHostEnvironment;
			_repo = @interface;
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
		private string GenrateCodeId()
		{
			string currentYear = AppDate.Now.ToString("yy");

			var code = _context.Employee
				.Where(x => !string.IsNullOrEmpty(x.CodeId))
				.OrderByDescending(x => x.Id)
				.Select(x => x.CodeId)
				.FirstOrDefault();

			if (string.IsNullOrEmpty(code))
				return $"EM-{currentYear}01";

			if (code.Length <= 5)
				return $"EM-{currentYear}01";

			string lastPart = code.Substring(5);

			if (!int.TryParse(lastPart, out int number))
				return $"EM-{currentYear}01";

			return $"EM-{currentYear}{number + 1}";
		}

		[HttpPost]
		public async Task<IActionResult> Save(EmployeeVM vm)
		{
			try
			{
				string message = "", action = "";

				string selectedAreasText = (vm.SelectedAreaIds != null && vm.SelectedAreaIds.Any())
					? string.Join(",", vm.SelectedAreaIds)
					: "None";

				if (vm.employee.Id == 0)
				{
					// ---------------- CREATE EMPLOYEE ----------------
					vm.employee.status = false;
					vm.employee.IsDelete = 0;
					vm.employee.CreatedBy = _userId;
					vm.employee.CreatedAt = AppDate.Now;
					vm.employee.CodeId = GenrateCodeId();

					_context.Employee.Add(vm.employee);
					_context.SaveChanges();

					// ---------------- CREATE ACCOUNT ----------------
					int accountNo = _repo.GetSingleValue<int>(
						"SELECT ISNULL(MAX(AccountNo),0)+1 FROM Account WHERE HeadId=5"
					);

					_context.Account.Add(new Account
					{
						Cid = vm.employee.Id,
						AccountNo = accountNo,
						CodeId = "",
						HeadId = 5,
						SubHead = 14,
						Name = vm.employee.Name,
						CreatedBy = _userId,
						CreatedAt = AppDate.Now,
						UpdatedBy = _userId,
						UpdatedAt = AppDate.Now,
						IsDelete = 0
					});

					// ---------------- SAVE AREAS ----------------
					if (vm.SelectedAreaIds != null && vm.SelectedAreaIds.Any())
					{
						_context.EmployeeArea.AddRange(
							vm.SelectedAreaIds.Select(areaId => new EmployeeArea
							{
								EmployeeId = vm.employee.Id,
								AreaId = areaId
							})
						);
					}

					if (vm.employee.ImageFile != null && vm.employee.ImageFile.Length > 0)
					{
						string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);

						string extension = Path.GetExtension(vm.employee.ImageFile.FileName);
						string fileName = $"Employee_{vm.employee.Id}{extension}";
						string filePath = Path.Combine(folder, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await vm.employee.ImageFile.CopyToAsync(stream);
						}

						_context.ImagesDetail.Add(new ImagesDetail
						{
							ImagePath = "/Images/" + fileName,
							Recordid = vm.employee.Id,
							Invtype = "Employee"
						});

						await _context.SaveChangesAsync();
					}

					_context.SaveChanges();

					message = "Employee Saved Successfully";
					action = "New";
				}
				else
				{
					// ---------------- UPDATE EMPLOYEE ----------------
					var db = _context.Employee.Find(vm.employee.Id);

					if (db == null)
					{
						TempData["error"] = "Employee not found!";
						return RedirectToAction("Index");
					}

					db.status = false;
					db.Name = vm.employee.Name;
					db.UrduName = vm.employee.UrduName;
					db.Salary = vm.employee.Salary;
					db.DateOfBirth = vm.employee.DateOfBirth;
					db.JoiningDate = vm.employee.JoiningDate;
					db.UpdatedBy = _userId;
					db.UpdatedAt = AppDate.Now;

					// ---------------- UPDATE ACCOUNT ----------------
					var acc = _context.Account.FirstOrDefault(x =>
						x.Cid == vm.employee.Id &&
						x.HeadId == 5 &&
						x.SubHead == 14 &&
						x.IsDelete == 0);

					if (acc != null)
					{
						acc.Name = vm.employee.Name;
						acc.UpdatedBy = _userId;
						acc.UpdatedAt = AppDate.Now;
					}

					// ---------------- UPDATE AREAS ----------------
					var oldAreas = _context.EmployeeArea
						.Where(x => x.EmployeeId == vm.employee.Id)
						.ToList();

					_context.EmployeeArea.RemoveRange(oldAreas);

					if (vm.SelectedAreaIds != null && vm.SelectedAreaIds.Any())
					{
						_context.EmployeeArea.AddRange(
							vm.SelectedAreaIds.Select(areaId => new EmployeeArea
							{
								EmployeeId = vm.employee.Id,
								AreaId = areaId
							})
						);
					}

					_context.SaveChanges();

					if (vm.employee.ImageFile != null && vm.employee.ImageFile.Length > 0)
					{
						string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);

						var existingImage = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == vm.employee.Id && x.Invtype == "Employee");
						if (existingImage != null)
						{
							string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingImage.ImagePath.TrimStart('/'));
							if (System.IO.File.Exists(oldFilePath))
							{
								System.IO.File.Delete(oldFilePath);
							}
							_context.ImagesDetail.Remove(existingImage);
						}

						string extension = Path.GetExtension(vm.employee.ImageFile.FileName);
						string fileName = $"Employee_{vm.employee.Id}{extension}";
						string filePath = Path.Combine(folder, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await vm.employee.ImageFile.CopyToAsync(stream);
						}

						_context.ImagesDetail.Add(new ImagesDetail
						{
							ImagePath = "/Images/" + fileName,
							Recordid = vm.employee.Id,
							Invtype = "Employee"
						});

						await _context.SaveChangesAsync();
					}

					message = "Employee Updated Successfully";
					action = "Edit";
				}

				// ---------------- CLEAN LOG DETAIL ----------------
				string detail =
					$"ID:{vm.employee.Id}, Name:{vm.employee.Name}, Salary:{vm.employee.Salary}, " +
					$"DOB:{vm.employee.DateOfBirth}, Joining:{vm.employee.JoiningDate}, " +
					$"Areas:[{selectedAreasText}]";

				TempData["save"] = message;
				_userLog.SaveHistory("Employee", action, detail);

				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				string error = ex.InnerException?.Message ?? ex.Message;

				_repo.LogErrorToFile(ex, $"Employee Save Error: {vm?.employee?.Name}");

				_userLog.SaveHistory(
					"Employee",
					"Error",
					$"Employee:{vm?.employee?.Name}, Error:{error}"
				);

				TempData["error"] = error;

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

			var imageDetail = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == id && x.Invtype == "Employee");
			if (employee != null)
			{
				employee.ImagePath = imageDetail?.ImagePath;
			}

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
				// ---------------- FIND EMPLOYEE ----------------
				var db = _context.Employee.Find(id);

				if (db == null)
					return NotFound();

				// ---------------- GET AREAS ----------------
				var employeeAreas = _context.EmployeeArea
					.Where(x => x.EmployeeId == id && x.IsDelete == 0)
					.ToList();

				foreach (var area in employeeAreas)
				{
					area.IsDelete = 1;
				}

				// Convert areas to readable string for log
				string areaText = employeeAreas.Any()
					? string.Join(",", employeeAreas.Select(x => x.AreaId))
					: "None";

				// ---------------- SOFT DELETE EMPLOYEE ----------------
				db.IsDelete = 1;

				// ---------------- SOFT DELETE ACCOUNT ----------------
				var acc = _context.Account.FirstOrDefault(x =>
					x.Cid == id &&
					x.HeadId == 5 &&
					x.SubHead == 14 &&
					x.IsDelete == 0);

				if (acc != null)
				{
					acc.IsDelete = 1;
				}

				// ---------------- SAVE ALL CHANGES ----------------
				_context.SaveChanges();

				// ---------------- CLEAN LOG DETAIL ----------------
				string detail =
					$"ID:{db.Id}, Name:{db.Name}, DOB:{db.DateOfBirth}, " +
					$"Joining:{db.JoiningDate}, Salary:{db.Salary}, Areas:[{areaText}]";

				_userLog.SaveHistory("Employee", "Delete", detail);

				TempData["save"] = "Employee deleted successfully";

				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = ex.Message;
				return RedirectToAction("Index");
			}
		}
	}
}