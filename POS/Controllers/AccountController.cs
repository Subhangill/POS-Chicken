using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;
using System.Reflection;

namespace POS.Controllers
{
	public class AccountController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		public AccountController(UserLog userLog, IInterface @interface, ApplicationDbContext applicationDbContext)
		{
			_userlog = userLog;
			_repo = @interface;
			_userId = UserHelper.GetCurrentUserId();
			_context = applicationDbContext;
		}
		public IActionResult Index()
		{
			var list = _repo.GetList<Account>("SELECT a.subhead as subhead, a.id,a.name,sub.name as Subheadname,ah.name as Headname,a.accountno as accountno  FROM Account as a LEFT JOIN Account_SubHead as sub on sub.Id=a.SubHead LEFT JOIN AccountHead ah on ah.Id=a.Headid WHERE a.IsDelete=0 ORDER BY a.Headid,a.Subhead,a.Id ").ToList();
			return View(list);
		}
		public IActionResult Create(Account account)
		{
			var vm = new AccountVm()
			{
				Account_SubHeadlist=new List<Account_SubHead>(),
				Account= account,
			};
			return View(vm);
		}

		[HttpGet]
		public JsonResult GetSubHeads(int headid)
		{
			return Json(_repo.GetList<Account_SubHead>("SELECT id,name FROM Account_SubHead WHERE HeadId =@id ORDER BY Id", new { id=headid}).ToList());
		}

		[HttpPost]
		public IActionResult Save(AccountVm accountVm)
		{
			string actionType = accountVm.Account.Id == 0 ? "New" : "Edit";
			string message = "";
			try
			{
				DateTime dateTime = AppDate.Now;
				bool isNew = accountVm.Account.Id == 0;
				if (isNew)
				{
					accountVm.Account.AccountNo = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(Accountno),0)+1 FROM Account WHERE HeadId=@id ", new {id=accountVm.Account.HeadId });
					accountVm.Account.CreatedBy = _userId;
					accountVm.Account.CodeId = "";
					accountVm.Account.IsDelete = 0;
					accountVm.Account.UpdatedAt = dateTime;
					accountVm.Account.CreatedAt = dateTime;
					_context.Account.Add(accountVm.Account);
					message = "Account Saved Successfully!!";
				}
				else
				{
					var existing = _context.Account.Find(accountVm.Account.Id);
					if (existing == null)
					{
						TempData["error"] = "Account not found.";
						return RedirectToAction("Index");
					}
					int trexist = _repo.GetSingleValue<int>("IF EXISTS (SELECT 1 FROM TransactionDetail WHERE Accountno = @id) SELECT 1 ELSE SELECT 0", new { id= existing.AccountNo });
					if (trexist > 0&&existing.HeadId!= accountVm.Account.HeadId)
					{
						TempData["error"] = "Transaction found for this account account head cannot be changed!!!";
						return View("Create", accountVm);
					}
					if(existing.HeadId != accountVm.Account.HeadId)
					{
						accountVm.Account.AccountNo = _repo.GetSingleValue<int>("SELECT ISNULL(MAX(Accountno),0)+1 FROM Account WHERE HeadId=@id ", new { id = accountVm.Account.HeadId });
					}

					existing.AccountNo = accountVm.Account.AccountNo;
					existing.SubHead = accountVm.Account.SubHead;
					existing.HeadId = accountVm.Account.HeadId;
					existing.Name = accountVm.Account.Name;
					existing.UpdatedBy = _userId;
					existing.UpdatedAt = AppDate.Now;

					

					message = "Account Updated Successfully!!";
				}
				_context.SaveChanges();
				string headname = _context.AccountHead.Where(e => e.Id == accountVm.Account.HeadId).Select(e => e.Name).FirstOrDefault()??"";
				string subheadname = _context.Account_SubHead.Where(e => e.Id == accountVm.Account.SubHead).Select(e => e.Name).FirstOrDefault()??"";
				string detail = $"Id:{accountVm.Account.Id}, Account Name:{accountVm.Account.Name},Account Head:{accountVm.Account.HeadId+"-"+headname},Account SubHead:{accountVm.Account.SubHead+"-"+subheadname },AccountNo:{accountVm.Account.AccountNo} ";
				_userlog.SaveHistory("Account", actionType, detail);
				TempData["save"] = message;
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error saving account: {accountVm.Account.Name}");
				_userlog.SaveHistory("AccountSubHead", actionType, $"Error: {ex.Message}");
				TempData["error"] = "An error occurred while saving the account";
			}
			return RedirectToAction("Index");
		}

		public IActionResult Edit(int id)
		{
			var existing = _context.Account.Find(id);
			if (existing == null)
			{
				TempData["error"] = "Account not found.";
				return RedirectToAction("Index");
			}
			var vm = new AccountVm()
			{
				Account_SubHeadlist = _repo.GetList<Account_SubHead>("SELECT id,name FROM Account_SubHead WHERE HeadId =@id ORDER BY Id", new { id = existing.HeadId }),
				Account = existing,
			};
			return View("Create", vm);
		}

		public IActionResult Delete(int id)
		{
			try
			{
				var account = _context.Account.Find(id);
				if (account == null)
				{
					TempData["error"] = "Account not found.";
					return RedirectToAction("Index");
				}
				string headname = _context.AccountHead.Where(e => e.Id == account.HeadId).Select(e => e.Name).FirstOrDefault() ?? "";
				string subheadname = _context.Account_SubHead.Where(e => e.Id == account.SubHead).Select(e => e.Name).FirstOrDefault() ?? "";

				string detail = $"Id:{account.Id}, Account Name:{account.Name},Account Head:{account.HeadId + "-" + headname},Account SubHead:{account.SubHead + "-" + subheadname},AccountNo:{account.AccountNo} ";

				_userlog.SaveHistory("Account", "Delete", detail);
				account.IsDelete = 1;
				_context.SaveChanges();
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error deleting Account ID: {id}");
				_userlog.SaveHistory("Account", "Delete", $"Error: {ex.Message.ToString()}");
				TempData["error"] = "An error occurred while deleting the Account.";
			}
			return RedirectToAction("Index");
		}

	}
}
