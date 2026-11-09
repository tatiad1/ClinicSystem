using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public class CreateReferralValidator : AbstractValidator<CreateReferralDTO>
    {
        public CreateReferralValidator()
        {
            RuleFor(x => x.ToSpecialtyId).GreaterThan(0).WithMessage("Specialty is required.");

            RuleFor(x => x.Reason!).MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");

            RuleFor(x => x.ValidForDays)
                .InclusiveBetween(1, 365).WithMessage("Valid days must be between 1 and 365.")
                .When(x => x.ValidForDays.HasValue);
        }
    }
}