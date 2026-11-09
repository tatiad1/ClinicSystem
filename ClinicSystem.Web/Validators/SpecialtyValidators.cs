using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public class CreateSpecialtyValidator : AbstractValidator<CreateSpecialtyDTO>
    {
        public CreateSpecialtyValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.RequiresReferral)
                .Equal(false)
                .When(x => x.IsFamilyMedicine)
                .WithMessage("Family medicine specialties cannot require a referral.");
        }
    }

    public class UpdateSpecialtyValidator : AbstractValidator<UpdateSpecialtyDTO>
    {
        public UpdateSpecialtyValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.RequiresReferral)
                .Equal(false)
                .When(x => x.IsFamilyMedicine)
                .WithMessage("Family medicine specialties cannot require a referral.");
        }
    }
}
