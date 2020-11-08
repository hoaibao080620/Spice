using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;

namespace Spice.Areas.Admin.Controllers {
    [Area("Admin")]
    public class SubCategoriesController : Controller {
        private readonly ApplicationDbContext dbContext;
        [TempData]
        public string StatusMessage { get; set; }

        public SubCategoriesController(ApplicationDbContext dbContext) {
            this.dbContext = dbContext;
        }
        //public async Task<IActionResult> Index() {
        //    var SubCategories = await dbContext.SubCategories.Include(s => s.Category).ToListAsync();
        //    return View(SubCategories);
        //}

        //public async Task<IActionResult> Create() {
        //    SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel() {
        //        Categories = await dbContext.Categories.ToListAsync(),
        //        SubCategory = new SubCategory(),
        //        SubCategoriesExist = await dbContext.SubCategories.OrderBy(s => s.SubCategoryName)
        //                                .Select(s => s.SubCategoryName).Distinct().ToListAsync()
        //    };

        //    return View(model);
        //}

        //[HttpPost]
        //public async Task<IActionResult> Create(SubCategoryAndCategoryViewModel model) {
        //    if (ModelState.IsValid) {
        //        var isExist = await dbContext.SubCategories.Include(s => s.Category)
        //            .Where(s => s.SubCategoryName == model.SubCategory.SubCategoryName && s.Category.Id==
        //            model.SubCategory.CategoryId).ToListAsync();      
        //        if(isExist.Count() > 0) {
        //            StatusMessage = $"Error Sub Category Exist in {isExist.First().Category.Name}";
        //        }
        //        else {
        //            await dbContext.SubCategories.AddAsync(model.SubCategory);
        //            await dbContext.SaveChangesAsync();
        //            return RedirectToAction(nameof(Index));
        //        }
        //    }
        //    var modelVw = new SubCategoryAndCategoryViewModel() {
        //        Categories = await dbContext.Categories.ToListAsync(),
        //        SubCategory = model.SubCategory,
        //        SubCategoriesExist = await dbContext.SubCategories.OrderBy(s => s.SubCategoryName)
        //                                .Select(s => s.SubCategoryName).Distinct().ToListAsync(),
        //        StatusMessage = this.StatusMessage
        //    };
        //    return View(modelVw);
        //}

        //public async Task<IActionResult> GetSubCategoriesExist(int id) {
        //    var categoriesExist = await dbContext.SubCategories.Where(s => s.CategoryId == id).ToListAsync();
        //    return Json(categoriesExist);

        //}
        //[HttpGet]
        //public async Task<IActionResult> Edit(int? id) {
        //    if(id == null) {
        //        return NotFound();
        //    }
        //    var subCategory = await dbContext.SubCategories.FirstOrDefaultAsync(s => s.Id == id);
        //    if(subCategory == null) {
        //        return NotFound();
        //    }

        //    SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel() {
        //        Categories = await dbContext.Categories.ToListAsync(),
        //        SubCategory = subCategory,
        //        SubCategoriesExist = await dbContext.SubCategories.OrderBy(s => s.SubCategoryName)
        //                                .Select(s => s.SubCategoryName).Distinct().ToListAsync()
        //    };

        //    return View(model);
        //}

        //[HttpPost]
        //public async Task<IActionResult> Edit(int id ,SubCategoryAndCategoryViewModel model) {
        //    if (ModelState.IsValid) {
        //        var isExist = await dbContext.SubCategories.Include(s => s.Category)
        //            .Where(s => s.SubCategoryName == model.SubCategory.SubCategoryName && s.Category.Id ==
        //            model.SubCategory.CategoryId).ToListAsync();
        //        if (isExist.Count() > 0) {
        //            StatusMessage = $"Error Sub Category Exist in {isExist.First().Category.Name}";
        //        }
        //        else {
        //            var subCategoryFromDb = await dbContext.SubCategories.
        //                FirstOrDefaultAsync(s => s.Id == id);
        //            subCategoryFromDb.SubCategoryName = model.SubCategory.SubCategoryName;
        //            await dbContext.SaveChangesAsync();
        //            return RedirectToAction(nameof(Index));
        //        }
        //    }
        //    var modelVw = new SubCategoryAndCategoryViewModel() {
        //        Categories = await dbContext.Categories.ToListAsync(),
        //        SubCategory = model.SubCategory,
        //        SubCategoriesExist = await dbContext.SubCategories.OrderBy(s => s.SubCategoryName)
        //                                .Select(s => s.SubCategoryName).Distinct().ToListAsync(),
        //        StatusMessage = this.StatusMessage
        //    };
        //    return View(modelVw);
        //}

        //public async Task<IActionResult> Detail(int? id) {
        //    if(id == null) {
        //        return NotFound();
        //    }

        //    var subCategory = await dbContext.SubCategories.Include(s => s.Category).FirstOrDefaultAsync(s => s.Id == id);
        //    if(subCategory == null) {
        //        return NotFound();
        //    }

        //    return View(subCategory);
        //}
        //[HttpGet]
        //public async Task<IActionResult> Delete(int? id) {
        //    if (id == null) {
        //        return NotFound();
        //    }

        //    var subCategory = await dbContext.SubCategories.Include(s => s.Category).FirstOrDefaultAsync(s => s.Id == id);
        //    if (subCategory == null) {
        //        return NotFound();
        //    }

        //    return View(subCategory);
        //}

        //[HttpPost]
        //public async Task<IActionResult> Delete(int id) {
        //    var subCategory = await dbContext.SubCategories.Include(s => s.Category).FirstOrDefaultAsync(s => s.Id == id);
        //    if (subCategory == null) {
        //        return NotFound();
        //    }
        //    else {
        //        dbContext.SubCategories.Remove(subCategory);
        //        await dbContext.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //}



        public async Task<IActionResult> Index() {
            var subCategories = await dbContext.SubCategories.Include(s => s.Category).ToListAsync();
            return View(subCategories);
        }

        public async Task<IActionResult> Create() {
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel() {
                Categories = await dbContext.Categories.ToListAsync(),
                SubCategory = new SubCategory(),
                SubCategoriesExist = await dbContext.SubCategories
                    .Distinct().Select(s => s.SubCategoryName).ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SubCategoryAndCategoryViewModel model) {
            if(ModelState.IsValid) {
                var isSubCategoriesExist = await dbContext.SubCategories.Include(s => s.Category)
                    .Where(s => s.CategoryId == model.SubCategory.CategoryId && s.SubCategoryName
                    == model.SubCategory.SubCategoryName).ToListAsync();
                if(isSubCategoriesExist.Count()>0) {
                    StatusMessage = $"Error : Exist in {isSubCategoriesExist.First().Category.Name}";
                }
                else {
                    dbContext.SubCategories.Add(model.SubCategory);
                    await dbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }



            SubCategoryAndCategoryViewModel modelVM = new SubCategoryAndCategoryViewModel() {
                Categories = await dbContext.Categories.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoriesExist = await dbContext.SubCategories
                    .Distinct().Select(s => s.SubCategoryName).ToListAsync(),
                StatusMessage = this.StatusMessage
            };

            return View(modelVM);
        }

        public async Task<IActionResult> GetSubCategoriesExist(int id) {
            var subCategories = await dbContext.SubCategories.Where(s => s.CategoryId == id).ToArrayAsync();
            return Json(subCategories);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id) {
            if (id == null)
                return NotFound();
            var subCategory = await dbContext.SubCategories.FindAsync(id);
            if(subCategory == null) {
                return NotFound();
            }

            SubCategoryAndCategoryViewModel modelVM = new SubCategoryAndCategoryViewModel() {
                Categories = await dbContext.Categories.ToListAsync(),
                SubCategory = subCategory,
                SubCategoriesExist = await dbContext.SubCategories
                    .Distinct().Select(s => s.SubCategoryName).ToListAsync()
            };

            return View(modelVM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id,SubCategoryAndCategoryViewModel model) {
            var subCategory = await dbContext.SubCategories.FirstOrDefaultAsync(s => s.Id == id);
            if (subCategory == null)
                return NotFound();
            if (ModelState.IsValid) {
                var isSubCategoriesExist = await dbContext.SubCategories.Include(s => s.Category)
                    .Where(s => s.CategoryId == model.SubCategory.CategoryId && s.SubCategoryName
                    == model.SubCategory.SubCategoryName).ToListAsync();
                if (isSubCategoriesExist.Count() > 0) {
                    StatusMessage = $"Error : Exist in {isSubCategoriesExist.First().Category.Name}";
                }
                else {
                    subCategory.SubCategoryName = model.SubCategory.SubCategoryName;
                    await dbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            SubCategoryAndCategoryViewModel modelVM = new SubCategoryAndCategoryViewModel() {
                Categories = await dbContext.Categories.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoriesExist = await dbContext.SubCategories
                    .Distinct().Select(s => s.SubCategoryName).ToListAsync(),
                StatusMessage = this.StatusMessage
            };

            return View(modelVM);
        }

        public async Task<IActionResult> Detail(int? id) {
            if (id == null)
                return NotFound();
            var subCategory = await dbContext.SubCategories.Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (subCategory == null) {
                return NotFound();
            }
            return View(subCategory);
        }

        public async Task<IActionResult> Delete(int? id) {
            if (id == null)
                return NotFound();
            var subCategory = await dbContext.SubCategories.Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (subCategory == null) {
                return NotFound();
            }
            return View(subCategory);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id) {
            var subCategory = await dbContext.SubCategories.Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (subCategory == null) {
                return NotFound();
            }
            dbContext.SubCategories.Remove(subCategory);
            await dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
