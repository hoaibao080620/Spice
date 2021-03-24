using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NETCore.MailKit.Core;
using Spice.Data;
using Spice.Models;
using Spice.Utilities;

namespace Spice.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext dbContext,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
            _dbContext = dbContext;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
            [Required]
            [StringLength(50, MinimumLength = 8,ErrorMessage = "Not Valid (>=8 characters)")]
            public string Username { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string Name { get; set; }
            
            
            public string PhoneNumber { get; set; }
            public string Address { get; set; }
            public string Image { get; set; }
        }

        public class InputModelValidator : AbstractValidator<InputModel> {
            public InputModelValidator() {
                RuleFor(x => x.PhoneNumber).NotNull().Matches(@"\+(84[3|5|7|8|9])+([0-9]{8})\b")
                    .WithMessage("Enter correct format, start with +84");
            }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        
        public async Task<IActionResult> OnPostAsync(string returnUrl = null) {
            var role = Request.Form["role"].ToString();
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                if (await _userManager.Users.FirstOrDefaultAsync(u => u.Email == Input.Email) != null) {
                    TempData.Remove("TempMessage");
                    TempData["TempMessage"] = "This email is already exist, try another one!";
                    return Page();
                }
                
                if (await _userManager.Users
                    .FirstOrDefaultAsync(u => u.UserName == Input.Username) != null) {
                    TempData.Remove("TempMessage");
                    TempData["TempMessage"] = "This username is already exist, try another one!";
                    
                    return Page();
                }

                if (await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == Input.PhoneNumber && u.PhoneNumberConfirmed) != null) {
                    TempData.Remove("TempMessage");
                    TempData["TempMessage"] = "This phone is already register!";
                    
                    return Page();
                }
                var user = new ApplicationUser() {
                    UserName = Input.Username,
                    Email = Input.Email,
                    Name = Input.Name,
                    Address = Input.Address,
                    PhoneNumber = Input.PhoneNumber
                };

                
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded) {
                    
                    if (string.IsNullOrEmpty(role)) {
                        role = UserRole.EndCustomer;
                    }
                    
                    await _userManager.AddToRoleAsync(user, role);
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
                    _logger.LogInformation("User created a new account with password.");

                    if (role == UserRole.EndCustomer) {
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var hostname = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
                        var link = hostname + Url.Action("ConfirmEmail",
                            "Email", new { area = "Customer", userId = user.Id, code });
                        var message = $"Link Confirm email: <a href=\"{link}\">Here</a>";
                        await _emailSender.SendEmailAsync(user.Email, user.Name, message);
                        return RedirectToAction("EmailConfirmSuccess", "Email", new { area = "Customer" });
                    }
                    user.EmailConfirmed = true;
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction("Index", "User",new {area = "Admin"});
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
