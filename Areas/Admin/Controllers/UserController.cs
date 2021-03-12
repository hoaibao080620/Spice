using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utilities;

namespace Spice.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize(Roles = UserRole.Manager)]
    public class UserController : Controller {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IPasswordHasher<IdentityUser> _passwordHasher;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public UserController(ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            IPasswordHasher<IdentityUser> passwordHasher,
            IMapper mapper,
            IWebHostEnvironment webHostEnvironment) {
            _dbContext = dbContext;
            _userManager = userManager;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index() {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            
            // user.EmailConfirmed = true;
            // _dbContext.ApplicationUser.Update(user);
            // await _dbContext.SaveChangesAsync();
            var users = await _dbContext.ApplicationUser
                .Where(u => u.Id != claim.Value).ToListAsync();
            var userViewModels = new List<ApplicationUserViewModel>();
            users.ForEach(u => userViewModels.Add(new ApplicationUserViewModel() {
                ApplicationUser = u,
                Roles = _userManager.GetRolesAsync(u).GetAwaiter().GetResult().ToList()
            }));
            return View(userViewModels);
        }

        private async Task<bool> IsManager(string id) {
            var user = await _dbContext.ApplicationUser
                .FirstOrDefaultAsync(u => u.Id == id);
            var roles = await _userManager.GetRolesAsync(user);
            
            return roles.Contains(UserRole.Manager);
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

        [HttpGet]
        public async Task<IActionResult> Update(string id) {
            var userFromDb = await _dbContext.ApplicationUser
                .FirstOrDefaultAsync(a => a.Id == id);
            if (userFromDb is null) {
                return NotFound();
            }
            

            return View(userFromDb);
        }

        [HttpPost]
        public async Task<IActionResult> Update(ApplicationUser userDto) {
            if (!ModelState.IsValid) {
                return BadRequest();
            }

            var user = await _dbContext.ApplicationUser
                .FirstOrDefaultAsync(u => u.Id == userDto.Id);
            if (user == null)
                return NotFound();
            
            _mapper.Map(userDto,user);
            var webroot = _webHostEnvironment.WebRootPath;
            var files = Request.Form.Files;
            if (files.Count > 0) {
                var extension = Path.GetExtension(files[0].FileName);
                var upload = Path.Combine(webroot, @"images\User");
                var destinationUserImage = upload+@$"\{user.Id}{extension}";
                
                //Delete old image
                var userImagePath = new DirectoryInfo(upload).GetFiles();
                foreach (var file in userImagePath) {
                    var fileName = file.Name.Trim().Split(".")[0].Trim();
                    if (fileName == user.Id) {
                        System.IO.File.Delete(file.FullName);
                        break;
                    }
                }
                //Copy new image to images folder
                await using (var fileStream = new FileStream(destinationUserImage, FileMode.Create)) {
                    await files[0].CopyToAsync(fileStream);
                }
                user.Image = @"\images\User\" + $"{user.Id}{extension}";
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        public async Task<IActionResult> Delete([FromQuery]string id) {
            var userFromDb = await _dbContext.ApplicationUser
                .FirstOrDefaultAsync(a => a.Id == id);
            if (userFromDb is null) {
                return NotFound();
            }

            var image = _webHostEnvironment.WebRootPath + userFromDb.Image;
            
            _dbContext.ApplicationUser.Remove(userFromDb);
            await _dbContext.SaveChangesAsync();
            if (System.IO.File.Exists(image)) {
                System.IO.File.Delete(image);
            }
            return RedirectToAction(nameof(Index));
        }
        
        
    }
    
    
}