using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
	public class AccountOpeningController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly DateTime _dateTime;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		public AccountOpeningController(UserLog userLog, ApplicationDbContext applicationDbContext, IInterface @interface)
		{
			_userlog = userLog;
			_dateTime = AppDate.Now;
			_userId = UserHelper.GetCurrentUserId();
			_context = applicationDbContext;
			_repo = @interface;
		}
		public IActionResult Index()
		{
			var list = _repo.GetList<AccountOpening>("Select ao.*,a.Name as Accountname From AccountOPening as ao LEFT JOIN Account as a on a.Accountno=ao.Accountid ORDER BY ao.Id DESC").ToList();
			return View(list);
		}
		public IActionResult Create()
		{
			var vm = new AccountVm()
			{
				Accountlist = _repo.GetList<Account>(query: "Select Accountno ,Name From Account WHere IsDelete=0 Order BY Headid ,SubHead,Accountno ").ToList()
			};
			return View(vm);
		}
		[HttpPost]
		public IActionResult Save(AccountVm accountVm)
		{
			try
			{
				if (accountVm.AccountOpeninglist != null && accountVm.AccountOpeninglist.Count > 0)
				{
					foreach (var itm in accountVm.AccountOpeninglist)
					{
						itm.Accountname = _repo.GetSingleValue<string>("Select Name From Account WHere Accountno=@id", new { id = itm.AccountId });
						itm.Date = _dateTime;
						_context.AccountOpening.Add(itm);
						_context.SaveChanges();
						_userlog.SaveHistory("Account Opening", "New", $"AccountId:{itm.AccountId},AccountName:{itm.Accountname},Dr:{itm.Dr},Cr:{itm.Cr},Detail:{itm.Detail}");
					}
				}
				TempData["save"] = "Data saved successfully!!";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["error"] = "An error occured while making transation";
				_repo.LogErrorToFile(ex, "Error occured in Account Opening");
				_userlog.SaveHistory("Account Opening", "New", "An error occured while making transation");
				accountVm.Accountlist = _repo.GetList<Account>(query: "Select Accountno ,Name From Account WHere IsDelete=0 Order BY Headid ,SubHead,Accountno ").ToList();
				return View("Create", accountVm);
			}
		}
	}
}
