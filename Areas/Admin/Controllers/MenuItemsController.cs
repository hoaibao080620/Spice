using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utilities;

namespace Spice.Areas.Admin.Controllers {
    [Area("Admin")]
    public class MenuItemsController : Controller {
        private readonly IWebHostEnvironment webHost;
        private readonly ApplicationDbContext dbContext;
        private readonly IWebHostEnvironment hostingEnvironment;

        public MenuItemsController(IWebHostEnvironment webHost, ApplicationDbContext dbContext
            ,IWebHostEnvironment hostingEnvironment) {
            this.webHost = webHost;
            this.dbContext = dbContext;
            this.hostingEnvironment = hostingEnvironment;
        }
        public async Task<IActionResult> Index() {
            var Items =await dbContext.MenuItems.Include(m => m.Category).Include(m => m.SubCategory).ToListAsync();
            //var model = new MenuItemAndCategoriesViewModel() {
            //    Item = new MenuItem(),
            //    Categories = await task
            //};
            return View(Items);
        }

        [HttpGet]
        public async Task<IActionResult> Create() {
            var task = dbContext.Categories.ToListAsync();
            var model = new MenuItemAndCategoriesViewModel() {
                Item = new MenuItem(),
                Categories = await task
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MenuItemAndCategoriesViewModel menuItem) {
            
            if(!ModelState.IsValid) {
                menuItem.Categories = await dbContext.Categories.ToListAsync();
                return View(menuItem);
            }

            dbContext.MenuItems.Add(menuItem.Item);
            await dbContext.SaveChangesAsync();

            var files = HttpContext.Request.Form.Files;

            var webRootPath = hostingEnvironment.WebRootPath;
            var itemFromDb = await dbContext.MenuItems.FindAsync(menuItem.Item.Id);

            if(files.Count>0) {
                var upload = Path.Combine(webRootPath, "images");
                var extensions = Path.GetExtension(files[0].FileName);
                using(var fileStream = new FileStream(Path.Combine(upload,itemFromDb.Id+extensions)
                    ,FileMode.Create)) {
                    files[0].CopyTo(fileStream);
                }
                itemFromDb.Image = @"\images\"+itemFromDb.Id+extensions;
            }
            else {
                var upload = Path.Combine(webRootPath, @"images\"+StaticData.defaultImage);
                System.IO.File.Copy(upload, webRootPath+@"\images\" + itemFromDb.Id + ".png");
                itemFromDb.Image = @"\images\" + itemFromDb.Id + ".png";
            }
            await dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id) {
            if (id == null)
                return NotFound();
            var categories = await dbContext.Categories.ToListAsync();
            var item = await dbContext.MenuItems.FirstOrDefaultAsync(m => m.Id == id);
            var model = new MenuItemAndCategoriesViewModel() {
                Item = item,
                Categories = categories
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MenuItemAndCategoriesViewModel menuItem) {

            if (!ModelState.IsValid) {
                menuItem.Categories = await dbContext.Categories.ToListAsync();
                return View(menuItem);
            }
            var files = HttpContext.Request.Form.Files;

            var webRootPath = hostingEnvironment.WebRootPath;
            var itemFromDb = await dbContext.MenuItems.FindAsync(menuItem.Item.Id);
            itemFromDb.ItemName = menuItem.Item.ItemName;
            itemFromDb.CategoryId = menuItem.Item.CategoryId;
            itemFromDb.SubCategoryId = menuItem.Item.SubCategoryId;
            itemFromDb.description = menuItem.Item.description;
            itemFromDb.Price = menuItem.Item.Price;
            itemFromDb.Spicyness = menuItem.Item.Spicyness;

            if (files.Count > 0) {
                var upload = Path.Combine(webRootPath, "images");
                var extensions = Path.GetExtension(files[0].FileName);
                using (var fileStream = new FileStream(Path.Combine(upload, itemFromDb.Id + extensions)
                    , FileMode.Create)) {
                    files[0].CopyTo(fileStream);
                }
                itemFromDb.Image = @"\images\" + itemFromDb.Id + extensions;
            }
            
            await dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
