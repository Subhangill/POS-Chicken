using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
	public class SaleReportController : Controller
	{
		private readonly DateTime _dateTime;
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		public SaleReportController(UserLog userLog, IInterface @interface, ApplicationDbContext applicationDbContext)
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
				reportVm.Sreportfilter ??= new SaleReport.Filter { From = _dateTime, To = _dateTime };
				reportVm.Productlist = _repo.GetList<Product>("Select Id,Name,CategoryId From Product WHERE IsDelete=0 and Categoryid=4").ToList();
				reportVm.Customerlist = _repo.GetList<Customer>("Select Id,Name From Customer WHERE IsDelete=0  ").ToList();
				var invoicelist = new List<SaleReport.InvoiceSummary>();
				var itemdetaillist = new List<SaleReport.ItemDetail>();

				if (reportVm.Sreportfilter.IsSearch)
				{
					if (reportVm.Sreportfilter.Rtype == 0)
					{
						invoicelist = _repo.GetList<SaleReport.InvoiceSummary>("Select p.id,p.date ,s.name as Custname,p.Gross as Grosstotal,p.Discount,p.Nettotal FROM Sale as p LEFT JOIN Customer as s on s.id=p.Customerid WHERE p.Date BETWEEN @from and @to  AND (@cusId = 0 OR p.Customerid = @cusId) ORDER BY p.id,p.date", new { from = reportVm.Sreportfilter.From.ToString("yyyy-MM-dd"), to = reportVm.Sreportfilter.To.ToString("yyyy-MM-dd"), cusId = reportVm.Sreportfilter.Cid }).ToList();
					}
					else if (reportVm.Sreportfilter.Rtype == 1)
					{
						itemdetaillist = _repo.GetList<SaleReport.ItemDetail>("SELECT pr.Name AS Pname, ISNULL(SUM(pd.Qty),0) AS Qty, ISNULL(SUM(pd.Nettotal),0) AS Total, pd.Pid FROM SaleDetail pd INNER JOIN Sale p ON p.Id=pd.InvId LEFT JOIN Product pr ON pr.Id=pd.Pid WHERE p.Date BETWEEN @from AND @to AND (@Custid IS NULL OR p.Customerid=@Custid) AND (@Pid IS NULL OR pd.Pid=@Pid) GROUP BY pd.Pid, pr.Name ORDER BY pr.Name", new { from = reportVm.Sreportfilter.From.ToString("yyyy-MM-dd"), to = reportVm.Sreportfilter.To.ToString("yyyy-MM-dd"), Custid = reportVm.Sreportfilter.Cid == 0 ? (int?)null : reportVm.Sreportfilter.Cid, Pid = reportVm.Sreportfilter.Pid == 0 ? (int?)null : reportVm.Sreportfilter.Pid }).ToList();
					}
				}
				reportVm.SreportItemDetaillist = itemdetaillist;
				reportVm.SreportInvoiceSummarylist = invoicelist;
			}
			catch (Exception ex)
			{
				TempData["error"] = "An error occured while fetching report.";
				_userlog.SaveHistory("Sale Report", "Get", "Error coming in Sale report.");
				_repo.LogErrorToFile(ex, $"Error coming in getting sale report:{ex.Message.ToString()}");
			}
			return View(reportVm);
		}
	}
}
