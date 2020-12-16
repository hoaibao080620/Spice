using System.Collections.Generic;

namespace Spice.Models.ViewModels {
    public class HomePageViewModel {
        public IEnumerable<Coupon> Coupons { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<MenuItem> MenuItems { get; set; }        
    }
}