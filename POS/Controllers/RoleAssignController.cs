using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Interface;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;

namespace POS.Controllers
{
	public class RoleAssignController : Controller
	{
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		private readonly UserLog _userLog;
		private readonly int _userId;
		private readonly IPasswordHasher<UserLogin> _passwordHasher;
		public RoleAssignController(ApplicationDbContext context, IInterface @interface, UserLog userLog)
		{
			_context = context;
			_repo = @interface;
			_userLog = userLog;
			_userId = UserHelper.GetCurrentUserId();
			// Use default PasswordHasher implementation
			_passwordHasher = new PasswordHasher<UserLogin>();
		}
		public IActionResult Index()
		{
			var list = _repo.GetList<Role>("SELECT DISTINCT r.Id, r.Name FROM RoleAssign rl INNER JOIN Role r ON r.Id = rl.RoleId ORDER BY r.Id DESC").ToList();
			return View(list);
		}
		public IActionResult Create(RoleAssign roleAssign)
		{
			var vm = new UserVm()
			{
				RoleAssignList = new List<RoleAssign>(),
				RoleAssign = roleAssign,
				Rolelist = _context.Role.Where(e => e.IsDelete == 0).ToList(),
				FormHeadlist = _context.FormHead.OrderBy(e => e.Sr).ToList(),
				Formlist = _context.Form.OrderBy(e => e.HeadId).ToList(),
				FormActionlist = _context.FormAction.OrderBy(e => e.FormId).ToList(),
			};
			return View(vm);
		}

		[HttpPost]
		public IActionResult Save(int RoleId, string[] selectedValues)
		{
			try
			{
				if (RoleId == 0 || selectedValues == null || selectedValues.Length == 0)
				{
					TempData["error"] = "Role and at least one permission are required.";
					return RedirectToAction("Create");
				}

				// Check if role already exists (for new creation)
				var isUpdate = _context.RoleAssign.Any(x => x.RoleId == RoleId);
				if (!isUpdate && _context.RoleAssign.Any(x => x.RoleId == RoleId))
				{
					// Usually updates are allowed, but per user request we check exists.
					// However, for practicality in an "Edit" flow, we usually delete and re-insert.
					// If user wants strict "Error if exists", we keep it.
				}

				// If it's an update, we technically delete old and insert new.
				// But user said: "check if role already exist if exist throw error".
				// I'll assume this applies to NEW creations.
				bool exists = _context.RoleAssign.Any(x => x.RoleId == RoleId);

				// Let's use a flag from hidden input or something if we want to distinguish Edit vs Save.
				// For now, if someone is in "Create" mode and clicks Save on an existing role, we block.
				// If they came from "Edit", we allow.

				// If we don't have an 'Id' for the RoleAssign set, we treat it as New.
				// Since RoleAssign is multi-row, we check by RoleId.

				// Let's implement the logic requested:
				if (Request.Form["isUpdate"] != "true" && exists)
				{
					TempData["ErrorMessage"] = "Permissions for this role already exist. Use Edit if you want to change them.";
					var vm = new UserVm
					{
						RoleAssign = new RoleAssign { RoleId = RoleId },
						Rolelist = _context.Role.Where(e => e.IsDelete == 0).ToList(),
						FormHeadlist = _context.FormHead.OrderBy(e => e.Sr).ToList(),
						Formlist = _context.Form.OrderBy(e => e.HeadId).ToList(),
						FormActionlist = _context.FormAction.OrderBy(e => e.FormId).ToList(),
						RoleAssignList = selectedValues.Select(v =>
						{
							var parts = v.Split('|');
							return new RoleAssign { HeadId = int.Parse(parts[0]), FormId = int.Parse(parts[1]), ActionId = int.Parse(parts[2]) };
						}).ToList()
					};
					return View("Create", vm);
				}

				using var transaction = _context.Database.BeginTransaction();
				try
				{
					// Delete existing if update
					if (exists)
					{
						var existing = _context.RoleAssign.Where(x => x.RoleId == RoleId).ToList();
						_context.RoleAssign.RemoveRange(existing);
					}

					var roleName = _repo.GetSingleValue<string>("SELECT Name FROM Role WHERE Id=@id", new { id = RoleId });
					var logsDescription = new List<string>();

					foreach (var val in selectedValues)
					{
						var parts = val.Split('|'); // HeadId|FormId|ActionCode
						var ra = new RoleAssign
						{
							RoleId = RoleId,
							HeadId = int.Parse(parts[0]),
							FormId = int.Parse(parts[1]),
							ActionId = int.Parse(parts[2])
						};
						_context.RoleAssign.Add(ra);

						// For logging
						var formName = _repo.GetSingleValue<string>("SELECT FormName FROM Form WHERE ids=@id", new { id = ra.FormId });
						var actionName = _repo.GetSingleValue<string>("SELECT ActionName FROM FormAction WHERE ActionCode=@code AND FormId=@fid", new { code = ra.ActionId, fid = ra.FormId });
						logsDescription.Add($"{formName}({actionName})");
					}

					_context.SaveChanges();
					transaction.Commit();

					string actionType = exists ? "Update" : "Create";
					_userLog.SaveHistory("Role Assign", actionType, $"Role {roleName} assigned permissions: {string.Join(", ", logsDescription)}");

					TempData["save"] = $"Role Assignments {actionType}d successfully.";
				}
				catch (Exception ex)
				{
					TempData["error"] = "An error coming.";
					transaction.Rollback();
					_repo.LogErrorToFile(ex, $"An error coming'{ex.Message.ToString()}'");
					throw ex;
				}

				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, "Error saving RoleAssign");
				TempData["ErrorMessage"] = "An error occurred while saving.";
				return RedirectToAction("Index");
			}
		}

		public IActionResult Edit(int roleId)
		{
			var existing = _context.RoleAssign.Where(x => x.RoleId == roleId).ToList();
			if (existing.Count == 0) return RedirectToAction("Index");

			var vm = new UserVm
			{
				RoleAssign = new RoleAssign { RoleId = roleId },
				Rolelist = _context.Role.Where(e => e.IsDelete == 0).ToList(),
				FormHeadlist = _context.FormHead.OrderBy(e => e.Sr).ToList(),
				Formlist = _context.Form.OrderBy(e => e.HeadId).ToList(),
				FormActionlist = _context.FormAction.OrderBy(e => e.FormId).ToList(),
				RoleAssignList = existing
			};
			ViewBag.IsUpdate = true;
			return View("Create", vm);
		}





	}
}
