using AutoMapper;
using Spice.Models;
using Spice.Models.ViewModels;

namespace Spice.Profiles {
    public class UserProfile : Profile  {
        public UserProfile() {
            CreateMap<ApplicationUserViewModel, ApplicationUser>().ReverseMap();
            CreateMap<ApplicationUser, ApplicationUser>().
                ForAllMembers(
                opt
                    => opt.Condition((src, dest, sourceMember) => sourceMember != null));;
        }
    }
}