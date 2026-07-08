using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;

namespace POS.Controllers
{
	public class SubHeadController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		public SubHeadController(UserLog userLog, IInterface @interface, ApplicationDbContext applicationDbContext)
		{
			_userlog = userLog;
			_repo = @interface;
			_userId = UserHelper.GetCurrentUserId();
			_context = applicationDbContext;
		}
		public IActionResult Index()
		{
			var list = _repo.GetList<Account_SubHead>("SELECT sub.*,ah.Name as Headname FROM Account_SubHead as sub LEFT JOIN AccountHead as ah on ah.Id=sub.Headid WHERE sub.IsDelete=0 ORDER BY sub.Headid,sub.id ").ToList();
			return View(list);
		}
		public IActionResult Create(Account_SubHead account_SubHead)
		{
			return View(account_SubHead);
		}
		[HttpPost]
		public IActionResult Save(Account_SubHead account_SubHead)
		{
			string actionType = account_SubHead.Id == 0 ? "New" : "Edit";
			string message = "";
			try
			{
				DateTime dateTime = AppDate.Now;
				bool isNew = account_SubHead.Id == 0;
				if(isNew)
				{
					account_SubHead.CreatedBy = _userId;
					account_SubHead.CodeId = "";
					account_SubHead.IsDelete = 0;
					account_SubHead.UpdatedAt = dateTime;
					account_SubHead.CreatedAt = dateTime;
					_context.Account_SubHead.Add(account_SubHead);
					message = "Account Sub Head Saved Successfully!!";
				}
				else
				{
					var existingsubhead = _context.Account_SubHead.Find(account_SubHead.Id);
					if (existingsubhead == null)
					{
						TempData["error"] = "Account SubHead not found.";
						return RedirectToAction("Index");
					}
					existingsubhead.HeadId = account_SubHead.HeadId;
					existingsubhead.Name = account_SubHead.Name;
					existingsubhead.UpdatedBy = _userId;
					existingsubhead.UpdatedAt = AppDate.Now;

					message = "Account Sub Head Updated Successfully!!";
				}
				_context.SaveChanges();
				string detail = $"Id:{account_SubHead.Id}, AccountSub Head Name:{account_SubHead.Name},AccountSub Head:{account_SubHead.HeadId}  ";
				_userlog.SaveHistory("SubHead", actionType, detail);
				TempData["save"] = message;
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error saving accountSubHead: {account_SubHead.Name}");
				_userlog.SaveHistory("AccountSubHead", actionType, $"Error: {ex.Message}");
				TempData["error"] = "An error occurred while saving the accountSubHead.";
			}
			return RedirectToAction("Index");
		}



		
		public IActionResult Edit(int id)
		{
			var existingsubhead = _context.Account_SubHead.Find(id);
			if (existingsubhead == null)
			{
				TempData["error"] = "AccountSubHead not found.";
				return RedirectToAction("Index");
			}
			return View("Create", existingsubhead);
		}
	
		public IActionResult Delete(int id)
		{
			try
			{
				var accountSubHead = _context.Account_SubHead.Find(id);
				if (accountSubHead == null)
				{
					TempData["error"] = "AccountSubHead not found.";
					return RedirectToAction("Index");
				}
				_userlog.SaveHistory( "SubHead", "Delete", $"AccountSubHead {accountSubHead.Name} HeadId:{accountSubHead.HeadId} deleted successfully.");
				accountSubHead.IsDelete = 1;
				_context.SaveChanges();
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error deleting accountSubHead ID: {id}");
				_userlog.SaveHistory("SubHead", "Delete", $"Error: {ex.Message.ToString()}");
				TempData["error"] = "An error occurred while deleting the accountSubHead.";
			}
			return RedirectToAction("Index");
		}


	}
}
