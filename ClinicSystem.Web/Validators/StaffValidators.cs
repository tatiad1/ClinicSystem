using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public class CreateStaffValidator : AbstractValidator<CreateStaffDTO>
    {
        public CreateStaffValidator()
        {
            RuleFor(x => x.FirstName).PersonName("First name");
            RuleFor(x => x.LastName).PersonName("Last name");
            RuleFor(x => x.PersonalNumber).PersonalNumber();
            RuleFor(x => x.Phone!).MaximumLength(20).WithMessage("Phone cannot exceed 20 characters.");
            RuleFor(x => x.Email).EmailField();
            RuleFor(x => x.Password).PasswordField();
        }
    }
}