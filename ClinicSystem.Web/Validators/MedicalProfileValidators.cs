using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    internal static class BloodTypes
    {
        public static readonly string[] Allowed = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
    }

    public class CreateMedicalProfileValidator : AbstractValidator<CreateMedicalProfileDTO>
    {
        public CreateMedicalProfileValidator()
        {
            RuleFor(x => x.BloodType!)
                .Must(b => BloodTypes.Allowed.Contains(b.Trim().ToUpperInvariant()))
                .WithMessage("Blood type must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-.")
                .When(x => !string.IsNullOrWhiteSpace(x.BloodType));

            RuleFor(x => x.Allergies!).MaximumLength(1000).WithMessage("Allergies cannot exceed 1000 characters.");
            RuleFor(x => x.ChronicConditions!).MaximumLength(1000).WithMessage("Chronic conditions cannot exceed 1000 characters.");
        }
    }

    public class UpdateMedicalProfileValidator : AbstractValidator<UpdateMedicalProfileDTO>
    {
        public UpdateMedicalProfileValidator()
        {
            RuleFor(x => x.BloodType!)
                .Must(b => BloodTypes.Allowed.Contains(b.Trim().ToUpperInvariant()))
                .WithMessage("Blood type must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-.")
                .When(x => !string.IsNullOrWhiteSpace(x.BloodType));

            RuleFor(x => x.Allergies!).MaximumLength(1000).WithMessage("Allergies cannot exceed 1000 characters.");
            RuleFor(x => x.ChronicConditions!).MaximumLength(1000).WithMessage("Chronic conditions cannot exceed 1000 characters.");
        }
    }
}