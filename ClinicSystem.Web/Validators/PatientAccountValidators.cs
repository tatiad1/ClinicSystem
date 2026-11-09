using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public class CreatePatientAccountValidator : AbstractValidator<CreatePatientAccountDTO>
    {
        public CreatePatientAccountValidator()
        {
            RuleFor(x => x.Email).EmailField();
            RuleFor(x => x.Password).PasswordField();
        }
    }
}