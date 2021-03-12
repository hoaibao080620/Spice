using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Spice.Models {
    public class ApplicationUser : IdentityUser {
        [Required]
        public string Name { get; set; }
        public string Address { get; set; }
        
        public string Image { get; set; }
    }
}