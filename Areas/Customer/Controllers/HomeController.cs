using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utilities;

namespace Spice.Areas.Customer.Controllers {
    [Area("Customer")]
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext) {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index() {
            var viewModel = new HomePageViewModel() {
                Coupons = await _dbContext.Coupons.Where(m => m.IsActive == true).ToListAsync(),
                Categories = await _dbContext.Categories.ToListAsync(),
                MenuItems = await _dbContext.MenuItems.Include(m => m.SubCategory)
                    .Include(m => m.Category).ToListAsync()
            };

            // var claimsIdentity = (ClaimsIdentity) User.Identity;
            // var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            //
            // var cartCount = await _dbContext.ShoppingCarts
            //     .Where(s => s.UserId == userId).CountAsync();
            // HttpContext.Session.SetInt32(StaticData.CartCount,cartCount);
            
            return View(viewModel);
        }

        public IActionResult Privacy() {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Detail(int id) {
            var menuItemFromDb = await _dbContext.MenuItems
                .Include(m => m.Category)
                .Include(m => m.SubCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (menuItemFromDb is null)
                return NotFound();

            var shoppingCart = new ShoppingCart() {
                MenuItem = menuItemFromDb,
                MenuItemId = menuItemFromDb.Id
            };
            return View(shoppingCart);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detail(ShoppingCart shoppingCart) {
            if (!ModelState.IsValid) {
                var menuItemFromDb = await _dbContext.MenuItems
                    .Include(m => m.Category)
                    .Include(m => m.SubCategory)
                    .FirstOrDefaultAsync(m => m.Id == shoppingCart.MenuItemId);
                if (menuItemFromDb is null)
                    return NotFound();

                var shoppingCartView = new ShoppingCart() {
                    MenuItem = menuItemFromDb,
                    MenuItemId = menuItemFromDb.Id
                };
                return View(shoppingCartView);
            }

            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var existShoppingCart = await _dbContext.ShoppingCarts
                .FirstOrDefaultAsync(s => s.MenuItemId == shoppingCart.MenuItemId
                                          && s.UserId == userId);
            if (existShoppingCart is null) {
                shoppingCart.UserId = userId;
                await _dbContext.ShoppingCarts.AddAsync(shoppingCart);
            }
            else {
                existShoppingCart.Count += shoppingCart.Count;
            }

            
            await _dbContext.SaveChangesAsync();
            var cartCount = await _dbContext.ShoppingCarts
                .Where(s => s.UserId == userId).CountAsync();
            HttpContext.Session.SetInt32(StaticData.CartCount,cartCount);
            
            return RedirectToAction(nameof(Index));
        }
    }
}
