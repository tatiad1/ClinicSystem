using FluentValidation;

namespace ClinicSystem.Web.Validators
{
    public static class ValidatorExtensions
    {
        public static IRuleBuilderOptions<T, string> PersonName<T>(this IRuleBuilder<T, string> rule, string field)
        {
            return rule
                .NotEmpty().WithMessage($"{field} is required.")
                .MaximumLength(50).WithMessage($"{field} cannot exceed 50 characters.");
        }

        public static IRuleBuilderOptions<T, string> PersonalNumber<T>(this IRuleBuilder<T, string> rule)
        {
            return rule
                .NotEmpty().WithMessage("Personal number is required.")
                .Matches("^[0-9]{11}$").WithMessage("Personal number must be exactly 11 digits.");
        }

        public static IRuleBuilderOptions<T, string> EmailField<T>(this IRuleBuilder<T, string> rule)
        {
            return rule
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");
        }

        public static IRuleBuilderOptions<T, string> PasswordField<T>(this IRuleBuilder<T, string> rule)
        {
            return rule
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
        }

        public static IRuleBuilderOptions<T, DateOnly> ValidBirthDate<T>(this IRuleBuilder<T, DateOnly> rule)
        {
            return rule
                .Must(d => d != default).WithMessage("Birth date is required.")
                .Must(d => d <= DateOnly.FromDateTime(DateTime.Today)).WithMessage("Birth date cannot be in the future.")
                .Must(d => d >= DateOnly.FromDateTime(DateTime.Today).AddYears(-120)).WithMessage("Birth date is not valid.");
        }
    }
}