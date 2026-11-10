using ClinicSystem.Domain.Interfaces;

namespace ClinicSystem.Web.BackgroundServices
{
    public sealed class EmailNotificationWorker : BackgroundService
    {
        private readonly EmailNotificationQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailNotificationWorker> _logger;

        public EmailNotificationWorker(
            EmailNotificationQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<EmailNotificationWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var email in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
                    await sender.SendAsync(email.To, email.Subject, email.Body, stoppingToken);
                    _logger.LogInformation("Queued email sent to {Recipient}", email.To);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to send queued email to {Recipient}", email.To);
                }
            }
        }
    }
}
