using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public class CreateDoctorValidator : AbstractValidator<CreateDoctorDTO>
    {
        public CreateDoctorValidator()
        {
            RuleFor(x => x.FirstName).PersonName("First name");
            RuleFor(x => x.LastName).PersonName("Last name");
            RuleFor(x => x.PersonalNumber).PersonalNumber();
            RuleFor(x => x.Phone!).MaximumLength(20).WithMessage("Phone cannot exceed 20 characters.");
            RuleFor(x => x.Email).EmailField();
            RuleFor(x => x.Password).PasswordField();
            RuleFor(x => x.SpecialtyId).GreaterThan(0).WithMessage("Specialty is required.");
        }
    }

    public class UpdateDoctorValidator : AbstractValidator<UpdateDoctorDTO>
    {
        public UpdateDoctorValidator()
        {
            RuleFor(x => x.FirstName).PersonName("First name");
            RuleFor(x => x.LastName).PersonName("Last name");
            RuleFor(x => x.Phone!).MaximumLength(20).WithMessage("Phone cannot exceed 20 characters.");
            RuleFor(x => x.SpecialtyId).GreaterThan(0).WithMessage("Specialty is required.");
        }
    }
}