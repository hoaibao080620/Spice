using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models.ViewModels {
    public class SubCategoryAndCategoryViewModel {
        public List<Category> Categories { get; set; }
        public SubCategory SubCategory { get; set; }
        public List<string> SubCategoriesExist { get; set; }
        public string StatusMessage { get; set; }
    }
}
