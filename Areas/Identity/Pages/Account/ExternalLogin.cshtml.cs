using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spice.Data;
using Spice.Models;
using Spice.Utilities;

namespace Spice.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _dbContext;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext dbContext,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
            this._dbContext = dbContext;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
            [Required]
            public string Name { get; set; }
            [Required]
            [RegularExpression(@"\+(84[3|5|7|8|9])+([0-9]{8})\b",ErrorMessage = "Start with +84")]
            public string PhoneNumber { get; set; }
            [Required]
            public string Address { get; set; }
        }

        

        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new {ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            
            if (await _userManager.Users
                .FirstOrDefaultAsync(u => u.Email == 
                    info.Principal.FindFirstValue(ClaimTypes.Email) && !u.EmailConfirmed) != null ) {
                TempData["AlreadyTaken"] = "Your email has been register!";
                return RedirectToPage("Login");
            }
                
            

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor : true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    Input = new InputModel
                    {
                        Email = info.Principal.FindFirstValue(ClaimTypes.Email),
                        Name = info.Principal.FindFirstValue(ClaimTypes.Name)
                    };
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid) {
                var user = new ApplicationUser() {
                    Email = Input.Email,
                    Name = Input.Name,
                    PhoneNumber = Input.PhoneNumber,
                    Address = Input.Address,
                    UserName = Input.Email,
                    EmailConfirmed = true
                };
                if (await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == Input.PhoneNumber && u.PhoneNumberConfirmed) != null) {
                    TempData.Remove("TempMessage");
                    TempData["TempMessage"] = "This phone is already register!";
                    return Page();
                }

                var result = await _userManager.CreateAsync(user);
                
                if (result.Succeeded) {
                    var webroot = _webHostEnvironment.WebRootPath;
                    var files = Request.Form.Files;
                    var userFromDb = await _dbContext.ApplicationUser
                        .FirstOrDefaultAsync(u => u.Email == Input.Email);
                    if (files.Count > 0) {
                        var extension = Path.GetExtension(files[0].FileName);
                        var destinationUserImage = Path.Combine(webroot, @"images\User")+@$"\{userFromDb.Id}{extension}";
                        await using (var fileStream = new FileStream(destinationUserImage, FileMode.Create)) {
                            await files[0].CopyToAsync(fileStream);
                        }
                        userFromDb.Image = @"\images\User\" + $"{userFromDb.Id}{extension}";
                    }
                    else {
                        var defaultUserImage = Path.Combine(webroot, @"images\User")+$"\\{StaticData.DefaultUserImage}";
                        var destinationUserImage = Path.Combine(webroot, @"images\User")+@$"\{userFromDb.Id}.jpg";
                        System.IO.File.Copy(defaultUserImage,destinationUserImage);
                        userFromDb.Image = @$"\images\User\{userFromDb.Id}.jpg";
                    }
                    
                    await _dbContext.SaveChangesAsync();
                    await _userManager.AddToRoleAsync(user, UserRole.EndCustomer);
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
