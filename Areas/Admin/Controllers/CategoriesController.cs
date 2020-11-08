using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;

namespace Spice.Areas.Admin.Controllers {
    [Area("Admin")]
    public class CategoriesController : Controller {
        private readonly ApplicationDbContext dbContext;

        public CategoriesController(ApplicationDbContext dbContext) {
            this.dbContext = dbContext;
        }
        public async Task<IActionResult> Index() {
            return View(await dbContext.Categories.ToListAsync());
        }

        public IActionResult Create() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category category) {
            if(ModelState.IsValid) {
                dbContext.Add(category);
                await dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        public async Task<IActionResult> Edit(int? id) {
            if (id == null) {
                return NotFound();
            }
            var category = await dbContext.Categories.FindAsync(id);
            if(category == null) {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category category) {
            if(ModelState.IsValid) {
                dbContext.Categories.Update(category);
                await dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        public async Task<IActionResult> Delete(int? id) {
            if (id == null) {
                return NotFound();
            }
            var category = await dbContext.Categories.FindAsync(id);
            if (category == null) {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirm(int? id) {
            var cateogry = await dbContext.Categories.FindAsync(id);
            if(cateogry==null) {
                return View();
            }
            dbContext.Categories.Remove(cateogry);
            await dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult>Detail(int? id) {
            var cateogry = await dbContext.Categories.FindAsync(id);
            if (cateogry == null) {
                return NotFound();
            }
            return View(cateogry);
        }
    }
}
