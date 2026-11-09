using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public class CreateConsultationValidator : AbstractValidator<CreateConsultationDTO>
    {
        public CreateConsultationValidator()
        {
            RuleFor(x => x.Diagnosis)
                .NotEmpty().WithMessage("Diagnosis is required.")
                .MaximumLength(1000).WithMessage("Diagnosis cannot exceed 1000 characters.");

            RuleFor(x => x.Prescription!)
                .MaximumLength(1000).WithMessage("Prescription cannot exceed 1000 characters.");
        }
    }

    public class UpdateConsultationValidator : AbstractValidator<UpdateConsultationDTO>
    {
        public UpdateConsultationValidator()
        {
            RuleFor(x => x.Diagnosis)
                .NotEmpty().WithMessage("Diagnosis is required.")
                .MaximumLength(1000).WithMessage("Diagnosis cannot exceed 1000 characters.");

            RuleFor(x => x.Prescription!)
                .MaximumLength(1000).WithMessage("Prescription cannot exceed 1000 characters.");
        }
    }
}