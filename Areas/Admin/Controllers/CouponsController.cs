using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Utilities;

namespace Spice.Areas.Admin.Controllers {
    [Area("Admin")]
    [Authorize(Roles = UserRole.Manager+","+UserRole.FrontDesk)]
    public class CouponsController : Controller {
        private readonly ApplicationDbContext _db;

        public CouponsController(ApplicationDbContext db) {
            this._db = db;
        }

        public async Task<IActionResult> Index() {
            return View(await _db.Coupons.ToListAsync());
        }

        [HttpGet]
        public IActionResult Create() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Coupon coupon) {
            if (!ModelState.IsValid) {
                return NotFound();
            }
            var files = HttpContext.Request.Form.Files;
            byte[] image;
            if (files.Count > 0) {
                using (var stream = files[0].OpenReadStream()) {
                    using (var memoryStream = new MemoryStream()) {
                        await stream.CopyToAsync(memoryStream);
                        image = memoryStream.ToArray();
                        coupon.CouponImage = image;
                    }
                }
            }
            _db.Coupons.Add(coupon);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int? id) {
            if (id == null) {
                return BadRequest();
            }

            var couponFromDb = await _db.Coupons.FirstOrDefaultAsync(c => c.CouponId == id);
            if (couponFromDb == null) {
                return NotFound();
            }
            return View(couponFromDb);
        }
        
        [HttpGet]
        public async Task<IActionResult> Edit(int? id) {
            if (id == null) {
                return BadRequest();
            }

            var couponFromDb = await _db.Coupons.FirstOrDefaultAsync(c => c.CouponId == id);
            if (couponFromDb == null) {
                return NotFound();
            }
            return View(couponFromDb);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Coupon coupon) {
            if (!ModelState.IsValid) {
                return BadRequest();
            }

            var couponFromDb = await _db.Coupons.FirstOrDefaultAsync(
                c => c.CouponId == coupon.CouponId);
            if (couponFromDb == null)
                return NotFound();
            couponFromDb.CouponName = coupon.CouponName;
            couponFromDb.Discount = coupon.Discount;
            couponFromDb.CouponType = coupon.CouponType;
            couponFromDb.IsActive = coupon.IsActive;
            couponFromDb.MinimumAmount = coupon.MinimumAmount;
            var files = HttpContext.Request.Form.Files;
            if (files.Count > 0) {
                using (var imageStream = files[0].OpenReadStream()) {
                    using (var memoryStream = new MemoryStream()) {
                        await imageStream.CopyToAsync(memoryStream);
                        couponFromDb.CouponImage = memoryStream.ToArray();
                    }
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id) {
            if (id == null) {
                return BadRequest();
            }

            var couponFromDb = await _db.Coupons.FirstOrDefaultAsync(c => c.CouponId == id);
            if (couponFromDb == null) {
                return NotFound();
            }
            return View(couponFromDb);
        }
        
        [HttpPost]
        public async Task<IActionResult> Delete(int id) {
            var couponFromDb = await _db.Coupons.FirstOrDefaultAsync(c => c.CouponId == id);
            if (couponFromDb == null) {
                return NotFound();
            }

            _db.Coupons.Remove(couponFromDb);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}