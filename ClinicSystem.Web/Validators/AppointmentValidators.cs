using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentDTO>
    {
        public CreateAppointmentValidator()
        {
            RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage("Doctor is required.");

            RuleFor(x => x.StartTime)
                .Must(t => t != default).WithMessage("Start time is required.");

            RuleFor(x => x.ReferralId)
                .GreaterThan(0).WithMessage("Referral id is not valid.")
                .When(x => x.ReferralId.HasValue);
        }
    }
}