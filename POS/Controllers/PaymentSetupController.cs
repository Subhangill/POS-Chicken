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
	public class PaymentSetupController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly DateTime _dateTime;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;

		public PaymentSetupController(UserLog userLog, ApplicationDbContext applicationDbContext, IInterface @interface)
		{
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
			var list = _repo.GetList<PaymentMaster>(@"
SELECT 
    p.Id,
    p.Date,
    p.Totalamount,
    s.Name AS Suppname,
    STUFF(
        (
            SELECT '<br/>' + CAST(ROW_NUMBER() OVER(ORDER BY pd.Id) AS VARCHAR(10)) + '. ' + pr2.Name
            FROM PaymentDetail pd
            INNER JOIN Product pr2 ON pr2.Id = pd.Pid
            WHERE pd.InvId = p.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Itemdetails,
    STUFF(
        (
            SELECT '<br/>' + CAST(pd.Rate AS VARCHAR(50))
            FROM PaymentDetail pd
            WHERE pd.InvId = p.Id
            ORDER BY pd.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Prices,
    STUFF(
        (
            SELECT '<br/>' + CAST(pd.Weight AS VARCHAR(50))
            FROM PaymentDetail pd
            WHERE pd.InvId = p.Id
            ORDER BY pd.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 5, '') AS Qtys
FROM PaymentMaster p 
LEFT JOIN Supplier s ON s.Id = p.Suppid 
WHERE  p.Isdelete = 0 
  AND p.Date BETWEEN @start AND @end 
ORDER BY p.Date, p.Id DESC", new { start = startdate.ToString("yyyy-MM-dd"), end = enddate.ToString("yyyy-MM-dd") }).ToList();
			ViewBag.s_date = startdate.ToString("yyyy-MM-dd");
			ViewBag.e_date = enddate.ToString("yyyy-MM-dd");
			return View(list);
		}

		public IActionResult Create(PaymentMaster paymentMaster)
		{
			paymentMaster.Date = _dateTime;
			var vm = new PaymentVm()
			{
				Productlist = _repo.GetList<Product>("Select Id,Name From Product Where IsDelete=0 and Categoryid=3").ToList(),
				Supplierlist = _repo.GetList<Supplier>("Select Id,Name From Supplier Where IsDelete=0").ToList(),
				PaymentMaster = paymentMaster,
			};
			return View(vm);
		}
		[HttpGet]
		public JsonResult GetReceivedWeight(int sid, int pid, string s_date, string e_date)
		{
			DateTime startdate = DateTime.Parse(s_date);
			DateTime enddate = DateTime.Parse(e_date);
			decimal weight = _repo.GetSingleValue<decimal>($"SELECT (ISNULL((SELECT SUM(sub.Weight) FROM WasteReceiveSubDetail AS sub INNER JOIN WasteReceiveDetail AS wd ON wd.Id = sub.WasteReceiveDetailId INNER JOIN WasteReceivedMaster AS wm ON wm.Id = wd.Invid WHERE wm.Date BETWEEN @start AND @end AND sub.SubSuppid = @sid AND sub.Pid = @pid),0) - ISNULL((SELECT SUM(pd.Weight) FROM PaymentDetail AS pd INNER JOIN PaymentMaster AS pm ON pm.Id = pd.InvId WHERE pd.Startdate = @start AND pd.Enddate = @end AND pm.Suppid = @sid AND pd.Pid = @pid),0)) AS RemainingWeight;", new { start = startdate.ToString("yyyy-MM-dd"), end = enddate.ToString("yyyy-MM-dd"), sid = sid, pid = pid });
			return Json(weight);
		}
		[HttpGet]
		public JsonResult GetPrevBal(int sid)
		{
			int acc = _repo.GetSingleValue<int>("Select AccountNo From Account WHERE SubHead=7 and Cid=@id", new { id = sid });
			//decimal opn
			return Json(0);
		}
		public IActionResult Print()
		{

			return View();
		}

		[HttpPost]
		public IActionResult Save(PaymentVm paymentVm)
		{
			try
			{
				string msg, action, itemdetail = "";
				bool isNew = paymentVm.PaymentMaster.Id == 0;
				if (isNew)
				{
					paymentVm.PaymentMaster.Note = paymentVm.PaymentMaster.Note ?? "";
					paymentVm.PaymentMaster.CreatedAt = _dateTime;
					paymentVm.PaymentMaster.CreatedBy = _userId;
					paymentVm.PaymentMaster.UpdatedBy = 0;
					paymentVm.PaymentMaster.CodeId = "";
					_context.PaymentMaster.Add(paymentVm.PaymentMaster);
					msg = "Payment Inv Saved Successfully!!";
					action = "New";
				}
				else
				{
					var existingPayment = _context.PaymentMaster.FirstOrDefault(e => e.Id == paymentVm.PaymentMaster.Id);
					if (existingPayment == null)
					{
						TempData["error"] = "Payment Not Found!!";
						return RedirectToAction("Index");
					}
					existingPayment.Note = paymentVm.PaymentMaster.Note ?? "";
					existingPayment.UpdatedBy = _userId;
					existingPayment.UpdatedAt = _dateTime;
					existingPayment.Date = paymentVm.PaymentMaster.Date;
					existingPayment.Suppid = paymentVm.PaymentMaster.Suppid;
					existingPayment.Grossamount = paymentVm.PaymentMaster.Grossamount;
					existingPayment.Previous = paymentVm.PaymentMaster.Previous;
					existingPayment.Totalamount = paymentVm.PaymentMaster.Totalamount;
					msg = "Payment Inv Updated Successfully!!";
					action = "Edit";
					_context.Database.ExecuteSqlRaw("DELETE FROM TransactionDetail WHERE InvType='PTINV' and InvNo=@p0", paymentVm.PaymentMaster.Id);
					_context.Database.ExecuteSqlRaw("DELETE FROM PaymentDetail WHERE InvId=@p0", paymentVm.PaymentMaster.Id);
				}
				_context.SaveChanges();
				int tranid = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(TransId),0)+1 FROM TransactionDetail");
				int voucherno = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(VoucherNo),0)+1 FROM TransactionDetail");
				int suppacc = _repo.GetSingleValue<int>("Select ISNULL(AccountNo,0)as acc From Account WHERE subhead=7 and Cid=@id ", new { id = paymentVm.PaymentMaster.Suppid });

				if (paymentVm.PaymentDetaillist != null && paymentVm.PaymentDetaillist.Count > 0)
				{
					foreach (var itm in paymentVm.PaymentDetaillist)
					{
						itm.InvId = paymentVm.PaymentMaster.Id;
						_context.PaymentDetail.Add(itm);
						string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "";
						itemdetail += $"Item:{itm.Pid + "-" + itemname} ||Weight:{itm.Weight} ||Price:{itm.Rate} ||Total:{itm.Total} || Startdate:{itm.Startdate}||Enddate:{itm.Enddate} ";
						_context.TransactionDetail.Add(new TransactionDetail { TransId = tranid, VoucherNo = voucherno, Detail = $"Amount payable for stock received against Inv:{paymentVm.PaymentMaster.Id}", Accountno = suppacc, Date = paymentVm.PaymentMaster.Date, Datetime = _dateTime, Dr = 0, Cr = paymentVm.PaymentMaster.Grossamount, InvType = "PTINV", VType = "PTV", InvNo = paymentVm.PaymentMaster.Id });
					}
					_context.SaveChanges();
				}

				string suppname = _context.Supplier.Where(e => e.Id == paymentVm.PaymentMaster.Suppid).Select(e => e.Name).FirstOrDefault() ?? "";
				_userlog.SaveHistory("Payment Detail", action, $"Id:{paymentVm.PaymentMaster.Id} , Date:{paymentVm.PaymentMaster.Date.ToString("yyyy-MM-dd")} ,Supplier:{paymentVm.PaymentMaster.Suppid + "-" + suppname}, Note:{paymentVm.PaymentMaster.Note}, Gross:{paymentVm.PaymentMaster.Grossamount}, Previous:{paymentVm.PaymentMaster.Previous}, Net:{paymentVm.PaymentMaster.Totalamount} || Items ==>{itemdetail}");
				TempData["save"] = msg;
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error in Payment Detail ID: {paymentVm.PaymentMaster.Id}");
				_userlog.SaveHistory("Payment Detail", "Error", $"Error: {ex.Message.ToString()}");
				TempData["error"] = "An error occurred while making transaction.";
				paymentVm.Productlist = _repo.GetList<Product>("Select Id,Name From Product Where IsDelete=0 and Categoryid=3").ToList();
				paymentVm.Supplierlist = _repo.GetList<Supplier>("Select Id,Name From Supplier Where IsDelete=0").ToList();
				return View("Create", paymentVm);
			}
		}
		public IActionResult Edit(int id)
		{
			var existingPayment = _context.PaymentMaster.FirstOrDefault(e => e.Id == id);
			if (existingPayment == null)
			{
				TempData["error"] = "Payment Not Found!!";
				return RedirectToAction("Index");
			}
			var vm = new PaymentVm()
			{
				PaymentDetaillist = _context.PaymentDetail.Where(e => e.InvId == id).ToList(),
				Productlist = _repo.GetList<Product>("Select Id,Name From Product Where IsDelete=0 and Categoryid=3").ToList(),
				Supplierlist = _repo.GetList<Supplier>("Select Id,Name From Supplier Where IsDelete=0").ToList(),
				PaymentMaster = existingPayment,
			};
			return View("Create", vm);
		}
		public IActionResult Delete(int id)
		{
			var existingPayment = _context.PaymentMaster.FirstOrDefault(e => e.Id == id);
			if (existingPayment == null)
			{
				TempData["error"] = "Payment Not Found!!";
				return RedirectToAction("Index");
			}
			string itemdetail = "";
			var details = _context.PaymentDetail.Where(e => e.InvId == id).ToList();
			if (details != null && details.Count > 0)
			{
				foreach (var itm in details)
				{
					string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "";
					itemdetail += $"Item:{itm.Pid + "-" + itemname} ||Weight:{itm.Weight} ||Price:{itm.Rate} ||Total:{itm.Total} || Startdate:{itm.Startdate}||Enddate:{itm.Enddate} ";
				}
			}
			_context.TransactionDetail.Where(e => e.InvNo == id && e.InvType == "PTINV").ExecuteDelete();
			string suppname = _context.Supplier.Where(e => e.Id == existingPayment.Suppid).Select(e => e.Name).FirstOrDefault() ?? "";
			existingPayment.IsDelete = 1;
			_context.SaveChanges();
			_userlog.SaveHistory("Payment Detail", "Delete", $"Id:{existingPayment.Id} , Date:{existingPayment.Date.ToString("yyyy-MM-dd")} ,Supplier:{existingPayment.Suppid + "-" + suppname}, Note:{existingPayment.Note}, Gross:{existingPayment.Grossamount}, Previous:{existingPayment.Previous}, Net:{existingPayment.Totalamount} || Items ==>{itemdetail}");
			return RedirectToAction(nameof(Index));
		}

	}
}
