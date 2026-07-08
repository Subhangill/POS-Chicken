using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
	public class LedgerController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly DateTime _dateTime;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		public LedgerController(UserLog userLog, ApplicationDbContext applicationDbContext, IInterface @interface)
		{
			_userlog = userLog;
			_dateTime = AppDate.Now;
			_userId = UserHelper.GetCurrentUserId();
			_context = applicationDbContext;
			_repo = @interface;
		}
		public IActionResult Index(AccountVm accountVm)
		{
			try
			{
				var resultlist = new List<LedgerResult>();
				accountVm.Accountlist = _repo.GetList<Account>(query: "Select Accountno ,Name From Account WHere IsDelete=0 Order BY Headid ,SubHead,Accountno ").ToList();
				accountVm.Ledger ??= new Ledger { From = _dateTime, To = _dateTime };
				if (accountVm.Ledger.IsSearch)
				{
					string query = @"SELECT
    t.TransId,
    t.Date,
    t.Dr,
    t.Cr,
    t.InvType,
    t.VType,
    t.InvNo,
    CASE WHEN t.InvType = 'PINV'AND t.VType = 'PV'THEN STUFF((SELECT' | ' +pr.Name +' Weight: ' + CAST(pd.Qty AS VARCHAR(20)) +' Price: ' + CAST(pd.Price AS VARCHAR(20)) +' Total: ' + CAST(pd.Total AS VARCHAR(20)) FROM PurchaseDetail pd LEFT JOIN Product pr ON pr.Id = pd.Pid WHERE pd.PurchaseId = t.InvNo FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,3,'') 
	WHEN t.InvType = 'FPINV'AND t.VType = 'FPV'THEN STUFF((SELECT' | ' +pr.Name +' Weight: ' + CAST(pd.Qty AS VARCHAR(20)) +' Price: ' + CAST(pd.Price AS VARCHAR(20)) +' Total: ' + CAST(pd.Total AS VARCHAR(20)) FROM FinishPurchaseDetail pd LEFT JOIN Product pr ON pr.Id = pd.Pid WHERE pd.PurchaseId = t.InvNo FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,3,'') 
    WHEN t.InvType='SINV' AND t.VType='SV' THEN STUFF((SELECT ' | '+pr.Name+' Weight: '+CAST(pd.Qty AS VARCHAR(20))+' Cut: '+CAST(pd.Cutof AS VARCHAR(20))+' Price: '+CAST(pd.Price AS VARCHAR(20))+' Total: '+CAST(pd.Total AS VARCHAR(20))+' Rent: '+CAST(pd.Rent AS VARCHAR(20) )+' Net: '+CAST(pd.Nettotal AS VARCHAR(20) ) FROM SaleDetail pd LEFT JOIN Product pr ON pr.Id=pd.Pid WHERE pd.InvId=t.InvNo FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,3,'')
    WHEN t.InvType='PTINV' AND t.VType='PTV' THEN STUFF((SELECT ' | '+pr.Name+' Period:'+REPLACE(CONVERT(VARCHAR(9),pd.Startdate,6),' ','-')+' to '+REPLACE(CONVERT(VARCHAR(9),pd.Enddate,6),' ','-')+' Weight: '+CAST(pd.Weight AS VARCHAR(20))+' Price: '+CAST(pd.Rate AS VARCHAR(20))+' Total: '+CAST(pd.Total AS VARCHAR(20)) FROM PaymentDetail pd LEFT JOIN Product pr ON pr.Id=pd.Pid WHERE pd.InvId=t.InvNo FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,3,'')
	ELSE Detail END AS Detail
FROM TransactionDetail t
WHERE
    t.AccountNo = @account
    AND t.Date BETWEEN @start AND @end
ORDER BY
    t.Date,
    t.TransId;";
					int headid = _repo.GetSingleValue<int>("Select ISNULL(Headid,0) as head FROM Account WHERE Accountno=@id", new { id = accountVm.Ledger.AccountId });
					decimal opndr = _repo.GetSingleValue<decimal>("SELECT ISNULL(SUM(Dr),0) as Dr From AccountOpening Where AccountId=@id ", new { id = accountVm.Ledger.AccountId });
					decimal opncr = _repo.GetSingleValue<decimal>("SELECT ISNULL(SUM(Cr),0) as Dr From AccountOpening Where AccountId=@id ", new { id = accountVm.Ledger.AccountId });
					if (headid == 1 || headid == 5)
					{
						accountVm.Ledger.Opening = (opndr - opncr) + _repo.GetSingleValue<decimal>("SELECT ISNULL(SUM(Dr),0)-ISNULL(SUM(Cr),0) FROM TransactionDetail WHERE Accountno=@account and Date <@start ", new { account = accountVm.Ledger.AccountId, start = accountVm.Ledger.From.ToString("yyyy-MM-dd") });
					}
					else
					{
						accountVm.Ledger.Opening = (opncr - opndr) + _repo.GetSingleValue<decimal>("SELECT ISNULL(SUM(Cr),0)-ISNULL(SUM(Dr),0) FROM TransactionDetail WHERE Accountno=@account and Date <@start ", new { account = accountVm.Ledger.AccountId, start = accountVm.Ledger.From.ToString("yyyy-MM-dd") });
					}
					resultlist = _repo.GetList<LedgerResult>(query, new { account = accountVm.Ledger.AccountId, start = accountVm.Ledger.From.ToString("yyyy-MM-dd"), end = accountVm.Ledger.To.ToString("yyyy-MM-dd") }).ToList();
					accountVm.Ledger.Accountname = _repo.GetSingleValue<string>("Select Name FROM Account WHERE Accountno=@id", new { id = accountVm.Ledger.AccountId }) ?? "N/A";
					accountVm.Ledger.HeadId = headid;
				}
				accountVm.LedgerResultlist = resultlist;
			}
			catch (Exception ex)
			{
				_userlog.SaveHistory("Account Ledger", "Fetch", "An error occured while fetching account ledger data!!!");
				_repo.LogErrorToFile(ex, "An error occured while fetching account ledger data!!!");
				TempData["error"] = "An error occured while fetching account ledger data!!!";
			}
			return View(accountVm);
		}
	}
}
