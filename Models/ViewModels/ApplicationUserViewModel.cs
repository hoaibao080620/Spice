using System.Collections.Generic;

namespace Spice.Models.ViewModels {
    public class ApplicationUserViewModel {
        public ApplicationUser ApplicationUser { get; set; }
        public List<string> Roles { get; set; }
    }
}