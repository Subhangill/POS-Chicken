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
	public class CashPaymentController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly DateTime _dateTime;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;
		public CashPaymentController(UserLog userLog, ApplicationDbContext applicationDbContext, IInterface @interface, IWebHostEnvironment webHostEnvironment)
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
			var list = _repo.GetList<Voucher>("Select InvId,Id,Date,Detail,Dr From Voucher WHERE Type='CPV' and isdelete=0 and Date BETWEEN @start and @end ORDER BY Date,Id DESC", new { start = startdate.ToString("yyyy-MM-dd"), end = enddate.ToString("yyyy-MM-dd") }).ToList();
			ViewBag.s_date = startdate.ToString("yyyy-MM-dd");
			ViewBag.e_date = enddate.ToString("yyyy-MM-dd");
			return View(list);
		}
		public IActionResult Create(Voucher voucher)
		{
			voucher.InvId = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(InvId),0)+1 FROM Voucher WHERE Type='CPV'"); ;

			voucher.Date = _dateTime;
			var vm = new VoucherVm()
			{
				Voucher = voucher,
				Accountlist = _repo.GetList<Account>("Select AccountNo,Name From Account WHERE HeadId in (2,5) and IsDelete=0 ORDER BY HeadId ,SubHead,AccountNo").ToList()
			};
			return View(vm);
		}
		[HttpPost]
		public async Task<IActionResult> Save(VoucherVm voucherVm)
		{
			try
			{
				string vdetails = "";
				bool isNew = voucherVm.Voucher.Id == 0;
				int vno = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(VoucherNo),0)+1 FROM TransactionDetail");
				int tran_id = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(TransId),0)+1 FROM TransactionDetail");
				if (isNew)
				{
					voucherVm.Voucher.InvId = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(InvId),0)+1 FROM Voucher WHERE Type='CPV'"); ;
					voucherVm.Voucher.CreatedAt = _dateTime;
					voucherVm.Voucher.IsDelete = 0;
					voucherVm.Voucher.CreatedBy = _userId;
					voucherVm.Voucher.Type = "CPV";
					voucherVm.Voucher.CodeId = "";
					voucherVm.Voucher.TranId = tran_id;
					voucherVm.Voucher.Detail = voucherVm.Voucher.Detail ?? "";
					voucherVm.Voucher.Vno = vno;
					_context.Voucher.Add(voucherVm.Voucher);
				}
				else
				{
					var existing = _context.Voucher.FirstOrDefault(e => e.Id == voucherVm.Voucher.Id);
					if (existing == null)
					{
						TempData["error"] = "Data not found!!";
						return RedirectToAction("Index");
					}
					_context.VoucherDetail.Where(e => e.VoucherId == voucherVm.Voucher.Id).ExecuteDelete();
					_context.TransactionDetail.Where(e => e.VType == "CPV" && e.InvNo == voucherVm.Voucher.InvId && e.TransId == voucherVm.Voucher.TranId).ExecuteDelete();
					existing.Cr = voucherVm.Voucher.Cr;
					existing.Detail = voucherVm.Voucher.Detail ?? "";
					existing.Date = voucherVm.Voucher.Date;
					existing.Dr = voucherVm.Voucher.Dr;
					existing.Vno = vno;
					existing.TranId = tran_id;
				}
				_context.SaveChanges();

				if (voucherVm.VoucherDetaillist != null && voucherVm.VoucherDetaillist.Count > 0)
				{
					foreach (var itm in voucherVm.VoucherDetaillist)
					{
						itm.Accname = _repo.GetSingleValue<string>("Select Name From Account Where Accountno=@id", new { id=itm.AccountId });
						itm.VoucherId = voucherVm.Voucher.Id;
						itm.Vtype = "CPV";
						itm.InvId = voucherVm.Voucher.InvId;
						_context.VoucherDetail.Add(itm);
						_context.TransactionDetail.Add(new TransactionDetail { Accountno = itm.AccountId, TransId = tran_id, Date = voucherVm.Voucher.Date, Datetime = _dateTime, Dr = itm.Dr, Cr = itm.Cr, Detail = itm.Detail, InvNo = voucherVm.Voucher.InvId, VoucherNo = vno, InvType = "CPV", VType = "CPV" });
						if (itm.AccountId != 1000001)
							vdetails += $"AccountId:{itm.AccountId},Account:{itm.Accname},Dr:{itm.Dr},Cr:{itm.Cr}";
					}
				}
				_context.SaveChanges();

				if (voucherVm.Voucher.ImageFile != null && voucherVm.Voucher.ImageFile.Length > 0)
				{
					string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

					if (!Directory.Exists(folder))
						Directory.CreateDirectory(folder);

					if (!isNew)
					{
						var existingImage = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == voucherVm.Voucher.Id && x.Invtype == "Cash Payment");
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

					string extension = Path.GetExtension(voucherVm.Voucher.ImageFile.FileName);
					string fileName = $"CashPayment_{voucherVm.Voucher.Id}{extension}";
					string filePath = Path.Combine(folder, fileName);

					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						await voucherVm.Voucher.ImageFile.CopyToAsync(stream);
					}

					_context.ImagesDetail.Add(new ImagesDetail
					{
						ImagePath = "/Images/" + fileName,
						Recordid = voucherVm.Voucher.Id,
						Invtype = "Cash Payment"
					});

					await _context.SaveChangesAsync();
				}
				TempData["save"] = isNew ? "Voucher saved successfully!!" : "Voucher updated successfully!!";
				_userlog.SaveHistory(form: "Cash Payment", isNew ? "New" : "Edit", $"Id:{voucherVm.Voucher.Id}, Date:{voucherVm.Voucher.Date.ToString("dd-MM-yyyy")}, Note:{voucherVm.Voucher.Detail},Total Dr:{voucherVm.Voucher.Dr},Cr:{voucherVm.Voucher.Cr} ,Details==>{vdetails}");
				return RedirectToAction(actionName: "Index");
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error in Voucher ID: {voucherVm.Voucher.Id}");
				_userlog.SaveHistory("Cash Payment", "Error", $"Error: {ex.Message.ToString()}");
				TempData["error"] = "An error occurred while making transaction.";
				voucherVm.Accountlist = _repo.GetList<Account>("Select AccountNo,Name From Account WHERE HeadId in (2,5) and IsDelete=0 ORDER BY HeadId ,SubHead,AccountNo").ToList();
				return View("Create", voucherVm);
			}
		}

		public IActionResult Edit(int id)
		{
			var existing = _context.Voucher.FirstOrDefault(e => e.Id == id);
			if (existing == null)
			{
				TempData["error"] = "Data not found!!";
				return RedirectToAction("Index");
			}

			var imageDetail = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == id && x.Invtype == "Cash Payment");
			if (existing != null)
			{
				existing.ImagePath = imageDetail?.ImagePath;
			}

			var vm = new VoucherVm()
			{
				VoucherDetaillist = _context.VoucherDetail.Where(e => e.VoucherId == id).ToList(),
				Voucher = existing,
				Accountlist = _repo.GetList<Account>("Select AccountNo,Name From Account WHERE HeadId in (2,5) and IsDelete=0 ORDER BY HeadId ,SubHead,AccountNo").ToList()
			};
			return View("Create", vm);
		}

		public IActionResult Delete(int id)
		{
			var existing = _context.Voucher.FirstOrDefault(e => e.Id == id);
			if (existing == null)
			{
				TempData["error"] = "Data not found!!";
				return RedirectToAction("Index");
			}
			string vdetails = "";
			var det = _context.VoucherDetail.Where(e => e.VoucherId == id).ToList();
			foreach (var itm in det)
			{
				itm.Accname = _repo.GetSingleValue<string>("Select Name From Account Where Accountno=@id", new { id = itm.AccountId });

				if (itm.AccountId != 1000001)
					vdetails += $"AccountId:{itm.AccountId},Account:{itm.Accname},Dr:{itm.Dr},Cr:{itm.Cr}";
			}
			_context.Voucher.Where(e=>e.Id==id).ExecuteUpdate(e => e.SetProperty(e => e.IsDelete, 1));
			_context.SaveChanges();
			_userlog.SaveHistory(form: "Cash Payment", "Delete", $"Id:{existing.Id}, Date:{existing.Date.ToString("dd-MM-yyyy")}, Note:{existing.Detail},Total Dr:{existing.Dr},Cr:{existing.Cr} ,Details==>{vdetails}");

			return RedirectToAction("Index");
		}

	}
}
