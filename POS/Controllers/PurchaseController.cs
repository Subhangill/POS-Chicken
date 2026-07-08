using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
	public class PurchaseController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly DateTime _dateTime;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public PurchaseController(UserLog userLog, ApplicationDbContext applicationDbContext, IInterface @interface, IWebHostEnvironment webHostEnvironment)
		{
			_webHostEnvironment = webHostEnvironment;
			_userlog = userLog;
			_dateTime = AppDate.Now;
			_userId = UserHelper.GetCurrentUserId();
			_context = applicationDbContext;
			_repo = @interface;
		}
		public IActionResult Index(string s_date, string e_date)
		{
			DateTime startdate, enddate;
			if (string.IsNullOrWhiteSpace(s_date) && string.IsNullOrWhiteSpace(e_date))
			{
				startdate = AppDate.Today;
				enddate = AppDate.Today;
			}
			else
			{
				startdate = DateTime.Parse(s_date);
				enddate = DateTime.Parse(e_date);
			}
			var list = _repo.GetList<WasteReceive>("Select s.name as Suppname,a.name as Areaname,w.source,w.Id,w.Date,w.NetWeight FROM WasteReceivedMaster as w LEFT JOIN Area as a on a.id=w.area LEFT JOIN Supplier as s on s.id=w.suppid WHERE w.isdelete=0 and w.Date BETWEEN @start and @end ORDER BY w.Date,w.Id DESC", new { start = startdate.ToString("yyyy-MM-dd"), end = enddate.ToString("yyyy-MM-dd") }).ToList();
			ViewBag.s_date = startdate.ToString("yyyy-MM-dd");
			ViewBag.e_date = enddate.ToString("yyyy-MM-dd");
			return View(list);
		}
		[HttpGet]
		public JsonResult GetSupplierList()
		{
			return Json(_repo.GetList<Supplier>("Select Id,name From Supplier WHERE IsDelete=0 Order by Id").ToList());
		}
		public IActionResult Create(WasteReceive wasteReceive)
		{
			wasteReceive.Date = _dateTime;
			var Vm = new PurchaseVm()
			{
				Vehiclelist=_context.Vehicle.Where(e=>e.IsDelete==0).ToList(),
				Arealist=_context.Area.Where(e=>e.IsDelete==0).ToList(),
				WasteReceived = wasteReceive,
				Employeelist = _repo.GetList<Employee>("Select Id,name From Employee WHERE IsDelete=0 Order by Id").ToList(),
				Supplierlist = _repo.GetList<Supplier>("Select Id,name From Supplier WHERE IsDelete=0 Order by Id").ToList(),
				Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 3).ToList(),
			};
			return View(Vm);
		}
		[HttpPost]
		public async Task<IActionResult> Save(PurchaseVm purchaseVm)
		{
			try
			{
				string wastedetail = "";
				string itemdetail = "";
				bool isNew = purchaseVm.WasteReceived.Id == 0;
				string action = isNew ? "New" : "Edit";

				// ====================== SAVE MASTER ======================
				if (isNew)
				{
					purchaseVm.WasteReceived.CreatedAt = _dateTime;
					purchaseVm.WasteReceived.CreatedBy = _userId;
					purchaseVm.WasteReceived.UpdatedBy = 0;
					_context.WasteReceivedMaster.Add(purchaseVm.WasteReceived);
				}
				else
				{
					var existing = _context.WasteReceivedMaster.FirstOrDefault(e => e.Id == purchaseVm.WasteReceived.Id);
					if (existing == null)
					{
						TempData["error"] = "Record Not Found!!";
						return RedirectToAction("Index");
					}

					existing.Note = purchaseVm.WasteReceived.Note ?? "";
					existing.GrossWeight = purchaseVm.WasteReceived.GrossWeight;
					existing.NetWeight = purchaseVm.WasteReceived.NetWeight;
					existing.Vehicle = purchaseVm.WasteReceived.Vehicle;
					existing.Source = purchaseVm.WasteReceived.Source;
					existing.Area = purchaseVm.WasteReceived.Area;
					existing.Suppid = purchaseVm.WasteReceived.Suppid;
					existing.Empid = purchaseVm.WasteReceived.Empid;
					existing.WasteWeight = purchaseVm.WasteReceived.WasteWeight;
					existing.UpdatedBy = _userId;
					existing.UpdatedAt = _dateTime;
					existing.Date = purchaseVm.WasteReceived.Date;  
				}

				_context.SaveChanges();

				int masterId = purchaseVm.WasteReceived.Id;

				if (purchaseVm.WasteReceived.ImageFile != null && purchaseVm.WasteReceived.ImageFile.Length > 0)
				{
					string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

					if (!Directory.Exists(folder))
						Directory.CreateDirectory(folder);

					if (!isNew)
					{
						var existingImage = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == masterId && x.Invtype == "Waste Receiving");
						if (existingImage != null)
						{
							string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingImage.ImagePath.TrimStart('/'));
							if (System.IO.File.Exists(oldFilePath))
							{
								System.IO.File.Delete(oldFilePath);
							}
							_context.ImagesDetail.Remove(existingImage);
						}
					}

					string extension = Path.GetExtension(purchaseVm.WasteReceived.ImageFile.FileName);
					string fileName = $"WasteReceive_{masterId}_{DateTime.Now.Ticks}{extension}";
					string filePath = Path.Combine(folder, fileName);

					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						await purchaseVm.WasteReceived.ImageFile.CopyToAsync(stream);
					}

					_context.ImagesDetail.Add(new ImagesDetail
					{
						ImagePath = "/Images/" + fileName,
						Recordid = masterId,
						Invtype = "Waste Receiving"
					});

					await _context.SaveChangesAsync();
				}

				// ====================== DELETE OLD DETAILS (For Edit) ======================
				if (!isNew)
				{
					_context.RawWastageDetail.Where(d => d.Invid == masterId).ExecuteDelete();
					_context.WasteReceiveSubDetail.Where(d => d.Invid == masterId).ExecuteDelete();
					_context.WasteReceiveDetail.Where(d => d.Invid == masterId).ExecuteDelete();   
				}

				// ====================== SAVE MAIN DETAILS + SUB DETAILS ======================
				if (purchaseVm.WasteReceiveDetaillist != null && purchaseVm.WasteReceiveDetaillist.Any())
				{
					foreach (var itm in purchaseVm.WasteReceiveDetaillist)
					{
						itm.Invid = masterId;
						_context.WasteReceiveDetail.Add(itm);
					}
					_context.SaveChanges();
					// Now save Sub Details + Build Log
					foreach (var itm in purchaseVm.WasteReceiveDetaillist)
					{
						string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "N/A";
						string suppname = _context.Supplier.Where(e => e.Id == itm.Suppid).Select(e => e.Name).FirstOrDefault() ?? "N/A";

						string subDetailLog = "";

						if (itm.SubDetails != null && itm.SubDetails.Any())
						{
							foreach (var sub in itm.SubDetails)
							{
								string subSuppName = _context.Supplier.Where(e => e.Id == sub.SubSuppid).Select(e => e.Name).FirstOrDefault() ?? "N/A";
								sub.WasteReceiveDetailId = itm.Id;
								sub.Invid = masterId;
								sub.Pid = itm.Pid;
								_context.WasteReceiveSubDetail.Add(sub);
								subDetailLog += $"{subSuppName}-{sub.SubSuppid}:{sub.Weight}kg, ";
							}
						}

						itemdetail += $"Item:{itm.Pid}-{itemname} || Weight:{itm.Weight} || Supplier:{itm.Suppid}-{suppname}";
						if (!string.IsNullOrEmpty(subDetailLog))
							itemdetail += $" || SubDetail==>{subDetailLog.TrimEnd(',', ' ')}";

						itemdetail += " || ";
					}
				}

				// ====================== SAVE WASTAGE DETAILS ======================
				if (purchaseVm.RawWastageDetaillist != null && purchaseVm.RawWastageDetaillist.Any())
				{
					foreach (var itm in purchaseVm.RawWastageDetaillist)
					{
						string pname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "";
						itm.Invid = masterId;
						_context.RawWastageDetail.Add(itm);
						wastedetail += $"Item:{itm.Pid+"-"+pname} || Weight:{itm.Weight}";
					}
				}

				_context.SaveChanges();

				// ====================== LOG HISTORY ======================
				string source = purchaseVm.WasteReceived.Source == 0 ? "Route" : "Sended By Supplier";

				_userlog.SaveHistory("Waste Receiving", action,
					$"Id:{masterId}, Date:{purchaseVm.WasteReceived.Date:yyyy-MM-dd}, " +
					$"Source:{source}, Note:{purchaseVm.WasteReceived.Note}, " +
					$"Gross Weight:{purchaseVm.WasteReceived.GrossWeight}, Waste Weight:{purchaseVm.WasteReceived.WasteWeight}, " +
					$"Net Weight:{purchaseVm.WasteReceived.NetWeight} || Items ==>{itemdetail} || Wastage==>{wastedetail} ");

				TempData["save"] = isNew ? "Inv Saved Successfully!!" : "Inv Updated Successfully!!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error in Waste Receiving ID: {purchaseVm.WasteReceived.Id}");
				_userlog.SaveHistory("Waste Receiving", "Error", $"Error: {ex.Message}");

				TempData["error"] = "An error occurred while saving.";

				// Reload lists for error view
				purchaseVm.Employeelist = _repo.GetList<Employee>("Select Id,name From Employee WHERE IsDelete=0 Order by Id").ToList();
				purchaseVm.Supplierlist = _repo.GetList<Supplier>("Select Id,name From Supplier WHERE IsDelete=0 Order by Id").ToList();
				purchaseVm.Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 3).ToList();

				return View("Create", purchaseVm);
			}
		}
		public IActionResult Edit(int id)
		{
			var existing = _context.WasteReceivedMaster.FirstOrDefault(e => e.Id == id);
			if (existing == null)
			{
				TempData["error"] = "Inv Not Found!!";
				return RedirectToAction("Index");
			}
			var details = _context.WasteReceiveDetail.Where(e => e.Invid == id).ToList();
			foreach (var itm in details)
			{
				itm.SubDetails = _context.WasteReceiveSubDetail.Where(e=>e.WasteReceiveDetailId==itm.Id&&e.Invid==id).ToList();
			}

			var imageDetail = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == id && x.Invtype == "Waste Receiving");
			if (existing != null)
			{
				existing.ImagePath = imageDetail?.ImagePath;
			}

			var Vm = new PurchaseVm()
			{
				Vehiclelist = _context.Vehicle.Where(e => e.IsDelete == 0).ToList(),
				Arealist = _context.Area.Where(e => e.IsDelete == 0).ToList(),
				WasteReceiveDetaillist =details,
				RawWastageDetaillist=_context.RawWastageDetail.Where(e=>e.Invid==id).ToList(),
				WasteReceived = existing,
				Employeelist = _repo.GetList<Employee>("Select Id,name From Employee WHERE IsDelete=0 Order by Id").ToList(),
				Supplierlist = _repo.GetList<Supplier>("Select Id,name From Supplier WHERE IsDelete=0 Order by Id").ToList(),
				Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 3).ToList(),
			};
			return View("Create", Vm);
		}

		[HttpGet]
		public IActionResult Delete(int id)
		{
			var existing = _context.WasteReceivedMaster.FirstOrDefault(e => e.Id == id);
			if (existing == null)
			{
				TempData["error"] = "Record Not Found!!";
				return RedirectToAction("Index");
			}

			try
			{
				// ====================== BUILD LOG BEFORE DELETING ======================
				string itemdetail = "";
				string wastedetail = "";

				// Get Main Received Items + Sub Details for logging
				var mainDetails = _context.WasteReceiveDetail
					.Where(d => d.Invid == id)
					.ToList();

				foreach (var itm in mainDetails)
				{
					string itemname = _context.Product.Where(e => e.Id == itm.Pid)
										.Select(e => e.Name).FirstOrDefault() ?? "N/A";
					string suppname = _context.Supplier.Where(e => e.Id == itm.Suppid)
										.Select(e => e.Name).FirstOrDefault() ?? "N/A";

					string subDetailLog = "";

					var subDetails = _context.WasteReceiveSubDetail
						.Where(s => s.WasteReceiveDetailId == itm.Id || s.Invid == id)
						.ToList();

					foreach (var sub in subDetails)
					{
						string subSuppName = _context.Supplier.Where(e => e.Id == sub.SubSuppid)
												.Select(e => e.Name).FirstOrDefault() ?? "N/A";
						subDetailLog += $"{subSuppName}-{sub.SubSuppid}:{sub.Weight}kg, ";
					}

					itemdetail += $"Item:{itm.Pid}-{itemname} || Weight:{itm.Weight} || Supplier:{itm.Suppid}-{suppname}";
					if (!string.IsNullOrEmpty(subDetailLog))
						itemdetail += $" || SubDetail==>{subDetailLog.TrimEnd(',', ' ')}";
					itemdetail += " || ";
				}

				// Get Wastage Items for logging
				var wastageDetails = _context.RawWastageDetail
					.Where(d => d.Invid == id)
					.ToList();

				foreach (var itm in wastageDetails)
				{
					string pname = _context.Product.Where(e => e.Id == itm.Pid)
									.Select(e => e.Name).FirstOrDefault() ?? "";
					wastedetail += $"Item:{itm.Pid}-{pname} || Weight:{itm.Weight} || ";
				}

				string source = existing.Source == 0 ? "Route" : "Sended By Supplier";

				// ====================== SAVE DELETE LOG ======================
				_userlog.SaveHistory("Waste Receiving", "Delete",
					$"Id:{existing.Id}, Date:{existing.Date:yyyy-MM-dd}, " +
					$"Source:{source}, Note:{existing.Note ?? ""}, " +
					$"Gross Weight:{existing.GrossWeight}, Waste Weight:{existing.WasteWeight}, " +
					$"Net Weight:{existing.NetWeight} || Items ==>{itemdetail} || Wastage==>{wastedetail} ");

				// ====================== DELETE RECORDS ======================
				_context.WasteReceivedMaster.Where(e => e.Id == id).ExecuteUpdate(e => e.SetProperty(e => e.IsDelete,1));

				_context.SaveChanges();

				TempData["save"] = "Record Deleted Successfully!!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error deleting Waste Receiving ID: {id}");
				_userlog.SaveHistory("Waste Receiving", "Delete Error", $"Error deleting ID {id}: {ex.Message}");
				TempData["error"] = "An error occurred while deleting the record.";
				return RedirectToAction("Index");
			}
		}




	}
}
