using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit.Text;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utilities;

namespace Spice.Areas.Customer.Controllers {
    [Area("Customer")]
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _webHost;

        public HomeController(ILogger<HomeController> logger,
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHost) {
            _logger = logger;
            _dbContext = dbContext;
            _webHost = webHost;
        }
        
        
        public async Task<IActionResult> Index() {
            var viewModel = new HomePageViewModel() {
                Coupons = await _dbContext.Coupons.Where(m => m.IsActive == true).ToListAsync(),
                Categories = await _dbContext.Categories.ToListAsync(),
                MenuItems = await _dbContext.MenuItems.Include(m => m.SubCategory)
                    .Include(m => m.Category).ToListAsync()
            };

            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null) {
                var cartCount = await _dbContext.ShoppingCarts
                    .Where(s => s.UserId == claim.Value).CountAsync();
                HttpContext.Session.SetInt32(StaticData.CartCount,cartCount);
            }

            var webPath = _webHost.WebRootPath;
            var files = Directory.GetFiles($@"{webPath}\images\Gallery");
            for (var i = 0;i<files.Length;i++) {
                files[i] = files[i].Substring(webPath.Length);
            }

            viewModel.Images = files;
            
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
                    MenuItemId = menuItemFromDb.Id,
                    DateCreated = DateTime.Now
                };
                return View(shoppingCartView);
            }
            shoppingCart.MenuItem.Id = shoppingCart.MenuItemId;
            shoppingCart.DateCreated = DateTime.Now;
            
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
