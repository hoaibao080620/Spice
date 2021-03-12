using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Extensions;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utilities;

namespace Spice.Areas.Customer.Controllers {
    [Area("Customer")]
    [Authorize]
    public class OrderController : Controller {
        private readonly ApplicationDbContext _dbContext;

        public OrderController(ApplicationDbContext dbContext) {
            _dbContext = dbContext;
        }
        
        public async Task<IActionResult> Confirm(int id) {
            var claimIdentity = (ClaimsIdentity) User.Identity;
            var userId = claimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            
            var viewModel = new DetailAndOrderHeaderViewModel {
                OrderHeader = await _dbContext.OrderHeaders.Include(o => o.ApplicationUser)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId),
                OrderDetails = await _dbContext.OrderDetails.Include(o => o.MenuItem)
                    .Where(o => o.OrderId == id).ToListAsync()
            };
            
            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(OrderHistoryQueryString queryString) {
        
            var claimIdentity = (ClaimsIdentity) User.Identity;
            var userId = claimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            
            var ordersHistory = new List<DetailAndOrderHeaderViewModel>();
            var orders =await _dbContext.OrderHeaders.Include(o => o.ApplicationUser)
                .Where(o => o.UserId == userId).ApplySort(queryString.OrderBy).ToListAsync();
            foreach (var order in orders) {
                var orderHistory = new DetailAndOrderHeaderViewModel {
                    OrderHeader = order,
                    OrderDetails = await _dbContext.OrderDetails.Where(o => o.OrderId == order.Id).ToListAsync()
                };
                ordersHistory.Add(orderHistory);
            }
            

            return View(PagedList<DetailAndOrderHeaderViewModel>.Paging(ordersHistory,
                queryString.PageNum,queryString.PageSize));
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderHistory(int id) {
            
            var orderDetail = new DetailAndOrderHeaderViewModel {
                OrderHeader = await _dbContext.OrderHeaders.Include(o => o.ApplicationUser)
                    .FirstOrDefaultAsync(o => o.Id == id),
                OrderDetails = await _dbContext.OrderDetails.Include(o => o.MenuItem).Where(o => o.OrderId == id).ToListAsync()
            };

            return PartialView("_OrderDetailPartialView", orderDetail);
        }
        
        
        [HttpGet]
        public async Task<IActionResult> GetOrderStatus(int id) {
            var order = await _dbContext.OrderHeaders.FirstOrDefaultAsync(o => o.Id == id);
            return PartialView("_OrderStatusPartialView", order);
        }
    }
}