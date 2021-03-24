using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using NETCore.MailKit.Core;
using Spice.Data;

namespace Spice.Areas.Customer.Controllers {
    [Area("Customer")]
    public class EmailController : Controller {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailService;
        private readonly ApplicationDbContext dbContext;

        public EmailController(UserManager<IdentityUser> userManager, IEmailSender emailService
            ,ApplicationDbContext dbContext) {
            _userManager = userManager;
            this._emailService = emailService;
            this.dbContext = dbContext;
        }
        public async Task<IActionResult> ConfirmEmail(string userId,string code) {
            var user =await _userManager.FindByIdAsync(userId);
            if (user is null) {
                return BadRequest();
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded) {
                return View();
            }
            return BadRequest();
        }

        public IActionResult EmailConfirmSuccess() {
            
            return View();
        }
    }
}