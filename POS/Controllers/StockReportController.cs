using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
	public class StockReportController : Controller
	{
		private readonly DateTime _dateTime;
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		public StockReportController(UserLog userLog, IInterface @interface, ApplicationDbContext applicationDbContext)
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
				reportVm.StockReportfilter ??= new StockReport.Filter { From = _dateTime, To = _dateTime };
				reportVm.Productlist = _repo.GetList<Product>("Select Id,Name,CategoryId From Product WHERE IsDelete=0 and Categoryid=4").ToList();
				reportVm.Customerlist = _repo.GetList<Customer>("Select Id,Name From Customer WHERE IsDelete=0  ").ToList();
				var list = new List<StockReport.RawStockReport>();
				if (reportVm.StockReportfilter.IsSearch)
				{
					string query = @"Select Id,Name, (popn+FromPurchase-FromProduction+FromReceived  ) AS Opn, Purchase,Production,Received,
(popn+Purchase+FromPurchase-Production-FromProduction+Received+FromReceived  ) AS Bal
FROM (
Select  p.Name,p.Id,iSNULL(p.opn,0) as popn,
( Select ISNULL(SUM(dt.qty),0) FROM PurchaseDetail as dt INNER JOIN Purchase as pt on pt.id=dt.PurchaseId WHERE dt.Pid=p.id and pt.Date BETWEEN @from and @to ) as Purchase,
( Select ISNULL(SUM(dt.qty),0) FROM PurchaseDetail as dt INNER JOIN Purchase as pt on pt.id=dt.PurchaseId WHERE dt.Pid=p.id and pt.Date < @from ) as FromPurchase,
( Select ISNULL(SUM(dt.weight),0) FROM RawDetails as dt INNER JOIN Production as pt on pt.id=dt.masterId WHERE dt.productId=p.id and pt.Date BETWEEN @from and @to ) as Production,
( Select ISNULL(SUM(dt.weight),0) FROM RawDetails as dt INNER JOIN Production as pt on pt.id=dt.masterId WHERE dt.productId=p.id and pt.Date < @from  ) as FromProduction,
( Select ISNULL(SUM(dt.weight),0) FROM WasteReceiveSubDetail as dt INNER JOIN WasteReceivedMaster as pt on pt.id=dt.Invid WHERE dt.Pid=p.id and pt.Date BETWEEN @from and @to ) as Received,
( Select ISNULL(SUM(dt.weight),0) FROM WasteReceiveSubDetail as dt INNER JOIN WasteReceivedMaster as pt on pt.id=dt.Invid WHERE dt.Pid=p.id and pt.Date < @from  ) as FromReceived
FROM  Product as P WHERE p.CategoryId=3 
) as b order by name";
					list = _repo.GetList<StockReport.RawStockReport>(query, new { from = reportVm.StockReportfilter.From.ToString("yyyy-MM-dd"), to = reportVm.StockReportfilter.To.ToString("yyyy-MM-dd") }).ToList();
				}

				reportVm.RawStockReportlist = list;
			}
			catch (Exception ex)
			{
				TempData["error"] = "An error occured while fetching report.";
				_userlog.SaveHistory("Stock Report", "Get", "Error coming in Stock report.");
				_repo.LogErrorToFile(ex, $"Error coming in getting Stock report:{ex.Message.ToString()}");
			}
			return View(reportVm);
		}
		public IActionResult Index1(ReportVm reportVm)
		{
			try
			{
				reportVm.StockReportfilter ??= new StockReport.Filter { From = _dateTime, To = _dateTime };
				reportVm.Productlist = _repo.GetList<Product>("Select Id,Name,CategoryId From Product WHERE IsDelete=0 and Categoryid=4").ToList();
				reportVm.Customerlist = _repo.GetList<Customer>("Select Id,Name From Customer WHERE IsDelete=0  ").ToList();
				var list = new List<StockReport.FinishStockReport>();
				if (reportVm.StockReportfilter.IsSearch)
				{
					string query = @"Select Id,Name, (popn+FromPurchase+FromProduction-FromSale  ) AS Opn, Purchase,Production,Sale,
(popn+Purchase+FromPurchase+Production+FromProduction-Sale-FromSale ) AS Bal
FROM (
Select  p.Name,p.Id,iSNULL(p.opn,0) as popn,
( Select ISNULL(SUM(dt.qty),0) FROM FinishPurchaseDetail as dt INNER JOIN FinishPurchaseMaster as pt on pt.id=dt.PurchaseId WHERE dt.Pid=p.id and pt.Date BETWEEN @from and @to ) as Purchase,
( Select ISNULL(SUM(dt.qty),0) FROM FinishPurchaseDetail as dt INNER JOIN FinishPurchaseMaster as pt on pt.id=dt.PurchaseId WHERE dt.Pid=p.id and pt.Date < @from ) as FromPurchase,
( Select ISNULL(SUM(dt.weight),0) FROM FinishDetails as dt INNER JOIN Production as pt on pt.id=dt.masterId WHERE dt.productId=p.id and pt.Date BETWEEN @from and @to ) as Production,
( Select ISNULL(SUM(dt.weight),0) FROM FinishDetails as dt INNER JOIN Production as pt on pt.id=dt.masterId WHERE dt.productId=p.id and pt.Date < @from  ) as FromProduction,
( Select ISNULL(SUM(dt.Qty),0) FROM SaleDetail as dt INNER JOIN Sale as pt on pt.id=dt.Invid WHERE dt.Pid=p.id and pt.Date BETWEEN @from and @to ) as Sale,
( Select ISNULL(SUM(dt.Qty),0) FROM SaleDetail as dt INNER JOIN Sale as pt on pt.id=dt.Invid WHERE dt.Pid=p.id and pt.Date < @from  ) as FromSale
FROM  Product as P WHERE p.CategoryId=4 
) as b order by name";
					list = _repo.GetList<StockReport.FinishStockReport>(query, new { from = reportVm.StockReportfilter.From.ToString("yyyy-MM-dd"), to = reportVm.StockReportfilter.To.ToString("yyyy-MM-dd") }).ToList();
				}
				reportVm.FinishStockReportlist = list;
			}
			catch (Exception ex)
			{
				TempData["error"] = "An error occured while fetching report.";
				_userlog.SaveHistory("Stock Report", "Get", "Error coming in Stock report.");
				_repo.LogErrorToFile(ex, $"Error coming in getting Stock report:{ex.Message.ToString()}");
			}
			return View(reportVm);
		}
	}
}
