using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public class CreatePatientValidator : AbstractValidator<CreatePatientDTO>
    {
        public CreatePatientValidator()
        {
            RuleFor(x => x.FirstName).PersonName("First name");
            RuleFor(x => x.LastName).PersonName("Last name");
            RuleFor(x => x.PersonalNumber).PersonalNumber();
            RuleFor(x => x.Phone!).MaximumLength(20).WithMessage("Phone cannot exceed 20 characters.");
            RuleFor(x => x.BirthDate).ValidBirthDate();
            When(x => !string.IsNullOrWhiteSpace(x.ContactEmail), () =>
                RuleFor(x => x.ContactEmail!).EmailField());

            RuleFor(x => x.FamilyDoctorId)
                .GreaterThan(0).WithMessage("Family doctor id is not valid.")
                .When(x => x.FamilyDoctorId.HasValue);

            // თუ ელფოსტა ან პაროლი მითითებულია, ორივე უნდა იყოს სწორი
            When(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Password), () =>
            {
                RuleFor(x => x.Email!).EmailField();
                RuleFor(x => x.Password!).PasswordField();
            });
        }
    }

    public class UpdatePatientValidator : AbstractValidator<UpdatePatientDTO>
    {
        public UpdatePatientValidator()
        {
            RuleFor(x => x.FirstName).PersonName("First name");
            RuleFor(x => x.LastName).PersonName("Last name");
            RuleFor(x => x.Phone!).MaximumLength(20).WithMessage("Phone cannot exceed 20 characters.");
            When(x => !string.IsNullOrWhiteSpace(x.ContactEmail), () =>
                RuleFor(x => x.ContactEmail!).EmailField());
            RuleFor(x => x.BirthDate).ValidBirthDate();
        }
    }
}
