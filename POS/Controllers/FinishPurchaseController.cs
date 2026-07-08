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
	public class FinishPurchaseController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly DateTime _dateTime;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public FinishPurchaseController(UserLog userLog, ApplicationDbContext applicationDbContext, IInterface @interface, IWebHostEnvironment webHostEnvironment)
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
			var list = _repo.GetList<Purchase>(@"
SELECT 
    p.Id,
    p.Invno,
    p.Nettotal,
    p.Date,
    p.Invtype,
    s.Name AS Suppname,
    STUFF(
        (
            SELECT '<br/>' + CAST(ROW_NUMBER() OVER(ORDER BY pd.Id) AS VARCHAR(10)) + '. ' + pr2.Name
            FROM FinishPurchaseDetail pd
            INNER JOIN Product pr2 ON pr2.Id = pd.Pid
            WHERE pd.PurchaseId = p.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Itemdetails,
    STUFF(
        (
            SELECT '<br/>' + CAST(pd.Price AS VARCHAR(50))
            FROM FinishPurchaseDetail pd
            WHERE pd.PurchaseId = p.Id
            ORDER BY pd.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Prices,
    STUFF(
        (
            SELECT '<br/>' + CAST(pd.Qty AS VARCHAR(50))
            FROM FinishPurchaseDetail pd
            WHERE pd.PurchaseId = p.Id
            ORDER BY pd.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Qtys
FROM FinishPurchaseMaster p 
LEFT JOIN Supplier s ON s.Id = p.Suppid 
WHERE p.Invoicetype = 0 
  AND p.Isdelete = 0 
  AND p.Date BETWEEN @start AND @end 
ORDER BY p.Date, p.Id DESC", new { start = startdate.ToString("yyyy-MM-dd"), end = enddate.ToString("yyyy-MM-dd") }).ToList();
			ViewBag.s_date = startdate.ToString("yyyy-MM-dd");
			ViewBag.e_date = enddate.ToString("yyyy-MM-dd");
			return View(list);
		}
		public IActionResult Create(FinishPurchaseMaster purchase)
		{
			purchase.Date = _dateTime;
			var Vm = new PurchaseVm()
			{
				FinishPurchaseMaster = purchase,
				Supplierlist = _repo.GetList<Supplier>("Select Id,name From Supplier WHERE IsDelete=0 Order by Id").ToList(),
				Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 4).ToList(),
			};
			return View(Vm);
		}
		[HttpPost]
		public async Task<IActionResult> Save(PurchaseVm purchaseVm)
		{
			try
			{
				string msg, action, itemdetail = "";
				bool isNew = purchaseVm.FinishPurchaseMaster.Id == 0;
				if (isNew)
				{
					if (purchaseVm.FinishPurchaseMaster.InvType == "Cash")
					{
						purchaseVm.FinishPurchaseMaster.Paid = purchaseVm.FinishPurchaseMaster.Nettotal;
						purchaseVm.FinishPurchaseMaster.Rem = 0;
					}
					else
					{
						purchaseVm.FinishPurchaseMaster.Rem = purchaseVm.FinishPurchaseMaster.Nettotal;
						purchaseVm.FinishPurchaseMaster.Paid = 0;
					}
					purchaseVm.FinishPurchaseMaster.Note = purchaseVm.FinishPurchaseMaster.Note ?? "";
					purchaseVm.FinishPurchaseMaster.Invoicetype = 0;
					purchaseVm.FinishPurchaseMaster.CreatedAt = _dateTime;
					purchaseVm.FinishPurchaseMaster.CreatedBy = _userId;
					purchaseVm.FinishPurchaseMaster.UpdatedBy = 0;
					purchaseVm.FinishPurchaseMaster.CodeId = "";
					purchaseVm.FinishPurchaseMaster.Detail = "";
					_context.FinishPurchaseMaster.Add(purchaseVm.FinishPurchaseMaster);
					_context.SaveChanges();

					if (purchaseVm.FinishPurchaseMaster.ImageFile != null && purchaseVm.FinishPurchaseMaster.ImageFile.Length > 0)
					{
						string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);

						string extension = Path.GetExtension(purchaseVm.FinishPurchaseMaster.ImageFile.FileName);
						string fileName = $"FinishPurchase_{purchaseVm.FinishPurchaseMaster.Id}{extension}";
						string filePath = Path.Combine(folder, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await purchaseVm.FinishPurchaseMaster.ImageFile.CopyToAsync(stream);
						}

						_context.ImagesDetail.Add(new ImagesDetail
						{
							ImagePath = "/Images/" + fileName,
							Recordid = purchaseVm.FinishPurchaseMaster.Id,
							Invtype = "Finish Purchase"
						});

						await _context.SaveChangesAsync();
					}

					msg = "Purchase Inv Saved Successfully!!";
					action = "New";
				}
				else
				{
					var existingPurchase = _context.FinishPurchaseMaster.FirstOrDefault(e => e.Id == purchaseVm.FinishPurchaseMaster.Id && e.Invoicetype == 0);
					if (existingPurchase == null)
					{
						TempData["error"] = "Purchase Not Found!!";
						return RedirectToAction("Index");
					}
					if (purchaseVm.FinishPurchaseMaster.InvType == "Cash")
					{
						existingPurchase.Paid = purchaseVm.FinishPurchaseMaster.Nettotal;
						existingPurchase.Rem = 0;
					}
					else
					{
						existingPurchase.Rem = purchaseVm.FinishPurchaseMaster.Nettotal;
						existingPurchase.Paid = 0;
					}
					existingPurchase.Note = purchaseVm.FinishPurchaseMaster.Note ?? "";
					existingPurchase.Invoicetype = 0;
					existingPurchase.Grosstotal = purchaseVm.FinishPurchaseMaster.Grosstotal;
					existingPurchase.Nettotal = purchaseVm.FinishPurchaseMaster.Nettotal;
					existingPurchase.SuppId = purchaseVm.FinishPurchaseMaster.SuppId;
					existingPurchase.InvType = purchaseVm.FinishPurchaseMaster.InvType;
					existingPurchase.Note = purchaseVm.FinishPurchaseMaster.Note;
					existingPurchase.Discount = purchaseVm.FinishPurchaseMaster.Discount;
					existingPurchase.UpdatedBy = _userId;
					existingPurchase.UpdatedAt = _dateTime;
					existingPurchase.Date = purchaseVm.FinishPurchaseMaster.Date;

					if (purchaseVm.FinishPurchaseMaster.ImageFile != null && purchaseVm.FinishPurchaseMaster.ImageFile.Length > 0)
					{
						string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);

						var existingImage = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == purchaseVm.FinishPurchaseMaster.Id && x.Invtype == "Finish Purchase");
						if (existingImage != null)
						{
							string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingImage.ImagePath.TrimStart('/'));
							if (System.IO.File.Exists(oldFilePath))
							{
								System.IO.File.Delete(oldFilePath);
							}
							_context.ImagesDetail.Remove(existingImage);
						}

						string extension = Path.GetExtension(purchaseVm.FinishPurchaseMaster.ImageFile.FileName);
						string fileName = $"FinishPurchase_{purchaseVm.FinishPurchaseMaster.Id}{extension}";
						string filePath = Path.Combine(folder, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await purchaseVm.FinishPurchaseMaster.ImageFile.CopyToAsync(stream);
						}

						_context.ImagesDetail.Add(new ImagesDetail
						{
							ImagePath = "/Images/" + fileName,
							Recordid = purchaseVm.FinishPurchaseMaster.Id,
							Invtype = "Finish Purchase"
						});

						await _context.SaveChangesAsync();
					}

					msg = "Purchase Inv Updated Successfully!!";
					action = "Edit";
					_context.Database.ExecuteSqlRaw("DELETE FROM TransactionDetail WHERE InvType='FPINV' and InvNo=@p0", purchaseVm.FinishPurchaseMaster.Id);
					_context.Database.ExecuteSqlRaw("DELETE FROM FinishPurchaseDetail WHERE Invoicetype=0 and Purchaseid=@p0", purchaseVm.FinishPurchaseMaster.Id);
				}
				_context.SaveChanges();
				if (purchaseVm.FinishPurchaseDetaillist != null&& purchaseVm.FinishPurchaseDetaillist.Count>0)
				{
					foreach (var itm in purchaseVm.FinishPurchaseDetaillist)
					{
						itm.Suppid = 0;
						itm.Invoicetype = 0;
						itm.PurchaseId = purchaseVm.FinishPurchaseMaster.Id;
						_context.FinishPurchaseDetail.Add(itm);
						string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "";
						itemdetail += $"Item:{itm.Pid + "-" + itemname} ||Weight:{itm.Qty} ||Price:{itm.Price} ||Total:{itm.Total} ";
					}
				}
				
				_context.SaveChanges();
				//-- Accounting Transactions --
				var transactionlists = new List<TransactionDetail>();
				int tranid = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(TransId),0)+1 FROM TransactionDetail");
				int voucherno = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(VoucherNo),0)+1 FROM TransactionDetail");
				int suppacc = _repo.GetSingleValue<int>("Select ISNULL(AccountNo,0)as acc From Account WHERE subhead=7 and Cid=@id ", new { id = purchaseVm.FinishPurchaseMaster.SuppId });
				// Stock Dr
				//transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno, Detail = $"Raw Items Stock Purchased against Inv:{purchaseVm.Purchase.Id}", Accountno = 1000002, Date = purchaseVm.Purchase.Date, Datetime = _dateTime, Dr = purchaseVm.Purchase.Nettotal, Cr = 0, InvType = "FPINV", VType = "FPV", InvNo = purchaseVm.Purchase.Id });
				// Supplier Cr
				transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno, Detail = $"Finish Items Stock Purchased against Inv:{purchaseVm.FinishPurchaseMaster.Id}", Accountno = suppacc, Date = purchaseVm.FinishPurchaseMaster.Date, Datetime = _dateTime, Dr = 0, Cr = purchaseVm.FinishPurchaseMaster.Nettotal, InvType = "FPINV", VType = "FPV", InvNo = purchaseVm.FinishPurchaseMaster.Id });

				if (purchaseVm.FinishPurchaseMaster.InvType == "Cash")
				{
					// Supplier Dr
					transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno + 1, Detail = $"Cash paid against purchase Inv:{purchaseVm.FinishPurchaseMaster.Id}", Accountno = suppacc, Date = purchaseVm.FinishPurchaseMaster.Date, Datetime = _dateTime, Dr = purchaseVm.FinishPurchaseMaster.Nettotal, Cr = 0, InvType = "FPINV", VType = "CPV", InvNo = purchaseVm.FinishPurchaseMaster.Id });
					// Cash Cr
					transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno + 1, Detail = $"Cash paid against purchase Inv:{purchaseVm.FinishPurchaseMaster.Id}", Accountno = 1000001, Date = purchaseVm.FinishPurchaseMaster.Date, Datetime = _dateTime, Dr = 0, Cr = purchaseVm.FinishPurchaseMaster.Nettotal, InvType = "FPINV", VType = "CPV", InvNo = purchaseVm.FinishPurchaseMaster.Id });
				}
				_context.TransactionDetail.AddRange(transactionlists);
				_context.SaveChanges();

				string suppname = _context.Supplier.Where(e => e.Id == purchaseVm.FinishPurchaseMaster.SuppId).Select(e => e.Name).FirstOrDefault() ?? "";
				_userlog.SaveHistory("Finish Purchase", action, $"Id:{purchaseVm.FinishPurchaseMaster.Id} , Date:{purchaseVm.FinishPurchaseMaster.Date.ToString("yyyy-MM-dd")} ,Supplier:{purchaseVm.FinishPurchaseMaster.SuppId + "-" + suppname}, Note:{purchaseVm.FinishPurchaseMaster.Note}, Gross:{purchaseVm.FinishPurchaseMaster.Grosstotal}, Discount:{purchaseVm.FinishPurchaseMaster.Discount}, Net:{purchaseVm.FinishPurchaseMaster.Nettotal} || Items ==>{itemdetail}");
				TempData["save"] = msg;
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error in Finish Purchase ID: {purchaseVm.FinishPurchaseMaster.Id}");
				_userlog.SaveHistory("Finish Purchase", "Error", $"Error: {ex.Message.ToString()}");
				TempData["error"] = "An error occurred while making transaction.";
				purchaseVm.Supplierlist = _repo.GetList<Supplier>("Select Id,name From Supplier WHERE IsDelete=0 Order by Id").ToList();
				purchaseVm.Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 4).ToList();
				return View("Create", purchaseVm);
			}
		}
		public IActionResult Edit(int id)
		{
			var purchase = _context.FinishPurchaseMaster.FirstOrDefault(e => e.Id == id && e.Invoicetype == 0);
			if (purchase == null)
			{
				TempData["error"] = "Purchase Not Found!!";
				return RedirectToAction("Index");
			}

			var imageDetail = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == id && x.Invtype == "Finish Purchase");
			if (purchase != null)
			{
				purchase.ImagePath = imageDetail?.ImagePath;
			}

			var Vm = new PurchaseVm()
			{
				FinishPurchaseDetaillist = _context.FinishPurchaseDetail.Where(e => e.PurchaseId == id && e.Invoicetype == 0).ToList(),
				FinishPurchaseMaster = purchase,
				Supplierlist = _repo.GetList<Supplier>("Select Id,name From Supplier WHERE IsDelete=0 Order by Id").ToList(),
				Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 4).ToList(),
			};
			return View("Create", Vm);
		}
		public IActionResult Delete(int id)
		{
			var purchase = _context.FinishPurchaseMaster.FirstOrDefault(e => e.Id == id && e.Invoicetype == 0);
			if (purchase == null)
			{
				TempData["error"] = "Purchase Not Found!!";
				return RedirectToAction("Index");
			}
			string itemdetail = "";
			var purchaseDetails = _context.FinishPurchaseDetail.Where(e => e.PurchaseId == id && e.Invoicetype == 0).ToList();
			if (purchaseDetails != null && purchaseDetails.Any())
			{
				foreach (var itm in purchaseDetails)
				{
					string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "N/A";
					itemdetail += $"Item:{itm.Pid}-{itemname} || Weight:{itm.Qty} || Price:{itm.Price} || Total:{itm.Total} | ";
				}
			}
			purchase.IsDelete = 1;
			_context.SaveChanges();
			_context.Database.ExecuteSqlRaw("DELETE FROM TransactionDetail WHERE InvType='FPINV' and InvNo=@p0", id);
			string suppname = _context.Supplier.Where(e => e.Id == purchase.SuppId).Select(e => e.Name).FirstOrDefault() ?? "";
			_userlog.SaveHistory("Finish Purchase", "Delete", $"Id:{purchase.Id} , Date:{purchase.Date.ToString("yyyy-MM-dd")} ,Supplier:{purchase.SuppId + "-" + suppname}, Note:{purchase.Note}, Gross:{purchase.Grosstotal}, Discount:{purchase.Discount}, Net:{purchase.Nettotal} || Items ==>{itemdetail}");
			return RedirectToAction("Index");
		}
	}
}
