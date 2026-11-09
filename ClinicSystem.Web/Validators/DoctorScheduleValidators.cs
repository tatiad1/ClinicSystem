using ClinicSystem.Domain.Models.DTOs;
using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public class CreateDoctorScheduleValidator : AbstractValidator<CreateDoctorScheduleDTO>
    {
        public CreateDoctorScheduleValidator()
        {
            RuleFor(x => x.DayOfWeek).IsInEnum().WithMessage("Invalid day of week.");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime).WithMessage("End time must be later than start time.");
        }
    }

    public class UpdateDoctorScheduleValidator : AbstractValidator<UpdateDoctorScheduleDTO>
    {
        public UpdateDoctorScheduleValidator()
        {
            RuleFor(x => x.DayOfWeek).IsInEnum().WithMessage("Invalid day of week.");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime).WithMessage("End time must be later than start time.");
        }
    }
}