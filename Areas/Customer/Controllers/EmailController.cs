using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NETCore.MailKit.Core;

namespace Spice.Areas.Customer.Controllers {
    [Area("Customer")]
    public class EmailController : Controller {
        private readonly UserManager<IdentityUser> _userManager;

        public EmailController(UserManager<IdentityUser> userManager) {
            _userManager = userManager;
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