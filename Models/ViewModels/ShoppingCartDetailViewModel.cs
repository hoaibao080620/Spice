using System.Collections.Generic;

namespace Spice.Models {
    public class ShoppingCartDetailViewModel {
        public List<ShoppingCart> Carts { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}