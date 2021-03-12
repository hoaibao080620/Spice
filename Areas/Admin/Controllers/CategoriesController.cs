using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Utilities;

namespace Spice.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize(Roles = UserRole.Manager)]
    public class CategoriesController : Controller {
        private readonly ApplicationDbContext _dbContext;
        
        public CategoriesController(ApplicationDbContext dbContext) {
            this._dbContext = dbContext;
        }
        public async Task<IActionResult> Index() {
            return View(await _dbContext.Categories.ToListAsync());
        }

        public IActionResult Create() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category category) {
            if (!ModelState.IsValid) 
                return View(category);
            _dbContext.Add(category);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id) {
            if (id == null) {
                return NotFound();
            }
            var category = await _dbContext.Categories.FindAsync(id);
            if(category == null) {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category category) {
            if (!ModelState.IsValid) 
                return View();
            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id) {
            if (id == null) {
                return NotFound();
            }
            var category = await _dbContext.Categories.FindAsync(id);
            if (category == null) {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirm(int? id) {
            var category = await _dbContext.Categories.FindAsync(id);
            if(category==null) {
                return View();
            }
            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult>Detail(int? id) {
            var category = await _dbContext.Categories.FindAsync(id);
            if (category == null) {
                return NotFound();
            }
            return View(category);
        }
    }
}
