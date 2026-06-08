using Microsoft.AspNetCore.Mvc;
using POS.Data;
using POS.Data.Service;
using POS.Models;
using POS.ViewModel;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace POS.Controllers
{
    public class ProductionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;

        public ProductionController(ApplicationDbContext context, UserLog userLog)
        {
            _userId = UserHelper.GetCurrentUserId();
            _context = context;
            _userLog = userLog;
        }

        public IActionResult Index()
        {
            var list = _context.Production.ToList();
            return View(list);
        }

        public IActionResult Create()
        {
            var vm = new ProductionVM
            {
                production = new Production { Date = AppDate.Now },
                productList = _context.Product.Where(p => p.IsDelete == 0).ToList(),
                RawProductId = _context.Product.Where(x=>x.CategoryId == 3 && x.IsDelete == 0).ToList(),
                FinishProductId = _context.Product.Where(x=>x.CategoryId == 4 && x.IsDelete == 0).ToList(),
                rawDetails = new List<RawDetail>(),
                finishDetails = new List<FinishDetail>()
            };
            return View(vm);
        }

        public string GenrateCodeId()
        {
            string currentYear = AppDate.Now.ToString("yy");
            var Code = _context.Production.Where(x => x.CodeId != null).OrderByDescending(x => x.Id).Select(x => x.CodeId).FirstOrDefault();

            if (string.IsNullOrEmpty(Code)) return $"PD-{currentYear}01";

            string lastpart = Code.Substring(5);
            int number = int.Parse(lastpart);
            int newNumber = number + 1;
            return $"PD-{currentYear}{newNumber}";
        }

        [HttpPost]
        public IActionResult Save(ProductionVM vm)
        {
            try
            {
                string ms = "";
                
                // Calculate totals from details
                vm.production.RawTotal = vm.rawDetails?.Sum(x => x.weight) ?? 0;
                vm.production.FinalTotal = vm.finishDetails?.Sum(x => x.weight) ?? 0;
                
                if (vm.production.Id == 0)
                {
                    vm.production.IsDelete = 0;
                    vm.production.CreatedBy = _userId;
                    vm.production.CreatedAt = AppDate.Now;
                    vm.production.CodeId = GenrateCodeId();
                    vm.production.Note = vm.production.Note ?? "N/A";

                    _context.Production.Add(vm.production);
                    _context.SaveChanges();

                    // Save details
                    if (vm.rawDetails != null && vm.rawDetails.Any())
                    {
                        foreach (var rd in vm.rawDetails)
                        {
                            rd.masterId = vm.production.Id;
                            _context.RawDetails.Add(rd);
                        }
                    }

                    if (vm.finishDetails != null && vm.finishDetails.Any())
                    {
                        foreach (var fd in vm.finishDetails)
                        {
                            fd.masterId = vm.production.Id;
                            _context.FinishDetails.Add(fd);
                        }
                    }
                    _context.SaveChanges();
                    ms = "Data Saved Successfully";
                }
                else
                {
                    var db = _context.Production.Find(vm.production.Id);
                    if (db != null)
                    {
                        db.Date = vm.production.Date;
                        db.Note = vm.production.Note;
                        db.RawTotal = vm.production.RawTotal;
                        db.FinalTotal = vm.production.FinalTotal;
                        db.UpdatedBy = UserHelper.GetCurrentUserId();
                        db.UpdatedAt = AppDate.Now;
                    }

                    // Remove existing details
                    var oldRaw = _context.RawDetails.Where(r => r.masterId == vm.production.Id).ToList();
                    _context.RawDetails.RemoveRange(oldRaw);

                    var oldFinish = _context.FinishDetails.Where(f => f.masterId == vm.production.Id).ToList();
                    _context.FinishDetails.RemoveRange(oldFinish);

                    // Save new details
                    if (vm.rawDetails != null && vm.rawDetails.Any())
                    {
                        foreach (var rd in vm.rawDetails)
                        {
                            rd.masterId = vm.production.Id;
                            _context.RawDetails.Add(rd);
                        }
                    }

                    if (vm.finishDetails != null && vm.finishDetails.Any())
                    {
                        foreach (var fd in vm.finishDetails)
                        {
                            fd.masterId = vm.production.Id;
                            _context.FinishDetails.Add(fd);
                        }
                    }
                    ms = "Data Updated Successfully";
                }

                string detail, Action;
                if (vm.production.Id == 0)
                {
                    detail = $"ID:{vm.production.Id}, CodeId:{vm.production.CodeId}, RawTotal:{vm.production.RawTotal}, FinalTotal:{vm.production.FinalTotal}";
                    Action = "New";
                }
                else
                {
                    detail = $"ID:{vm.production.Id}, CodeId:{vm.production.CodeId}, RawTotal:{vm.production.RawTotal}, FinalTotal:{vm.production.FinalTotal}";
                    Action = "Edit";
                }

                TempData["save"] = ms;
                _context.SaveChanges();
                _userLog.SaveHistory("Production", Action, detail);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Create");
            }
            }

        public IActionResult Edit(int id)
        {
            var dbProduction = _context.Production.Find(id);
            if (dbProduction == null) return NotFound();

            var vm = new ProductionVM
            {
                production = dbProduction,
                productList = _context.Product.Where(p => p.IsDelete == 0).ToList(),
                RawProductId = _context.Product.Where(x => x.CategoryId == 3 && x.IsDelete == 0).ToList(),
                FinishProductId = _context.Product.Where(x => x.CategoryId == 4 && x.IsDelete == 0).ToList(),
                rawDetails = _context.RawDetails.Where(rd => rd.masterId == dbProduction.Id).ToList(),
                finishDetails = _context.FinishDetails.Where(fd => fd.masterId == dbProduction.Id).ToList()
            };

            return View("Create", vm);
        }

        public IActionResult Delete(int id)
        {
            var db = _context.Production.Find(id);
            if (db == null) return NotFound();

            string detail = $"ID:{db.Id}, CodeId:{db.CodeId}";
            db.IsDelete = 1;
            _context.SaveChanges();
            _userLog.SaveHistory("Production", "Delete", detail);
            return RedirectToAction("Index");
        }
    }
}
