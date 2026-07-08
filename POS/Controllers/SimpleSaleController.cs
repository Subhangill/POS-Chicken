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
	public class SimpleSaleController : Controller
	{
		private readonly UserLog _userlog;
		private readonly int _userId;
		private readonly DateTime _dateTime;
		private readonly IInterface _repo;
		private readonly ApplicationDbContext _context;
		public SimpleSaleController(UserLog userLog, ApplicationDbContext applicationDbContext, IInterface @interface)
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
			var list = _repo.GetList<Order>("Select p.Id,Grossweight,p.date,s.name as Suppname,c.Name as Customername FROM [Order] As P LEFT JOIN SUpplier as s on s.ID=p.Supplierid LEFT JOIN Customer as c on c.ID=p.Customerid WHERE p.isdelete=0 and p.Date BETWEEN @start and @end ORDER BY p.Date,Id DESC", new { start = startdate.ToString("yyyy-MM-dd"), end = enddate.ToString("yyyy-MM-dd") }).ToList();
			ViewBag.s_date = startdate.ToString("yyyy-MM-dd");
			ViewBag.e_date = enddate.ToString("yyyy-MM-dd");
			return View(list);
		}
		public IActionResult Create(Order order)
		{
			order.Date = _dateTime;
			var vm = new SaleVm()
			{
				Order = order,
				Customerlist = _repo.GetList<Customer>("Select id,name From Customer WHERE Isdelete=0"),
				Supplierlist = _repo.GetList<Supplier>("Select id,name From Supplier WHERE Isdelete=0"),
				Productlist = _repo.GetList<Product>("Select id,name From Product WHERE Isdelete=0 and Categoryid=3")
			};
			return View(vm);
		}

		[HttpPost]
		public IActionResult Save(SaleVm saleVm)
		{
			try
			{
				string msg, action, itemdetail = "";
				bool isNew = saleVm.Order.Id == 0;
				if (isNew)
				{
					saleVm.Order.Note = saleVm.Order.Note ?? "";
					saleVm.Order.CreatedAt = _dateTime;
					saleVm.Order.CreatedBy = _userId;
					saleVm.Order.UpdatedBy = 0;
					saleVm.Order.CodeId = "";
					_context.Order.Add(saleVm.Order);
					msg = "Inv Saved Successfully!!";
					action = "New";
				}
				else
				{
					var existing = _context.Order.FirstOrDefault(e => e.Id == saleVm.Order.Id);
					if (existing == null)
					{
						TempData["error"] = "Order Not Found!!";
						return RedirectToAction("Index");
					}
					existing.Note = saleVm.Order.Note ?? "";
					existing.Grossweight = saleVm.Order.Grossweight;
					existing.Customerid = saleVm.Order.Customerid;
					existing.Supplierid = saleVm.Order.Supplierid;
					existing.Note = saleVm.Order.Note;
					existing.UpdatedBy = _userId;
					existing.UpdatedAt = _dateTime;
					existing.Date = saleVm.Order.Date;
					msg = "Inv Updated Successfully!!";
					action = "Edit";
					_context.OrderDetail.Where(e => e.Invid == saleVm.Order.Id).ExecuteDelete();
				}
				_context.SaveChanges();
				foreach (var itm in saleVm.OrderDetaillist)
				{
					itm.Invid = saleVm.Order.Id;
					_context.OrderDetail.Add(itm);
					string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "";
					itemdetail += $"Item:{itm.Pid + "-" + itemname} ||Weight:{itm.Weight} ";
				}
				_context.SaveChanges();

				string custname = _context.Customer.Where(e => e.Id == saleVm.Order.Customerid).Select(e => e.Name).FirstOrDefault() ?? "";
				string suppname = _context.Supplier.Where(e => e.Id == saleVm.Order.Supplierid).Select(e => e.Name).FirstOrDefault() ?? "";
				_userlog.SaveHistory("Raw Sale", action, $"Id:{saleVm.Order.Id} , Date:{saleVm.Order.Date.ToString("yyyy-MM-dd")} ,Supplier:{saleVm.Order.Supplierid + "-" + suppname}, Customer:{saleVm.Order.Customerid + "-" + custname} Note:{saleVm.Order.Note}, GrossWeight:{saleVm.Order.Grossweight} || Items ==>{itemdetail}");
				TempData["save"] = msg;
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_repo.LogErrorToFile(ex, $"Error in Order ID: {saleVm.Order.Id}");
				_userlog.SaveHistory("Raw Sale", "Error", $"Error: {ex.Message.ToString()}");
				TempData["error"] = "An error occurred while making transaction.";
				saleVm.Customerlist = _repo.GetList<Customer>("Select id,name From Customer WHERE Isdelete=0");
				saleVm.Supplierlist = _repo.GetList<Supplier>("Select id,name From Supplier WHERE Isdelete=0");
				saleVm.Productlist = _repo.GetList<Product>("Select id,name From Product WHERE Isdelete=0 and Categoryid=3");
				return View("Create", saleVm);
			}
		}
		public IActionResult Edit(int id)
		{
			var existing = _context.Order.FirstOrDefault(e => e.Id == id);
			if (existing == null)
			{
				TempData["error"] = "Order Not Found!!";
				return RedirectToAction("Index");
			}

			var vm = new SaleVm()
			{
				OrderDetaillist=_context.OrderDetail.Where(e=>e.Invid==id).ToList(),
				Order = existing,
				Customerlist = _repo.GetList<Customer>("Select id,name From Customer WHERE Isdelete=0"),
				Supplierlist = _repo.GetList<Supplier>("Select id,name From Supplier WHERE Isdelete=0"),
				Productlist = _repo.GetList<Product>("Select id,name From Product WHERE Isdelete=0 and Categoryid=3")
			};
			return View("Create", vm);
		}
		public IActionResult Delete(int id)
		{
			var existing = _context.Order.FirstOrDefault(e => e.Id == id);
			if (existing == null)
			{
				TempData["error"] = "Order Not Found!!";
				return RedirectToAction("Index");
			}
			string itemdetail = "";
			var details = _context.OrderDetail.Where(e => e.Invid == id).ToList();
			foreach(var itm in details)
			{
				string itemname = _context.Product.Where(e => e.Id == itm.Pid).Select(e => e.Name).FirstOrDefault() ?? "";
				itemdetail += $"Item:{itm.Pid + "-" + itemname} ||Weight:{itm.Weight} ";
			}
			_context.Order.Where(e => e.Id == id).ExecuteUpdate(e=>e.SetProperty(e=>e.IsDelete,1));
			_context.SaveChanges();
			string custname = _context.Customer.Where(e => e.Id == existing.Customerid).Select(e => e.Name).FirstOrDefault() ?? "";
			string suppname = _context.Supplier.Where(e => e.Id == existing.Supplierid).Select(e => e.Name).FirstOrDefault() ?? "";

			_userlog.SaveHistory("Raw Sale", "Delete", $"Id:{existing.Id} , Date:{existing.Date.ToString("yyyy-MM-dd")} ,Supplier:{existing.Supplierid + "-" + suppname}, Customer:{existing.Customerid + "-" + custname} Note:{existing.Note}, GrossWeight:{existing.Grossweight} || Items ==>{itemdetail}");
			return RedirectToAction("Index");
		}


	}
}
