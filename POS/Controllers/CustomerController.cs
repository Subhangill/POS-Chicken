using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
	public class CustomerController : Controller
	{
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		private readonly UserLog _userLog;
		private readonly int _userId;
		private readonly IWebHostEnvironment _webHostEnvironment;
		public CustomerController(IInterface @interface, IWebHostEnvironment webHostEnvironment, ApplicationDbContext context, UserLog userLog)
		{
			_webHostEnvironment = webHostEnvironment;
			_repo = @interface;
			_userId = UserHelper.GetCurrentUserId();
			_context = context;
			_userLog = userLog;
		}
		public IActionResult Index()
		{
			var list = _context.Customer.ToList();
			return View(list);
		}

		public IActionResult Create(Customer customer)
		{
			var vm = new CustomerVM()
			{
				customer = customer,
				arealist = _context.Area.ToList()
			};
			return View(vm);
		}

		private string GenrateCodeId()
		{
			string currentYear = AppDate.Now.ToString("yy");
			var Code = _context.Customer.Where(x => x.CodeId != null).OrderByDescending(x => x.Id).Select(x => x.CodeId).FirstOrDefault();

			if (string.IsNullOrEmpty(Code)) return $"CU-{currentYear}01";

			string lastpart = Code.Substring(5);
			int number = int.Parse(lastpart);
			int newNumber = number + 1;
			return $"CU-{currentYear}{newNumber}";

		}

		[HttpPost]
		public async Task<IActionResult> Save(CustomerVM vm)
		{
			try
			{
				string message, redirectAction, action;

				if (vm.customer.Id == 0)
				{
					vm.customer.CodeId = GenrateCodeId();
					vm.customer.CreatedBy = _userId;
					vm.customer.CreatedAt = AppDate.Now;
					vm.customer.IsDelete = 0;

					_context.Customer.Add(vm.customer);
					_context.SaveChanges();

					int accountNo = _repo.GetSingleValue<int>(
						"SELECT ISNULL(MAX(AccountNo),0)+1 FROM Account WHERE HeadId = 1");

					_context.Account.Add(new Account
					{
						Cid = vm.customer.Id,
						AccountNo = accountNo,
						HeadId = 1,
						CodeId="",
						SubHead = 3,
						Name = vm.customer.Name,
						CreatedBy = _userId,
						CreatedAt = AppDate.Now,
						UpdatedBy = _userId,
						UpdatedAt = AppDate.Now,
						IsDelete = 0
					});

					if (vm.customer.ImageFile != null && vm.customer.ImageFile.Length > 0)
					{
						string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);

						string extension = Path.GetExtension(vm.customer.ImageFile.FileName);
						string fileName = $"Customer_{vm.customer.Id}{extension}";
						string filePath = Path.Combine(folder, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await vm.customer.ImageFile.CopyToAsync(stream);
						}

						_context.ImagesDetail.Add(new ImagesDetail
						{
							ImagePath = "/Images/" + fileName,
							Recordid = vm.customer.Id,
							Invtype = "Customer"
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
					var db = _context.Customer.Find(vm.customer.Id);

					if (db == null)
					{
						TempData["error"] = "Customer not found!";
						return RedirectToAction("Index");
					}

					db.Name = vm.customer.Name;
					db.UrduName = vm.customer.UrduName;
					db.AreaId = vm.customer.AreaId;
					db.Phone = vm.customer.Phone;
					db.Email = vm.customer.Email;
					db.BusinessName = vm.customer.BusinessName;
					db.UpdatedBy = _userId;
					db.UpdatedAt = AppDate.Now;

					// Update Account Name also
					var acc = _context.Account
						.FirstOrDefault(x => x.Cid == vm.customer.Id && x.IsDelete == 0 && x.SubHead == 3);

					if (acc != null)
					{
						acc.Name = vm.customer.Name;
						acc.UpdatedBy = _userId;
						acc.UpdatedAt = AppDate.Now;
					}

					_context.SaveChanges();

					if (vm.customer.ImageFile != null && vm.customer.ImageFile.Length > 0)
					{
						string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);

						var existingImage = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == vm.customer.Id && x.Invtype == "Customer");
						if (existingImage != null)
						{
							string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingImage.ImagePath.TrimStart('/'));
							if (System.IO.File.Exists(oldFilePath))
							{
								System.IO.File.Delete(oldFilePath);
							}
							_context.ImagesDetail.Remove(existingImage);
						}

						string extension = Path.GetExtension(vm.customer.ImageFile.FileName);
						string fileName = $"Customer_{vm.customer.Id}{extension}";
						string filePath = Path.Combine(folder, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await vm.customer.ImageFile.CopyToAsync(stream);
						}

						_context.ImagesDetail.Add(new ImagesDetail
						{
							ImagePath = "/Images/" + fileName,
							Recordid = vm.customer.Id,
							Invtype = "Customer"
						});

						await _context.SaveChangesAsync();
					}

					message = "Data Updated Successfully";
					redirectAction = "Index";
					action = "Edit";
				}

				string detail =$"ID:{vm.customer.Id}, Name:{vm.customer.Name}, Business:{vm.customer.BusinessName}, Phone:{vm.customer.Phone}";

				TempData["save"] = message;

				_userLog.SaveHistory("Customer", action, detail);

				return RedirectToAction(redirectAction);
			}
			catch (Exception ex)
			{
				string errorMessage = ex.InnerException?.Message ?? ex.Message;
				_repo.LogErrorToFile(ex,$"Error saving customer: {vm?.customer?.Name}");
				_userLog.SaveHistory("Customer","Error",$"Customer:{vm?.customer?.Name}, Error:{errorMessage}");
				TempData["error"] = errorMessage;
				return RedirectToAction(vm?.customer?.Id > 0 ? "Edit" : "Create",new { id = vm.customer?.Id });
			}
		}

		public ActionResult Edit(int id)
		{
			var cust = _context.Customer.Find(id);
			if (cust == null) return NotFound();

			var imageDetail = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == id && x.Invtype == "Customer");
			if (cust != null)
			{
				cust.ImagePath = imageDetail?.ImagePath;
			}

			var vm = new CustomerVM()
			{
				arealist = _context.Area.ToList(),
				customer = cust
			};
			return View("Create", vm);
		}
		public ActionResult Delete(int id)
		{
			var db = _context.Customer.Find(id);
			if (db == null) return NotFound();

			string detail = $"ID:{db.Id},  Name:{db.Name} ,  Email:{db.Email},  Phone:{db.Phone},  Urdu Name:{db.UrduName},  Business Name:{db.BusinessName},  Area:{db.AreaId}";
			db.IsDelete = 1;
			var acc = _context.Account.FirstOrDefault(x => x.Cid == id && x.IsDelete == 0 && x.SubHead == 3);

			if (acc != null)
			{
				acc.IsDelete = 1;
			}
			_context.SaveChanges();
			_userLog.SaveHistory("Customer", "Delete", detail);

			return RedirectToAction("Index");

		}
	}
}
