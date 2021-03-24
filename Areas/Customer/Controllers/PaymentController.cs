using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BraintreeHttp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PayPal.Core;
using PayPal.v1.Payments;
using Spice.Data;
using Spice.Models;
using Spice.Utilities;
using Stripe;
using Stripe.Checkout;
using Coupon = Spice.Models.Coupon;

namespace Spice.Areas.Customer.Controllers {
    [Area("Customer")]
    [Authorize]
    public class PaymentController : Controller {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly string _clientId;
        private readonly string _secretKey;

        public PaymentController(ApplicationDbContext applicationDbContext,IConfiguration configuration) {
            _dbContext = applicationDbContext;
            _configuration = configuration;
            _clientId = configuration["Paypal:ClientId"];
            _secretKey = configuration["Paypal:SecretKey"];
        }
        // GET
        public IActionResult Payment() {
            return View();
        }
        private string GetCurrentUserId() {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            return claim?.Value;
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
        
        private async Task CommonPayment(ShoppingCartDetailViewModel cartDetail, CancellationToken cal) {
            
            var userId = GetCurrentUserId();
            
            cartDetail.Carts = await _dbContext.ShoppingCarts.Where(s => s.UserId == userId).ToListAsync(cal);
            cartDetail.OrderHeader.ApplicationUser =
                await _dbContext.ApplicationUser.FirstOrDefaultAsync(a => a.Id == userId,cal);
            
            cartDetail.OrderHeader.UserId = userId;
            cartDetail.OrderHeader.Status = OrderStatus.PaymentStatusPending;
            cartDetail.OrderHeader.PaymentStatus = OrderStatus.PaymentStatusPending;
            cartDetail.OrderHeader.PickupDate = DateTime.Now;
            cartDetail.OrderHeader.PickupTime = Convert.ToDateTime(
                $"{cartDetail.OrderHeader.PickupDate.ToShortDateString()} " +
                $"{cartDetail.OrderHeader.PickupTime.ToShortTimeString()}");

            await _dbContext.AddAsync(cartDetail.OrderHeader, cal);
            await _dbContext.SaveChangesAsync(cal);

            cartDetail.OrderHeader.TotalOriginal = 0;
            foreach (var item in cartDetail.Carts) {
                item.MenuItem = await _dbContext.MenuItems
                    .FirstOrDefaultAsync(m => m.Id == item.MenuItemId, cal);
                var orderDetail = new OrderDetail {
                    OrderId = cartDetail.OrderHeader.Id,
                    MenuItemId = item.MenuItemId,
                    Name = item.MenuItem.ItemName,
                    Description = item.MenuItem.description,
                    Count = item.Count,
                    Price = item.MenuItem.Price
                };
                await _dbContext.OrderDetails.AddAsync(orderDetail, cal);
                cartDetail.OrderHeader.TotalOriginal += (item.Count * item.MenuItem.Price);
            }

            if (HttpContext.Session.GetString(StaticData.CouponCode)!=null) {
                var coupon = await _dbContext.Coupons
                    .FirstOrDefaultAsync(c => c.CouponName.ToLower()
                    == HttpContext.Session.GetString(StaticData.CouponCode).ToLower(), cal);
                cartDetail.OrderHeader.TotalFinal = GetDiscount(coupon, cartDetail.OrderHeader.TotalOriginal);
            }
            else {
                cartDetail.OrderHeader.TotalFinal = cartDetail.OrderHeader.TotalOriginal;
            }

            cartDetail.OrderHeader.CouponCodeDiscount =
                cartDetail.OrderHeader.TotalOriginal - cartDetail.OrderHeader.TotalFinal;
            
            
            HttpContext.Session.Remove(StaticData.CouponCode);
            await _dbContext.SaveChangesAsync(cal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(ShoppingCartDetailViewModel cartDetail) {
            await CommonPayment(cartDetail, new CancellationToken(false));
            var userId = GetCurrentUserId();
            var hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            var items = new List<SessionLineItemOptions>();
            foreach (var cart in cartDetail.Carts) {
                items.Add(new SessionLineItemOptions() {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        ProductData = new SessionLineItemPriceDataProductDataOptions {
                            Name = cart.MenuItem.ItemName,
                        },
                        UnitAmount = (int)(cart.MenuItem.Price*cart.Count*100),
                        Currency = "usd",
                        
                    },
                    Quantity = cart.Count
                });
            }
            
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                LineItems = items
                ,
               
                
                Mode = "payment",
                SuccessUrl = $"{hostname}/Customer/Order/Confirm?id=" +
                             $"{cartDetail.OrderHeader.Id}",
                CancelUrl = $"{hostname}/Customer/Payment/Fail",
               
            Metadata = new Dictionary<string, string> {
                    {"description",$"{cartDetail.OrderHeader.Id}"},
                    {"userId",$"{userId}" }
                }
                
            };

            
            
            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            
            
            return RedirectToAction(nameof(RedirectToStripe),new {session = session.Id});
        }

        public IActionResult RedirectToStripe(string session) {
            if (session is null) {
                return BadRequest();
            }
            return View("RedirectToStripe", session);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Processing() {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try {
                var stripeEvent = EventUtility.ConstructEvent(
                  json,
                  Request.Headers["Stripe-Signature"],
                  "whsec_auRuHfcMAuZK5DltKerSAmm7Zzbv8BS0"
                );
                if (stripeEvent.Type == Events.CheckoutSessionCompleted) {
                        var session = stripeEvent.Data.Object as Session;
                        var paymentIntent = await new PaymentIntentService().GetAsync(session?.PaymentIntentId);
                        var charge = paymentIntent.Charges.Data[0];
                        var orderId = Convert.ToInt32(session?.Metadata["description"]);
                        var order = await _dbContext.OrderHeaders.FirstOrDefaultAsync(o => o.Id == orderId);

                        if (charge.BalanceTransactionId != null) {
                            order.TransactionId = charge.BalanceTransactionId;
                        }
                        else {
                            order.PaymentStatus = OrderStatus.PaymentStatusPending;
                        }


                        if (session?.PaymentStatus == "paid") {
                            var userId = session?.Metadata["userId"];
                            var carts = await _dbContext.ShoppingCarts.Where(s => s.UserId == userId).ToListAsync();
                            _dbContext.ShoppingCarts.RemoveRange(carts);
                            order.PaymentStatus = OrderStatus.StatusSubmitted;
                            order.Status = OrderStatus.StatusSubmitted;
                        }
                        else {
                            order.PaymentStatus = OrderStatus.PaymentStatusPending;
                        }

                        await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception) {

                return BadRequest();
            }
            return Ok();

            
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PayPalPayment(ShoppingCartDetailViewModel cartDetail) {
            await CommonPayment(cartDetail,new CancellationToken());
            var environment = new SandboxEnvironment(_clientId, _secretKey);
            var client = new PayPalHttpClient(environment);
            var itemList = new ItemList()
            {
                Items = new List<Item>()
            };

            var claimIdentity = (ClaimsIdentity) User.Identity;
            var userId = claimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            cartDetail.Carts = await _dbContext.ShoppingCarts.Where(s => s.UserId == userId).ToListAsync();

            var total = 0.0;
            foreach (var item in cartDetail.Carts) {
                item.MenuItem = await _dbContext.MenuItems.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                total += item.MenuItem.Price*item.Count;
                itemList.Items.Add(new Item {
                    Name = item.MenuItem.ItemName,
                    Currency = "USD",
                    Price = item.MenuItem.Price.ToString(CultureInfo.InvariantCulture),
                    Quantity = item.Count.ToString(),
                    Sku = "sku",
                    Tax = "0"
                });
            }
            
            var hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            
            var payment = new Payment()

            {
                Intent = "SALE",
                Transactions = new List<Transaction>() 
                {
                    new Transaction()
                    {
                        Amount = new Amount()
                        {
                            Total = total.ToString(CultureInfo.InvariantCulture),
                            Currency = "USD"
                        },
                        ItemList = itemList,
                        Description = $"Invoice #{cartDetail.OrderHeader.Id}",
                        InvoiceNumber = cartDetail.OrderHeader.Id.ToString()
                    }
                },
                RedirectUrls = new RedirectUrls() 
                {
                    CancelUrl = $"{hostname}/Customer/Payment/Fail",
                    ReturnUrl = $"{hostname}/Customer/Payment/PaypalSuccess?orderId={cartDetail.OrderHeader.Id}"
                },
                
                Payer = new Payer
                {
                    PaymentMethod = "paypal"
                }
            };

            PaymentCreateRequest request = new PaymentCreateRequest();
            request.RequestBody(payment);
            
            try {
                var response = await client.Execute(request);
                var statusCode = response.StatusCode;
                Payment result = response.Result<Payment>();
                
                using var links = result.Links.GetEnumerator();
                string paypalRedirectUrl = null;
                while (links.MoveNext())
                {
                    LinkDescriptionObject lnk = links.Current;
                    if (lnk != null && lnk.Rel.ToLower().Trim().Equals("approval_url"))
                    {
                        
                        //saving the payapalredirect URL to which user will be redirected for payment  
                        paypalRedirectUrl = lnk.Href;
                    }
                }

                return Redirect(paypalRedirectUrl);
            } 
            catch(HttpException httpException) 
            {
                var statusCode = httpException.StatusCode;
                var debugId = httpException.Headers.GetValues("PayPal-Debug-Id").FirstOrDefault();

                return RedirectToAction(nameof(Fail));
            }
        }
        
        public async Task<IActionResult> PaypalSuccess(int orderId,string paymentId,string payerId) {
            var order = await _dbContext.OrderHeaders.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == orderId);
            order.PaymentStatus = OrderStatus.PaymentStatusApproved;
            order.Status = OrderStatus.StatusSubmitted;
            order.TransactionId = paymentId;
            
            var claimIdentity = (ClaimsIdentity) User.Identity;
            var userId = claimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            var carts =  _dbContext.ShoppingCarts.Where(s => s.UserId == userId).ToList();
            _dbContext.ShoppingCarts.RemoveRange(carts);
            
            var request = new PaymentExecuteRequest(paymentId);

            request.RequestBody(new PaymentExecution() {
                PayerId = payerId
            });
            
            var environment = new SandboxEnvironment(_clientId, _secretKey);
            var client = new PayPalHttpClient(environment);

            var response = await client.Execute(request);
            var payment = response.Result<Payment>();

            if (payment.State != "approved") { // no blance or something else?
                return RedirectToAction(nameof(Fail));
            }

            
            var hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Confirm","Order",new {
                area = "Customer",
                id = orderId,
            });
        }

        public IActionResult Fail() {
            return View();
        }
        
        
    }
}