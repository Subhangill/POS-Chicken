using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;
using System.Collections.Immutable;

namespace POS.Controllers
{
	public class PurchaseReportController : Controller
	{
		private readonly DateTime _dateTime;
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		public PurchaseReportController(UserLog userLog, IInterface @interface, ApplicationDbContext applicationDbContext)
		{
			_userlog = userLog;
			_repo = @interface;
			_userId = UserHelper.GetCurrentUserId();
			_context = applicationDbContext; _dateTime = AppDate.Now;
		}
		public IActionResult Index(ReportVm reportVm)
		{
			try
			{
				reportVm.Preportfilter ??= new PurchaseReport.Filter { From = _dateTime, To = _dateTime };
				reportVm.Productlist = _repo.GetList<Product>("Select Id,Name,CategoryId From Product WHERE IsDelete=0 ").ToList();
				reportVm.Supplierlist = _repo.GetList<Supplier>("Select Id,Name From Supplier WHERE IsDelete=0  ").ToList();
				var invoicelist = new List<PurchaseReport.InvoiceSummary>();
				var itemdetaillist = new List<PurchaseReport.ItemDetail>();

				if (reportVm.Preportfilter.IsSearch)
				{
					if (reportVm.Preportfilter.Ptype == 0 && reportVm.Preportfilter.Rtype == 0)
					{
						invoicelist = _repo.GetList<PurchaseReport.InvoiceSummary>("Select p.id,p.date ,s.name as suppname,p.Grosstotal,p.Discount,p.Nettotal FROM Purchase as p LEFT JOIN SUpplier as s on s.id=p.suppid WHERE p.Date BETWEEN @from and @to  AND (@SuppId = 0 OR p.suppid = @SuppId) ORDER BY p.id,p.date", new { from= reportVm.Preportfilter.From.ToString("yyyy-MM-dd"), to = reportVm.Preportfilter.To.ToString("yyyy-MM-dd"), SuppId = reportVm.Preportfilter.Sid }).ToList();
					}
					else if (reportVm.Preportfilter.Ptype == 0 && reportVm.Preportfilter.Rtype == 1)
					{
						itemdetaillist = _repo.GetList<PurchaseReport.ItemDetail>("SELECT pr.Name AS Pname, ISNULL(SUM(pd.Qty),0) AS Qty, ISNULL(SUM(pd.Nettotal),0) AS Total, pd.Pid FROM PurchaseDetail pd INNER JOIN Purchase p ON p.Id=pd.PurchaseId LEFT JOIN Product pr ON pr.Id=pd.Pid WHERE p.Date BETWEEN @from AND @to AND (@SuppId IS NULL OR p.SuppId=@SuppId) AND (@Pid IS NULL OR pd.Pid=@Pid) GROUP BY pd.Pid, pr.Name ORDER BY pr.Name", new { from = reportVm.Preportfilter.From.ToString("yyyy-MM-dd"), to = reportVm.Preportfilter.To.ToString("yyyy-MM-dd"), SuppId = reportVm.Preportfilter.Sid == 0 ? (int?)null : reportVm.Preportfilter.Sid, Pid = reportVm.Preportfilter.Pid == 0 ? (int?)null : reportVm.Preportfilter.Pid }).ToList();
					}
					else if (reportVm.Preportfilter.Ptype == 1 && reportVm.Preportfilter.Rtype == 0)
					{
						invoicelist = _repo.GetList<PurchaseReport.InvoiceSummary>("Select p.id,p.date ,s.name as suppname,p.Grosstotal,p.Discount,p.Nettotal FROM FinishPurchaseMaster as p LEFT JOIN SUpplier as s on s.id=p.suppid WHERE p.Date BETWEEN @from and @to  AND (@SuppId = 0 OR p.suppid = @SuppId) ORDER BY p.id,p.date", new { from = reportVm.Preportfilter.From.ToString("yyyy-MM-dd"), to = reportVm.Preportfilter.To.ToString("yyyy-MM-dd"), SuppId = reportVm.Preportfilter.Sid }).ToList();
					}
					else if (reportVm.Preportfilter.Ptype == 1 && reportVm.Preportfilter.Rtype == 1)
					{
						itemdetaillist = _repo.GetList<PurchaseReport.ItemDetail>("SELECT pr.Name AS Pname, ISNULL(SUM(pd.Qty),0) AS Qty, ISNULL(SUM(pd.Nettotal),0) AS Total, pd.Pid FROM FinishPurchaseDetail pd INNER JOIN FinishPurchaseMaster p ON p.Id=pd.PurchaseId LEFT JOIN Product pr ON pr.Id=pd.Pid WHERE p.Date BETWEEN @from AND @to AND (@SuppId IS NULL OR p.SuppId=@SuppId) AND (@Pid IS NULL OR pd.Pid=@Pid) GROUP BY pd.Pid, pr.Name ORDER BY pr.Name", new { from = reportVm.Preportfilter.From.ToString("yyyy-MM-dd"), to = reportVm.Preportfilter.To.ToString("yyyy-MM-dd"), SuppId = reportVm.Preportfilter.Sid == 0 ? (int?)null : reportVm.Preportfilter.Sid, Pid = reportVm.Preportfilter.Pid == 0 ? (int?)null : reportVm.Preportfilter.Pid }).ToList();
					}
				}
				reportVm.PreportItemDetaillist = itemdetaillist;
				reportVm.PreportInvoiceSummarylist = invoicelist;
			}
			catch (Exception ex)
			{
				TempData["error"] = "An error occured while fetching report.";
				_userlog.SaveHistory("Purchase Report", "Get", "Error coming in purchase report.");
				_repo.LogErrorToFile(ex, $"Error coming in getting purchase report:{ex.Message.ToString()}");
			}
			return View(reportVm);
		}

		[HttpGet]
		public JsonResult GetItems(int cid)
		{
			return Json(_repo.GetList<Product>("Select Id,Name From Product WHERE IsDelete=0 and CategoryId=@id ", new {@id=cid }).ToList());
		}



	}
}
