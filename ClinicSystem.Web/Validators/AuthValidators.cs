using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public class LoginValidator : AbstractValidator<LoginDTO>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }

    public class RegisterValidator : AbstractValidator<RegisterDTO>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.FirstName).PersonName("First name");
            RuleFor(x => x.LastName).PersonName("Last name");
            RuleFor(x => x.PersonalNumber).PersonalNumber();
            RuleFor(x => x.Phone!).MaximumLength(20).WithMessage("Phone cannot exceed 20 characters.");
            RuleFor(x => x.BirthDate).ValidBirthDate();
            RuleFor(x => x.Email).EmailField();
            RuleFor(x => x.Password).PasswordField();
        }
    }

    public class ChangePasswordValidator : AbstractValidator<ChangePasswordDTO>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required.");

            RuleFor(x => x.NewPassword).PasswordField();

            RuleFor(x => x.NewPassword)
                .NotEqual(x => x.CurrentPassword)
                .WithMessage("New password must be different from the current password.");
        }
    }
}