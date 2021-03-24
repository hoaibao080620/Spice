using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Spice.Data;
using Spice.Models;
using Spice.Utilities;
using Stripe;
using Coupon = Spice.Models.Coupon;

namespace Spice.Areas.Customer.Controllers {
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller {
        private readonly ApplicationDbContext _dbContext;

        public CartController(ApplicationDbContext dbContext) {
            _dbContext = dbContext;
        }

        private string GetCurrentUserId() {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            return claim?.Value;
        }
        [Authorize]
        public async Task<IActionResult> Index() {
            var shoppingCartDetail = new ShoppingCartDetailViewModel() {
                OrderHeader = new OrderHeader()
            };
            var currentUserId = GetCurrentUserId();
            var carts =await _dbContext.ShoppingCarts
                .Where(s => s.UserId == currentUserId).ToListAsync();

            if (carts != null) {
                shoppingCartDetail.Carts = carts;
            }
            var totalPrice = await GetTotalPrice(carts);
            
            shoppingCartDetail.OrderHeader.TotalOriginal = totalPrice;
            shoppingCartDetail.OrderHeader.TotalFinal = shoppingCartDetail.OrderHeader.TotalOriginal;
            // Set coupon code to session
            if (HttpContext.Session.GetString(StaticData.CouponCode) != null) {
                var coupon = await _dbContext.Coupons
                    .FirstOrDefaultAsync(c =>
                        c.CouponName.ToLower() == HttpContext.Session.GetString(StaticData.CouponCode).ToLower());
                var finalPrice = GetDiscount(coupon, totalPrice);
                if (totalPrice - finalPrice == 0) {
                    TempData["CodeMessage"] = $"Code is not valid, Minimum amount is {coupon.MinimumAmount}$!";
                    HttpContext.Session.Remove(StaticData.CouponCode);
                }
                else {
                    shoppingCartDetail.OrderHeader.TotalFinal = finalPrice;
                    shoppingCartDetail.OrderHeader.CouponCode = HttpContext.Session.GetString(StaticData.CouponCode);
                }
            }
            return View(shoppingCartDetail);
        }
        
        private async Task<double> GetTotalPrice(IEnumerable<ShoppingCart> shoppingCarts) {
            var totalPrice = 0.0;
            // Get total price of all shopping cart
            foreach (var cart in shoppingCarts) {
                cart.MenuItem = await _dbContext.MenuItems
                    .FirstOrDefaultAsync(m => m.Id == cart.MenuItemId);
                totalPrice += cart.MenuItem.Price * cart.Count;
                if (cart.MenuItem.description.Length > 100) {
                    cart.MenuItem.description = cart.MenuItem.description.Substring(0, 99) + "...";
                }
            }
            return totalPrice;
        }

        private double GetDiscount(Coupon coupon, double totalOriginal) {
            if (coupon.MinimumAmount > totalOriginal)
                return totalOriginal;
            return Convert.ToInt32(coupon.CouponType) switch {
                (int) Coupon.ECouponType.Dollar => Math.Round(totalOriginal - coupon.Discount,2),
                (int) Coupon.ECouponType.Percent => 
                    Math.Round(totalOriginal - (totalOriginal * coupon.Discount / 100),2),
                _ => totalOriginal
            };
        }

        [HttpPost]
        public async Task<IActionResult> AddCouponCode(ShoppingCartDetailViewModel shoppingCartDetail) {
            if (!string.IsNullOrEmpty(shoppingCartDetail.OrderHeader.CouponCode)) {
                var coupon = await _dbContext.Coupons
                    .FirstOrDefaultAsync(c =>
                        c.CouponName.ToLower() == shoppingCartDetail.OrderHeader.CouponCode.ToLower()
                        && c.IsActive);
                if (coupon != null) {
                    HttpContext.Session.SetString(StaticData.CouponCode,shoppingCartDetail.OrderHeader.CouponCode);
                }
                else {
                    TempData["CodeMessage"] = "Code is not correct or no longer active";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult RemoveCouponCode() {
            HttpContext.Session.Remove(StaticData.CouponCode);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId) {
            var cart = await _dbContext.ShoppingCarts.FirstOrDefaultAsync(s => s.CartId == cartId);
            if (cart != null) {
                cart.Count++;
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        
        public async Task<IActionResult> Minus(int cartId) {
            var cart = await _dbContext.ShoppingCarts.FirstOrDefaultAsync(s => s.CartId == cartId);
            if (cart != null) {
                if (cart.Count > 1) {
                    cart.Count--;
                    await _dbContext.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DeleteShoppingCart(int cartId) {
            var cart = await _dbContext.ShoppingCarts.FirstOrDefaultAsync(s => s.CartId == cartId);
            if (cart != null) {
                _dbContext.ShoppingCarts.Remove(cart);
                await _dbContext.SaveChangesAsync();
                var count = await _dbContext.ShoppingCarts
                    .CountAsync(s => s.UserId == cart.UserId);
                HttpContext.Session.SetInt32(StaticData.CartCount,count);
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        public async Task<IActionResult> Summary() {
            var shoppingCartDetail = new ShoppingCartDetailViewModel() {
                OrderHeader = new OrderHeader()
            };

            // Get carts for order
            var currentUserId = GetCurrentUserId();
            var user = await _dbContext.ApplicationUser.FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (!user.PhoneNumberConfirmed) {
                return RedirectToPage("/Account/VerifyPhoneModel",new {area = "Identity"});
            }
            var carts =await _dbContext.ShoppingCarts
                .Where(s => s.UserId == currentUserId).ToListAsync();
            if (carts != null) {
                shoppingCartDetail.Carts = carts;
                if (carts.Count == 0) {
                    return BadRequest();
                }
            }
            //Default Information for Order
            var totalPrice = await GetTotalPrice(carts);
            shoppingCartDetail.OrderHeader.TotalOriginal = totalPrice;
            shoppingCartDetail.OrderHeader.TotalFinal = shoppingCartDetail.OrderHeader.TotalOriginal;
            
            shoppingCartDetail.OrderHeader.PickupName = user.Name;
            shoppingCartDetail.OrderHeader.PhoneNumber = user.PhoneNumber;
            // Set coupon code to session
            if (HttpContext.Session.GetString(StaticData.CouponCode) != null) {
                var coupon = await _dbContext.Coupons
                    .FirstOrDefaultAsync(c =>
                        c.CouponName.ToLower() == HttpContext.Session.GetString(StaticData.CouponCode).ToLower());
                var finalPrice = GetDiscount(coupon, totalPrice);
                shoppingCartDetail.OrderHeader.CouponCodeDiscount = totalPrice - finalPrice;
                shoppingCartDetail.OrderHeader.TotalFinal = finalPrice;
                shoppingCartDetail.OrderHeader.CouponCode = HttpContext.Session.GetString(StaticData.CouponCode);
            }
            return View(shoppingCartDetail);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Summary(ShoppingCartDetailViewModel cartDetail,CancellationToken cal,
                                                    string stripeToken) {
            var option = new ChargeCreateOptions {
                Amount = Convert.ToInt32(cartDetail.OrderHeader.TotalFinal * 100),
                Currency = "usd",
                Description = $"Order Id: {cartDetail.OrderHeader.Id}",
                Source = stripeToken,
                ReceiptEmail = cartDetail.OrderHeader.ApplicationUser.Email
            };

            var services = new ChargeService();
            
            var charge = await services.CreateAsync(option, cancellationToken: cal);
            if (charge.BalanceTransactionId is null) {
                cartDetail.OrderHeader.PaymentStatus = OrderStatus.PaymentStatusRejected;
            }
            else {
                cartDetail.OrderHeader.TransactionId = charge.BalanceTransactionId;
            }

            if (charge.Status == "succeeded") {
                cartDetail.OrderHeader.Status = OrderStatus.StatusSubmitted;
                cartDetail.OrderHeader.PaymentStatus = OrderStatus.PaymentStatusApproved;
            }
            else {
                cartDetail.OrderHeader.PaymentStatus = OrderStatus.PaymentStatusRejected;
            }
            

            await _dbContext.SaveChangesAsync(cal);

            return RedirectToAction("Confirm", "Order", new {id = cartDetail.OrderHeader.Id});
        }
        
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AddToCart(int id) {
            if (!User.Identity.IsAuthenticated) {
                return BadRequest();
            }
            var menuItem = await _dbContext.MenuItems.FirstOrDefaultAsync(m => m.Id == id);
            if (menuItem is null) {
                return BadRequest();
            }
            
            var userId = GetCurrentUserId();
            
            var cart = new ShoppingCart {
                Count = 1,
                MenuItemId = id,
                DateCreated = DateTime.Now
            };

            // await _dbContext.ShoppingCarts.AddAsync(cart);
            // await _dbContext.SaveChangesAsync();
            //
            var existShoppingCart = await _dbContext.ShoppingCarts
                .FirstOrDefaultAsync(s => s.MenuItemId == menuItem.Id
                                          && s.UserId == userId);
            var isExist = false;
            
            if (existShoppingCart is null) {
                cart.UserId = userId;
                await _dbContext.ShoppingCarts.AddAsync(cart);
            }
            else {
                isExist = true;
                existShoppingCart.Count += 1;
            }
            
            await _dbContext.SaveChangesAsync();
            var cartCount = 0;
            if (HttpContext.Session.GetInt32(StaticData.CartCount)!=null) {
                cartCount = (int) (HttpContext.Session.GetInt32(StaticData.CartCount) + 1);
            }

            HttpContext.Session.SetInt32(StaticData.CartCount, cartCount);
            return Ok(JsonConvert.SerializeObject(new {isExist}));
        }
        

        
    }
}