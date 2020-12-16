using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Utilities;

namespace Spice.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize(Roles = UserRole.Manager)]
    public class UserController : Controller {
        private readonly ApplicationDbContext _dbContext;

        public UserController(ApplicationDbContext dbContext) {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index() {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            return View(await _dbContext.ApplicationUser.Where(u => u.Id != claim.Value).ToListAsync());
        }

        public async Task<IActionResult> Lock(string id) {
            if (id is null)
                return NotFound();
            var user = _dbContext.ApplicationUser.FirstOrDefault(u => u.Id == id);
            
            if (user is null)
                return NotFound();
            user.LockoutEnd = DateTime.Now.AddHours(10);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }
        
        public async Task<IActionResult> Unlock(string id) {
            if (id is null)
                return NotFound();
            var user = _dbContext.ApplicationUser.FirstOrDefault(u => u.Id == id);
            
            if (user is null)
                return NotFound();
            user.LockoutEnd = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }
    }
    
    
}