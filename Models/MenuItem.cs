using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models {
    public class MenuItem {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(255,MinimumLength = 1, ErrorMessage ="Name must equal or greater than 1 character")]
        public string ItemName { get; set; }
        [Required]
        public string description { get; set; }
        public string Spicyness { get; set; }
        public enum ESpice { NA = 0 , NotSpicy = 1, Spicy = 2, VerySpicy = 3 }
        public string Image { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        
        public int SubCategoryId { get; set; }
        [Required]
        [Range(1,Int64.MaxValue, ErrorMessage = "Price must greater than or equal 1")]
        public double Price { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
        [ForeignKey("SubCategoryId")]
        public SubCategory SubCategory { get; set; }
    }
}
