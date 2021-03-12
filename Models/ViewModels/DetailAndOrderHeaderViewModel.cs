using System.Collections.Generic;

namespace Spice.Models.ViewModels {
    public class DetailAndOrderHeaderViewModel {
        public OrderHeader OrderHeader { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
    }
}