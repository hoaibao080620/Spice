using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spice.Models {
    public class ShoppingCart {
        
        [Key]
        public int CartId { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        [NotMapped]
        public ApplicationUser User { get; set; }
        
        public int MenuItemId { get; set; }
        [ForeignKey("MenuItemId")]
        [NotMapped]
        public MenuItem MenuItem { get; set; }
        [Range(1,Int32.MaxValue,ErrorMessage = "Count must equal or greater than 1")]
        public int Count { get; set; }

        public DateTime DateCreated { get; set; }

        public ShoppingCart() {
            Count = 0;
        }
    }
}