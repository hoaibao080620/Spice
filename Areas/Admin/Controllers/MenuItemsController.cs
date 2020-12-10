using System.IO;
using System.Linq;
using System.Threading;
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
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public MenuItemsController(ApplicationDbContext dbContext
            ,IWebHostEnvironment hostingEnvironment) {
            _dbContext = dbContext;
            _hostingEnvironment = hostingEnvironment;
        }
        
        public async Task<IActionResult> Index() {
            var items =await _dbContext.MenuItems.Include(m => m.Category)
                .Include(m => m.SubCategory).ToListAsync();
            // var model = new MenuItemAndCategoriesViewModel() {
            //     Item = new MenuItem(),
            //     Categories = await task
            // };
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Create() {
            var task = _dbContext.Categories.ToListAsync();
            var model = new MenuItemAndCategoriesViewModel() {
                Item = new MenuItem() {
                    ItemName = "hello this is first time test"
                },
                Categories = await task
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MenuItemAndCategoriesViewModel menuItem) {
            
            if(!ModelState.IsValid) {
                menuItem.Categories = await _dbContext.Categories.ToListAsync();
                return View(menuItem);
            }

            await _dbContext.MenuItems.AddAsync(menuItem.Item);
            await _dbContext.SaveChangesAsync();

            var fileFromForm = Request.Form.Files;
            var webRoot = _hostingEnvironment.WebRootPath;

            var itemFromDb = await _dbContext.MenuItems.FindAsync(menuItem.Item.Id);

            if (fileFromForm.Any()) {
                var extension = Path.GetExtension(fileFromForm[0].FileName);
                var uploads = Path.Combine(webRoot, "images\\"+itemFromDb.Id+extension);
                await using (var fileStream = new FileStream(uploads, FileMode.Create)) {
                    await fileFromForm[0].CopyToAsync(fileStream);
                }
                itemFromDb.Image = @"\images\" + itemFromDb.Id + extension;
            }
            else {
                var uploads = Path.Combine(webRoot, @"images\" + StaticData.defaultImage);
                System.IO.File.Copy(uploads,webRoot+@"\images\"+itemFromDb.Id+".png");
                itemFromDb.Image = @"\images\" + itemFromDb.Id + ".png";
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id) {
            if (id == null)
                return NotFound();
            var categories = await _dbContext.Categories.ToListAsync();
            var item = await _dbContext.MenuItems.FirstOrDefaultAsync(m => m.Id == id);
            var model = new MenuItemAndCategoriesViewModel() {
                Item = item,
                Categories = categories
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MenuItemAndCategoriesViewModel menuItem) {

            if (!ModelState.IsValid) {
                menuItem.Categories = await _dbContext.Categories.ToListAsync();
                return View(menuItem);
            }
            var files = HttpContext.Request.Form.Files;

            var webRootPath = _hostingEnvironment.WebRootPath;
            var itemFromDb = await _dbContext.MenuItems.FindAsync(menuItem.Item.Id);
            itemFromDb.ItemName = menuItem.Item.ItemName;
            itemFromDb.CategoryId = menuItem.Item.CategoryId;
            itemFromDb.SubCategoryId = menuItem.Item.SubCategoryId;
            itemFromDb.description = menuItem.Item.description;
            itemFromDb.Price = menuItem.Item.Price;
            itemFromDb.Spicyness = menuItem.Item.Spicyness;

            if (files.Count > 0) {
                var upload = Path.Combine(webRootPath, "images");
                var extensions = Path.GetExtension(files[0].FileName);
                await using (var fileStream = new FileStream(Path.Combine(upload, itemFromDb.Id + extensions)
                    , FileMode.Create)) {
                    await files[0].CopyToAsync(fileStream);
                }
                itemFromDb.Image = @"\images\" + itemFromDb.Id + extensions;
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detail(int? id) {
            if (id == null)
                return NotFound();
            var menuItem = await _dbContext.MenuItems.Include(m => m.SubCategory)
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if(menuItem==null) 
                return NotFound();
            
            return View(menuItem);
        }
        
        [HttpGet]
        public async Task<IActionResult> Delete(int? id) {
            if (id == null)
                return NotFound();
            var menuItem = await _dbContext.MenuItems.Include(m => m.SubCategory)
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if(menuItem==null) 
                return NotFound();
            
            return View(menuItem);
        }
        
        [HttpPost]
        public async Task<IActionResult> Delete(int id) {
            var itemRemoved = await _dbContext.MenuItems.FindAsync(id);
            _dbContext.MenuItems.Remove(itemRemoved);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
