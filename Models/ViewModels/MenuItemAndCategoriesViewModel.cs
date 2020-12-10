using System.Collections.Generic;
namespace Spice.Models.ViewModels {
    public class MenuItemAndCategoriesViewModel {
        public MenuItem Item { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<SubCategory> SubCategories { get; set; }
    }
}
