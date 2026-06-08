using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using POS.Data;
using POS.Data.Service;
using POS.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace POS.Controllers
{
    public class SettingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserLog _userLog;
        private readonly int _userId;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SettingController(ApplicationDbContext context, UserLog userLog, IWebHostEnvironment webHostEnvironment)
        {
            _userId = UserHelper.GetCurrentUserId();
            _context = context;
            _userLog = userLog;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Setting/Create
        [HttpGet]
        public IActionResult Create()
        {
            var setting = _context.Setting.FirstOrDefault();
            return View(setting ?? new Setting());
        }

        public string GenrateCodeIdSetting()
        {
            string currentYear = AppDate.Now.ToString("yy");
            var Code = _context.Setting.Where(x => x.CodeId != null).OrderByDescending(x => x.Id).Select(x => x.CodeId).FirstOrDefault();

            if (string.IsNullOrEmpty(Code)) return $"ST-{currentYear}01";

            string lastpart = Code.Substring(5);
            if (int.TryParse(lastpart, out int number))
            {
                int newNumber = number + 1;
                return $"ST-{currentYear}{newNumber}";
            }
            return $"ST-{currentYear}01";
        }

        // POST: /Setting/Save (mirrors Area pattern)
        [HttpPost]
        public async Task<IActionResult> Save(Setting setting)
        {
            try
            {
                string ms = "";
                string logoFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Logo");
                if (!Directory.Exists(logoFolder))
                {
                    Directory.CreateDirectory(logoFolder);
                }

                // Check if setting record exists
                var db = _context.Setting.FirstOrDefault();

                if (db == null) // Create new
                {
                    ms = "Data Saved Successfully";

                    if (setting.httpPostedFile != null)
                    {
                        string filename = Path.GetFileNameWithoutExtension(setting.httpPostedFile.FileName);
                        string extension = Path.GetExtension(setting.httpPostedFile.FileName);
                        filename = filename + DateTime.Now.ToString("yyyyMMddHHmmssfff") + extension;
                        setting.logo = "~/Logo/" + filename;

                        string path = Path.Combine(logoFolder, filename);
                        using (var fileStream = new FileStream(path, FileMode.Create))
                        {
                            await setting.httpPostedFile.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                        setting.logo = "~/Logo/null";
                    }

                    setting.CreatedBy = _userId;
                    setting.CreatedAt = AppDate.Now;
                    setting.CodeId = GenrateCodeIdSetting();

                    _context.Setting.Add(setting);
                }
                else // Update existing
                {
                    ms = "Data update successfully";

                    db.Name = setting.Name;
                    db.PhoneNumber = setting.PhoneNumber;
                    db.Email = setting.Email;
                    db.Address = setting.Address;
                    db.UpdatedBy = UserHelper.GetCurrentUserId();
                    db.IsDelete = 0;
                    db.UpdatedAt = AppDate.Now;

                    if (setting.httpPostedFile != null)
                    {
                        // Delete previous logo file from disk if it exists
                        if (!string.IsNullOrEmpty(db.logo) && db.logo != "~/Logo/null" && db.logo.StartsWith("~/"))
                        {
                            try
                            {
                                var relativePath = db.logo.Replace("~/", "");
                                var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                                if (System.IO.File.Exists(physicalPath))
                                {
                                    System.IO.File.Delete(physicalPath);
                                }
                            }
                            catch (Exception)
                            {
                                // Ignore file deletion errors
                            }
                        }

                        // Save the new logo file
                        string filename = Path.GetFileNameWithoutExtension(setting.httpPostedFile.FileName);
                        string extension = Path.GetExtension(setting.httpPostedFile.FileName);
                        filename = filename + DateTime.Now.ToString("yyyyMMddHHmmssfff") + extension;
                        
                        setting.logo = "~/Logo/" + filename;
                        db.logo = setting.logo;

                        string path = Path.Combine(logoFolder, filename);
                        using (var fileStream = new FileStream(path, FileMode.Create))
                        {
                            await setting.httpPostedFile.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                        // Keep previous logo path in setting.logo for the history log!
                        setting.logo = db.logo;
                    }
                }

                // Enforce that setting.Id matches the saved/updated ID for accurate logs!
                if (db != null)
                {
                    setting.Id = db.Id;
                }

                string detail;
                string Action;
                if (db == null)
                {
                    detail = $"ID:{setting.Id}, Name:{setting.Name}, Logo:{setting.logo}, Phone:{setting.PhoneNumber}, Email:{setting.Email}, Address:{setting.Address}";
                    Action = "New";
                }
                else
                {
                    detail = $"ID:{setting.Id}, Name:{setting.Name}, Logo:{setting.logo}, Phone:{setting.PhoneNumber}, Email:{setting.Email}, Address:{setting.Address}";
                    Action = "Edit";
                }

                TempData["save"] = ms;
                await _context.SaveChangesAsync();

                // Now setting.Id has the generated ID from SaveChangesAsync() if it was a new record
                if (db == null)
                {
                    // Update detail with the generated ID for the new record
                    detail = $"ID:{setting.Id}, Name:{setting.Name}, Logo:{setting.logo}, Phone:{setting.PhoneNumber}, Email:{setting.Email}, Address:{setting.Address}";
                }

                _userLog.SaveHistory("Setting", Action, detail);
                return RedirectToAction("Create");
            }
            catch (Exception er)
            {
                TempData["error"] = er.Message;
                return RedirectToAction("Create");
            }
        }
    }
}
