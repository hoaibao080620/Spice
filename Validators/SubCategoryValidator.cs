using System.Data;
using FluentValidation;
using Spice.Models;

namespace Spice.Validators {
    public class SubCategoryValidator : AbstractValidator<SubCategory> {
        public SubCategoryValidator() {
            RuleFor(s => s.SubCategoryName).Length(5, 255).NotNull();
        }
    }
}