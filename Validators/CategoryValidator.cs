using System.Data;
using FluentValidation;
using Spice.Models;

namespace Spice.Validators {
    public class CategoryValidator : AbstractValidator<Category> {
        public CategoryValidator() {
            RuleFor(c => c.Name).NotNull().Length(5, 255);
        }
    }
}