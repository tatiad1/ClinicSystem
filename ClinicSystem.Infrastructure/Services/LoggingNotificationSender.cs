using ClinicSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClinicSystem.Infrastructure.Services
{
    // რეალურად არ აგზავნის, წერილს ლოგში ბეჭდავს.
    // რეალურ პროექტში ამ კლასს ჩაანაცვლებდა SMTP ან სხვა მომსახურების რეალიზაცია.
    public class LoggingNotificationSender : INotificationSender
    {
        private readonly ILogger<LoggingNotificationSender> _logger;

        public LoggingNotificationSender(ILogger<LoggingNotificationSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("EMAIL to {To} | {Subject} | {Body}", to, subject, body);
            return Task.CompletedTask;
        }
    }
}