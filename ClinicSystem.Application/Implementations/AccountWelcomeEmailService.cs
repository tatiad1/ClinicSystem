using ClinicSystem.Application.Interfaces;
using ClinicSystem.Application.Settings;
using ClinicSystem.Domain.Models.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicSystem.Application.Implementations
{
    public sealed class AccountWelcomeEmailService : IAccountWelcomeEmailService
    {
        private readonly IEmailNotificationQueue _emailQueue;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<AccountWelcomeEmailService> _logger;

        public AccountWelcomeEmailService(
            IEmailNotificationQueue emailQueue,
            IOptions<EmailSettings> emailOptions,
            ILogger<AccountWelcomeEmailService> logger)
        {
            _emailQueue = emailQueue;
            _emailSettings = emailOptions.Value;
            _logger = logger;
        }

        public async Task SendWelcomeEmailAsync(string email, string firstName, string role)
        {
            if (!_emailSettings.Enabled || string.IsNullOrWhiteSpace(email))
                return;

            var (accountType, nextStep) = role switch
            {
                Roles.Patient => (
                    "patient account",
                    "You can now sign in to book appointments and view your visit information."),
                Roles.Doctor => (
                    "doctor account",
                    "You can now sign in to manage your schedule and patient visits."),
                Roles.Receptionist => (
                    "receptionist account",
                    "You can now sign in to help manage patient records and appointments."),
                Roles.Admin => (
                    "administrator account",
                    "You can now sign in to manage clinic staff and operations."),
                _ => (
                    "Clinic Portal account",
                    "You can now sign in to Clinic Portal using your account details.")
            };

            var subject = $"Welcome to Clinic Portal, {firstName}!";
            var body = $"Hello {firstName},\n\n" +
                       $"Welcome to Clinic Portal. Your {accountType} has been created successfully.\n\n" +
                       $"{nextStep}\n\n" +
                       $"Sign-in email: {email.Trim()}\n\n" +
                       "For your security, your password is not included in this email. " +
                       "If you did not expect this account, please contact the clinic.\n\n" +
                       "Clinic Portal";

            try
            {
                await _emailQueue.EnqueueAsync(
                    email.Trim(), subject, body, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // The account is already saved, so a mail queue failure must not undo creation.
                _logger.LogError(ex, "Failed to queue welcome email for {Email}", email);
            }
        }
    }
}
