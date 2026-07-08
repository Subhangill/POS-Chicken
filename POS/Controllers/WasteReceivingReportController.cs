using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace POS.Controllers
{
	public class WasteReceivingReportController : Controller
	{
		private readonly DateTime _dateTime;
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		public WasteReceivingReportController(UserLog userLog, IInterface @interface, ApplicationDbContext applicationDbContext)
		{
			_userlog = userLog;
			_repo = @interface;
			_userId = UserHelper.GetCurrentUserId();
			_context = applicationDbContext;
			_dateTime = AppDate.Now;
		}
		public IActionResult Index(ReportVm reportVm)
		{
			try
			{
				string filter = "";
				reportVm.WasteReportfilter ??= new WasteReport.Filter {Source=-1, From = _dateTime, To = _dateTime };
				reportVm.Productlist = _repo.GetList<Product>("Select Id,Name,CategoryId From Product WHERE IsDelete=0 and Categoryid=3").ToList();
				reportVm.Supplierlist = _repo.GetList<Supplier>("Select Id,Name From Supplier WHERE IsDelete=0  ").ToList();
				var listitem = new List<WasteReport.ItemDetail>();
				var list = new List<WasteReport.InvoiceSummary>();
				if (reportVm.WasteReportfilter.IsSearch)
				{
					if (reportVm.WasteReportfilter.Type == 0)
					{
						list = _repo.GetList<WasteReport.InvoiceSummary>($"SELECT wd.Id,wd.GrossWeight,wd.WasteWeight, wd.NetWeight, wd.Date,wd.Source,s.name as Suppname, a.name as Areaname,emp.name as Empname FROM WasteReceivedMaster as wd LEFT JOIN Area as a on a.id=wd.Area LEFT JOIN Employee as emp on emp.id=wd.Empid LEFT JOIN Supplier as s on s.id=wd.Suppid WHERE wd.date BETWEEN @from and @to "+ filter + "", new { from = reportVm.WasteReportfilter.From.ToString("yyyy-MM-dd"), to = reportVm.WasteReportfilter.To.ToString("yyyy-MM-dd"),Source = reportVm.WasteReportfilter.Source,Sid = reportVm.WasteReportfilter.Sid}).ToList();
					}
					else
					{
						if (reportVm.WasteReportfilter.Sid > 0)
							filter += " AND wsd.SubSuppid = @Sid";

						if (reportVm.WasteReportfilter.Source != -1)
							filter += " AND wd.Source = @Source";

						if (reportVm.WasteReportfilter.Pid > 0)
							filter += " AND wsd.Pid = @Pid";

						listitem = _repo.GetList<WasteReport.ItemDetail>($"SELECT wsd.pid,p.name as Name,ISNULL(SUM(wsd.Weight),0) as Weight FROM WasteReceiveSubDetail as wsd LEFT JOIN Product as p on p.id=wsd.Pid INNER JOIN WasteReceiveDetail as wdt on wdt.id=wsd.WasteReceiveDetailId INNER JOIN WasteReceivedMaster as wd on wd.Id=wdt.Invid WHERE wd.date BETWEEN @from and @to "+ filter + " GROUP BY wsd.pid, p.name ORDER BY p.name ", new { from = reportVm.WasteReportfilter.From.ToString("yyyy-MM-dd"), to = reportVm.WasteReportfilter.To.ToString("yyyy-MM-dd"), Source = reportVm.WasteReportfilter.Source, Sid = reportVm.WasteReportfilter.Sid, pid = reportVm.WasteReportfilter.Pid }).ToList();
					}
				}

				reportVm.WreportInvoicelist = list;
				reportVm.WreportItemdetaillist = listitem;
			}
			catch (Exception ex)
			{
				TempData["error"] = "An error occured while fetching report.";
				_userlog.SaveHistory("Waste Report", "Get", "Error coming in Waste report.");
				_repo.LogErrorToFile(ex, $"Error coming in getting Waste report:{ex.Message.ToString()}");
			}
			return View(reportVm);
		}
	}
}
