using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;
using System;

namespace POS.Controllers
{
	public class SimplePurchaseController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly DateTime _dateTime;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;
		public SimplePurchaseController(UserLog userLog, ApplicationDbContext applicationDbContext, IInterface @interface, IWebHostEnvironment webHostEnvironment)
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
            FROM PurchaseDetail pd
            INNER JOIN Product pr2 ON pr2.Id = pd.Pid
            WHERE pd.PurchaseId = p.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Itemdetails,
    STUFF(
        (
            SELECT '<br/>' + CAST(pd.Price AS VARCHAR(50))
            FROM PurchaseDetail pd
            WHERE pd.PurchaseId = p.Id
            ORDER BY pd.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Prices,
    STUFF(
        (
            SELECT '<br/>' + CAST(pd.Qty AS VARCHAR(50))
            FROM PurchaseDetail pd
            WHERE pd.PurchaseId = p.Id
            ORDER BY pd.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Qtys
FROM Purchase p 
LEFT JOIN Supplier s ON s.Id = p.Suppid 
WHERE p.Invoicetype = 0 
  AND p.Isdelete = 0 
  AND p.Date BETWEEN @start AND @end 
ORDER BY p.Date, p.Id DESC", new { start = startdate.ToString("yyyy-MM-dd"), end = enddate.ToString("yyyy-MM-dd") }).ToList();
			ViewBag.s_date = startdate.ToString("yyyy-MM-dd");
			ViewBag.e_date = enddate.ToString("yyyy-MM-dd");
			return View(list);
		}
		public IActionResult Create(Purchase purchase)
		{
			purchase.Date = _dateTime;
			var Vm = new PurchaseVm()
			{
				Purchase = purchase,
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
				string msg, action,itemdetail = "";
				bool isNew = purchaseVm.Purchase.Id == 0;
				if (isNew)
				{
					if (purchaseVm.Purchase.InvType == "Cash")
					{
						purchaseVm.Purchase.Paid = purchaseVm.Purchase.Nettotal;
						purchaseVm.Purchase.Rem = 0;
					}
					else
					{
						purchaseVm.Purchase.Rem = purchaseVm.Purchase.Nettotal;
						purchaseVm.Purchase.Paid = 0;
					}
					purchaseVm.Purchase.Note = purchaseVm.Purchase.Note ?? "";
					purchaseVm.Purchase.Invoicetype = 0;
					purchaseVm.Purchase.CreatedAt = _dateTime;
					purchaseVm.Purchase.CreatedBy = _userId;
					purchaseVm.Purchase.UpdatedBy = 0;
					purchaseVm.Purchase.CodeId = "";
					purchaseVm.Purchase.Detail = "";
					_context.Purchase.Add(purchaseVm.Purchase);
					_context.SaveChanges();

					if (purchaseVm.Purchase.ImageFile != null && purchaseVm.Purchase.ImageFile.Length > 0)
					{
						string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);

						string extension = Path.GetExtension(purchaseVm.Purchase.ImageFile.FileName);
						string fileName = $"Purchase_{purchaseVm.Purchase.Id}{extension}";
						string filePath = Path.Combine(folder, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await purchaseVm.Purchase.ImageFile.CopyToAsync(stream);
						}

						_context.ImagesDetail.Add(new ImagesDetail
						{
							ImagePath = "/Images/" + fileName,
							Recordid = purchaseVm.Purchase.Id,
							Invtype = "Raw Purchase"
						});

						await _context.SaveChangesAsync();
					}

					msg = "Purchase Inv Saved Successfully!!";
					action = "New";
				}
				else
				{
					var existingPurchase = _context.Purchase.FirstOrDefault(e => e.Id == purchaseVm.Purchase.Id && e.Invoicetype == 0);
					if (existingPurchase == null)
					{
						TempData["error"] = "Purchase Not Found!!";
						return RedirectToAction("Index");
					}
					if (purchaseVm.Purchase.InvType == "Cash")
					{
						existingPurchase.Paid = purchaseVm.Purchase.Nettotal;
						existingPurchase.Rem = 0;
					}
					else
					{
						existingPurchase.Rem = purchaseVm.Purchase.Nettotal;
						existingPurchase.Paid = 0;
					}
					existingPurchase.Note = purchaseVm.Purchase.Note ?? "";
					existingPurchase.Invoicetype = 0;
					existingPurchase.Grosstotal = purchaseVm.Purchase.Grosstotal;
					existingPurchase.Nettotal = purchaseVm.Purchase.Nettotal;
					existingPurchase.SuppId = purchaseVm.Purchase.SuppId;
					existingPurchase.InvType = purchaseVm.Purchase.InvType;
					existingPurchase.Note = purchaseVm.Purchase.Note;
					existingPurchase.Discount = purchaseVm.Purchase.Discount;
					existingPurchase.UpdatedBy = _userId;
					existingPurchase.UpdatedAt = _dateTime;
					existingPurchase.Date = purchaseVm.Purchase.Date;

					if (purchaseVm.Purchase.ImageFile != null && purchaseVm.Purchase.ImageFile.Length > 0)
					{
						string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);

						var existingImage = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == purchaseVm.Purchase.Id && x.Invtype == "Raw Purchase");
						if (existingImage != null)
						{
							string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingImage.ImagePath.TrimStart('/'));
							if (System.IO.File.Exists(oldFilePath))
							{
								System.IO.File.Delete(oldFilePath);
							}
							_context.ImagesDetail.Remove(existingImage);
						}

						string extension = Path.GetExtension(purchaseVm.Purchase.ImageFile.FileName);
						string fileName = $"Purchase_{purchaseVm.Purchase.Id}{extension}";
						string filePath = Path.Combine(folder, fileName);

						using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await purchaseVm.Purchase.ImageFile.CopyToAsync(stream);
						}

						_context.ImagesDetail.Add(new ImagesDetail
						{
							ImagePath = "/Images/" + fileName,
							Recordid = purchaseVm.Purchase.Id,
							Invtype = "Raw Purchase"
						});

						await _context.SaveChangesAsync();
					}

					msg = "Purchase Inv Updated Successfully!!";
					action = "Edit";
					_context.Database.ExecuteSqlRaw("DELETE FROM TransactionDetail WHERE InvType='PINV' and InvNo=@p0", purchaseVm.Purchase.Id);
					_context.Database.ExecuteSqlRaw("DELETE FROM PurchaseDetail WHERE Invoicetype=0 and Purchaseid=@p0", purchaseVm.Purchase.Id);
				}
				_context.SaveChanges();
				foreach (var itm in purchaseVm.PurchaseDetaillist)
				{
					itm.Suppid = 0;
					itm.Invoicetype = 0;
					itm.PurchaseId = purchaseVm.Purchase.Id;
					_context.PurchaseDetail.Add(itm);
					string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault()??"";
					itemdetail += $"Item:{itm.Pid + "-" + itemname} ||Qty:{itm.Qty} ||Price:{itm.Price} ||Total:{itm.Total} ";
				}
				//-- Accounting Transactions --
				var transactionlists = new List<TransactionDetail>();
				int tranid = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(TransId),0)+1 FROM TransactionDetail");
				int voucherno = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(VoucherNo),0)+1 FROM TransactionDetail");
				int suppacc = _repo.GetSingleValue<int>("Select ISNULL(AccountNo,0)as acc From Account WHERE subhead=7 and Cid=@id ", new { id = purchaseVm.Purchase.SuppId });
				// Stock Dr
				transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno, Detail = $"Raw Items Stock Purchased against Inv:{purchaseVm.Purchase.Id}", Accountno = 1000002, Date = purchaseVm.Purchase.Date, Datetime = _dateTime, Dr = purchaseVm.Purchase.Nettotal, Cr = 0, InvType = "PINV", VType = "PV", InvNo = purchaseVm.Purchase.Id });
				// Supplier Cr
				transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno, Detail = $"Raw Items Stock Purchased against Inv:{purchaseVm.Purchase.Id}", Accountno = suppacc, Date = purchaseVm.Purchase.Date, Datetime = _dateTime, Dr = 0, Cr = purchaseVm.Purchase.Nettotal, InvType = "PINV", VType = "PV", InvNo = purchaseVm.Purchase.Id });

				if (purchaseVm.Purchase.InvType == "Cash")
				{ 
					// Supplier Dr
					transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno + 1, Detail = $"Cash paid against purchase Inv:{purchaseVm.Purchase.Id}", Accountno = suppacc, Date = purchaseVm.Purchase.Date, Datetime = _dateTime, Dr = purchaseVm.Purchase.Nettotal, Cr = 0, InvType = "PINV", VType = "CPV", InvNo = purchaseVm.Purchase.Id });
					// Cash Cr
					transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno + 1, Detail = $"Cash paid against purchase Inv:{purchaseVm.Purchase.Id}", Accountno = 1000001, Date = purchaseVm.Purchase.Date, Datetime = _dateTime, Dr = 0, Cr = purchaseVm.Purchase.Nettotal, InvType = "PINV", VType = "CPV", InvNo = purchaseVm.Purchase.Id });
				}
				_context.TransactionDetail.AddRange(transactionlists);
				_context.SaveChanges();

				string suppname = _context.Supplier.Where(e => e.Id == purchaseVm.Purchase.SuppId).Select(e => e.Name).FirstOrDefault() ?? "";
				_userlog.SaveHistory("Purchase", action, $"Id:{purchaseVm.Purchase.Id} , Date:{purchaseVm.Purchase.Date.ToString("yyyy-MM-dd")} ,Supplier:{purchaseVm.Purchase.SuppId + "-" + suppname}, Note:{purchaseVm.Purchase.Note}, Gross:{purchaseVm.Purchase.Grosstotal}, Discount:{purchaseVm.Purchase.Discount}, Net:{purchaseVm.Purchase.Nettotal} || Items ==>{itemdetail}");
				TempData["save"] = msg;
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error in Purchase ID: {purchaseVm.Purchase.Id}");
				_userlog.SaveHistory("Purchase", "Error", $"Error: {ex.Message.ToString()}");
				TempData["error"] = "An error occurred while making transaction.";
				purchaseVm.Supplierlist = _repo.GetList<Supplier>("Select Id,name From Supplier WHERE IsDelete=0 Order by Id").ToList();
				purchaseVm.Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 3).ToList();
				return View("Create", purchaseVm);
			}
		}
		public IActionResult Edit(int id)
		{
			var purchase = _context.Purchase.FirstOrDefault(e => e.Id == id && e.Invoicetype == 0);
			if (purchase == null)
			{
				TempData["error"] = "Purchase Not Found!!";
				return RedirectToAction("Index");
			}

			var imageDetail = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == id && x.Invtype == "Raw Purchase");
			if (purchase != null)
			{
				purchase.ImagePath = imageDetail?.ImagePath;
			}

			var Vm = new PurchaseVm()
			{
				PurchaseDetaillist = _context.PurchaseDetail.Where(e => e.PurchaseId == id && e.Invoicetype == 0).ToList(),
				Purchase = purchase,
				Supplierlist = _repo.GetList<Supplier>("Select Id,name From Supplier WHERE IsDelete=0 Order by Id").ToList(),
				Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 3).ToList(),
			};
			return View("Create", Vm);
		}
		public IActionResult Delete(int id)
		{
			var purchase = _context.Purchase.FirstOrDefault(e => e.Id == id && e.Invoicetype == 0);
			if (purchase == null)
			{
				TempData["error"] = "Purchase Not Found!!";
				return RedirectToAction("Index");
			}
			string itemdetail = "";
			var purchaseDetails = _context.PurchaseDetail.Where(e => e.PurchaseId == id && e.Invoicetype == 0).ToList();
			if (purchaseDetails != null && purchaseDetails.Any())
			{
				foreach (var itm in purchaseDetails)
				{
					string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "N/A";
					itemdetail += $"Item:{itm.Pid}-{itemname} || Qty:{itm.Qty} || Price:{itm.Price} || Total:{itm.Total} | ";
				}
			}
			purchase.IsDelete = 1;
			_context.SaveChanges();
			_context.Database.ExecuteSqlRaw("DELETE FROM TransactionDetail WHERE InvType='PINV' and InvNo=@p0", id);
			string suppname = _context.Supplier.Where(e => e.Id == purchase.SuppId).Select(e => e.Name).FirstOrDefault() ?? "";
			_userlog.SaveHistory("Purchase", "Delete", $"Id:{purchase.Id} , Date:{purchase.Date.ToString("yyyy-MM-dd")} ,Supplier:{purchase.SuppId + "-" + suppname}, Note:{purchase.Note}, Gross:{purchase.Grosstotal}, Discount:{purchase.Discount}, Net:{purchase.Nettotal} || Items ==>{itemdetail}");
			return RedirectToAction("Index");
		}
	}
}
