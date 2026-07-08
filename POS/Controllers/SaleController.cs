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
	public class SaleController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly DateTime _dateTime;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;
		public SaleController(UserLog userLog, ApplicationDbContext applicationDbContext, IInterface @interface, IWebHostEnvironment webHostEnvironment)
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
			var list = _repo.GetList<Sale>(@"
SELECT 
    w.Id,
    w.Date,
    w.Nettotal,
    c.Name AS CustName,

    STUFF(
    (
        SELECT '<br/>' + CAST(ROW_NUMBER() OVER(ORDER BY sd2.Id) AS VARCHAR(10)) + '. ' + p2.Name
        FROM SaleDetail sd2
        INNER JOIN Product p2 ON p2.Id = sd2.Pid
        WHERE sd2.InvId = w.Id
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS ItemNames,

    STUFF(
    (
        SELECT '<br/>' + CAST(sd2.Price AS VARCHAR(50))
        FROM SaleDetail sd2
        WHERE sd2.InvId = w.Id
        ORDER BY sd2.Id
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Prices,

    STUFF(
    (
        SELECT '<br/>' + CAST(sd2.Qty AS VARCHAR(50))
        FROM SaleDetail sd2
        WHERE sd2.InvId = w.Id
        ORDER BY sd2.Id
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Qty

FROM Sale w
LEFT JOIN Customer c
    ON c.Id = w.CustomerId
WHERE
    w.IsDelete = 0
    AND w.Date BETWEEN @start AND @end
ORDER BY
    w.Date,
    w.Id DESC",
			new
			{
				start = startdate.ToString("yyyy-MM-dd"),
				end = enddate.ToString("yyyy-MM-dd")
			}).ToList(); 
			ViewBag.s_date = startdate.ToString("yyyy-MM-dd");
			ViewBag.e_date = enddate.ToString("yyyy-MM-dd");
			return View(list);
		}
		public IActionResult Create(Sale sale)
		{
			sale.Date = _dateTime;
			var vm = new SaleVm()
			{
				Sale = sale,
				Customerlist = _repo.GetList<Customer>("Select Id,name From Customer where IsDelete=0 Order By Id").ToList(),
				Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 4).ToList()
			};
			return View(vm);
		}
		[HttpPost]
		public async Task<IActionResult> Save(SaleVm saleVm)
		{
			try
			{
				string msg, action, itemdetail = "";
				bool isNew = saleVm.Sale.Id == 0;
				if (isNew)
				{
					if (saleVm.Sale.InvType == "Cash")
					{
						saleVm.Sale.Received = saleVm.Sale.Nettotal;
						saleVm.Sale.Rem = 0;
					}
					else
					{
						saleVm.Sale.Rem = saleVm.Sale.Nettotal;
						saleVm.Sale.Received = 0;
					}
					saleVm.Sale.Note = saleVm.Sale.Note ?? "";
					saleVm.Sale.CreatedAt = _dateTime;
					saleVm.Sale.CreatedBy = _userId;
					saleVm.Sale.UpdatedBy = 0;
					saleVm.Sale.CodeId = "";
					_context.Sale.Add(saleVm.Sale);
					msg = "Sale Inv Saved Successfully!!";
					action = "New";
				}
				else
				{
					var existingsale = _context.Sale.FirstOrDefault(e => e.Id == saleVm.Sale.Id);
					if (existingsale == null)
					{
						TempData["error"] = "Sale Not Found!!";
						return RedirectToAction("Index");
					}
					if (saleVm.Sale.InvType == "Cash")
					{
						existingsale.Received = saleVm.Sale.Nettotal;
						existingsale.Rem = 0;
					}
					else
					{
						existingsale.Rem = saleVm.Sale.Nettotal;
						existingsale.Received = 0;
					}
					existingsale.Note = saleVm.Sale.Note ?? "";
					existingsale.Gross = saleVm.Sale.Gross;
					existingsale.Nettotal = saleVm.Sale.Nettotal;
					existingsale.Customerid = saleVm.Sale.Customerid;
					existingsale.Discount = saleVm.Sale.Discount;
					existingsale.UpdatedBy = _userId;
					existingsale.InvType = saleVm.Sale.InvType;
					existingsale.UpdatedAt = _dateTime;
					existingsale.Date = saleVm.Sale.Date;
					msg = "Sale Inv Updated Successfully!!";
					action = "Edit";
					_context.Database.ExecuteSqlRaw("DELETE FROM TransactionDetail WHERE InvType='SINV' and InvNo=@p0", saleVm.Sale.Id);
					_context.Database.ExecuteSqlRaw("DELETE FROM SaleDetail WHERE  Invid=@p0", saleVm.Sale.Id);
				}
				_context.SaveChanges();

				if (saleVm.Sale.ImageFile != null && saleVm.Sale.ImageFile.Length > 0)
				{
					string folder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

					if (!Directory.Exists(folder))
						Directory.CreateDirectory(folder);

					if (!isNew)
					{
						var existingImage = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == saleVm.Sale.Id && x.Invtype == "Sale");
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

					string extension = Path.GetExtension(saleVm.Sale.ImageFile.FileName);
					string fileName = $"Sale_{saleVm.Sale.Id}{extension}";
					string filePath = Path.Combine(folder, fileName);

					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						await saleVm.Sale.ImageFile.CopyToAsync(stream);
					}

					_context.ImagesDetail.Add(new ImagesDetail
					{
						ImagePath = "/Images/" + fileName,
						Recordid = saleVm.Sale.Id,
						Invtype = "Sale"
					});

					await _context.SaveChangesAsync();
				}
				foreach (var itm in saleVm.SaleDetaillist)
				{
					itm.InvId = saleVm.Sale.Id;
					_context.SaleDetail.Add(itm);
					string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "";
					itemdetail += $"Item:{itm.Pid + "-" + itemname} ||Qty:{itm.Qty} ||Price:{itm.Price} ||Total:{itm.Total} ";
				}
				//-- Accounting Transactions --
				var transactionlists = new List<TransactionDetail>();
				int tranid = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(TransId),0)+1 FROM TransactionDetail");
				int voucherno = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(VoucherNo),0)+1 FROM TransactionDetail");
				int custacc = _repo.GetSingleValue<int>("Select ISNULL(AccountNo,0)as acc From Account WHERE subhead=3 and Cid=@id ", new { id = saleVm.Sale.Customerid });
				// Stock Cr
				//transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno, Detail = $"Raw Items Stock Purchased against Inv:{saleVm.Sale.Id}", Accountno = 1000002, Date = saleVm.Sale.Date, Datetime = _dateTime, Dr = saleVm.Sale.Nettotal, Cr = 0, InvType = "SINV", VType = "SV", InvNo = saleVm.Sale.Id });
				// Customer Dr
				transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno, Detail = $"Items Sold against Inv:{saleVm.Sale.Id}", Accountno = custacc, Date = saleVm.Sale.Date, Datetime = _dateTime, Dr = saleVm.Sale.Nettotal, Cr = 0, InvType = "SINV", VType = "SV", InvNo = saleVm.Sale.Id });

				if (saleVm.Sale.InvType == "Cash")
				{
					// Customer Cr
					transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno + 1, Detail = $"Cash received against sale Inv:{saleVm.Sale.Id}", Accountno = custacc, Date = saleVm.Sale.Date, Datetime = _dateTime, Dr = 0, Cr = saleVm.Sale.Nettotal, InvType = "SINV", VType = "CRV", InvNo = saleVm.Sale.Id });
					// Cash Dr
					transactionlists.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno + 1, Detail = $"Cash received against sale Inv:{saleVm.Sale.Id}", Accountno = 1000001, Date = saleVm.Sale.Date, Datetime = _dateTime, Dr = saleVm.Sale.Nettotal, Cr = 0, InvType = "SINV", VType = "CRV", InvNo = saleVm.Sale.Id });
				}
				_context.TransactionDetail.AddRange(transactionlists);
				_context.SaveChanges();

				string suppname = _context.Customer.Where(e => e.Id == saleVm.Sale.Customerid).Select(e => e.Name).FirstOrDefault() ?? "";
				_userlog.SaveHistory("Sale", action, $"Id:{saleVm.Sale.Id} , Date:{saleVm.Sale.Date.ToString("yyyy-MM-dd")} ,Customer:{saleVm.Sale.Customerid + "-" + suppname}, Note:{saleVm.Sale.Note}, Gross:{saleVm.Sale.Gross}, Discount:{saleVm.Sale.Discount}, Net:{saleVm.Sale.Nettotal} || Items ==>{itemdetail}");
				TempData["save"] = msg;
				return RedirectToAction(actionName: "Index");
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error in Sale ID: {saleVm.Sale.Id}");
				_userlog.SaveHistory("Sale", "Error", $"Error: {ex.Message.ToString()}");
				TempData["error"] = "An error occurred while making transaction.";
				saleVm.Customerlist = _repo.GetList<Customer>("Select Id,name From Customer where IsDelete=0 Order By Id").ToList();
				saleVm.Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 4).ToList();
				return View("Create", saleVm);
			}
		}
		public IActionResult Edit(int id)
		{
			var sale = _context.Sale.FirstOrDefault(e => e.Id == id);
			if (sale == null)
			{
				TempData["error"] = "Sale Not Found!!";
				return RedirectToAction("Index");
			}

			var imageDetail = _context.ImagesDetail.FirstOrDefault(x => x.Recordid == id && x.Invtype == "Sale");
			if (sale != null)
			{
				sale.ImagePath = imageDetail?.ImagePath;
			}

			var Vm = new SaleVm()
			{
				SaleDetaillist = _context.SaleDetail.Where(e => e.InvId == id).ToList(),
				Sale = sale,
				Customerlist = _repo.GetList<Customer>("Select Id,name From Customer where IsDelete=0 Order By Id").ToList(),
				Productlist = _context.Product.Where(e => e.IsDelete == 0 && e.CategoryId == 4).ToList()
			};
			return View("Create", Vm);
		}
		public IActionResult Delete(int id)
		{
			var existingsale = _context.Sale.FirstOrDefault(e => e.Id == id);
			if (existingsale == null)
			{
				TempData["error"] = "Sale Not Found!!";
				return RedirectToAction("Index");
			}
			string itemdetail = "";
			var details = _context.SaleDetail.Where(e => e.InvId == id).ToList();
			foreach (var itm in details)
			{
				string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "";
				itemdetail += $"Item:{itm.Pid + "-" + itemname} ||Qty:{itm.Qty} ||Price:{itm.Price} ||Total:{itm.Total} ";
			}
			string suppname = _context.Customer.Where(e => e.Id == existingsale.Customerid).Select(e => e.Name).FirstOrDefault() ?? "";
			_userlog.SaveHistory("Sale", "Delete", $"Id:{existingsale.Id} , Date:{existingsale.Date.ToString("yyyy-MM-dd")} ,Customer:{existingsale.Customerid + "-" + suppname}, Note:{existingsale.Note}, Gross:{existingsale.Gross}, Discount:{existingsale.Discount}, Net:{existingsale.Nettotal} || Items ==>{itemdetail}");
			_context.Database.ExecuteSqlRaw("DELETE FROM TransactionDetail WHERE InvType='SINV' and InvNo=@p0", id);
			_context.Sale.Where(e => e.Id == id).ExecuteUpdate(e => e.SetProperty(e => e.IsDelete, 1));
			return RedirectToAction("Index");
		}

	}
}
