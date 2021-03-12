using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models {
    public class Coupon {
        
        [Key]
        [Required]
        public int CouponId { get; set; }
        [Required]
        public string CouponName { get; set; }
        [Required]
        public string CouponType { get; set; }
        public enum ECouponType {Dollar = 0, Percent = 1 }
        [Required]
        public double Discount { get; set; }
        [Required]
        public double MinimumAmount { get; set; }
        public byte[] CouponImage { get; set; }
        [Required]
        public bool IsActive { get; set; }

    }
}
