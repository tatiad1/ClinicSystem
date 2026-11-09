using ClinicSystem.Application.Interfaces;
using ClinicSystem.Application.Settings;
using Microsoft.Extensions.Options;

namespace ClinicSystem.Web.BackgroundServices
{
    public class AppointmentReminderWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ReminderSettings _settings;
        private readonly ILogger<AppointmentReminderWorker> _logger;

        public AppointmentReminderWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<ReminderSettings> options,
            ILogger<AppointmentReminderWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.CheckIntervalMinutes));
            using var timer = new PeriodicTimer(interval);

            try
            {
                do
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var reminderService = scope.ServiceProvider.GetRequiredService<IAppointmentReminderService>();

                        var count = await reminderService.SendDueRemindersAsync(stoppingToken);
                        if (count > 0)
                            _logger.LogInformation("Sent {Count} appointment reminder(s)", count);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // ერთი ჩავარდნა არ უნდა აჩერებდეს მთელ სერვისს
                        _logger.LogError(ex, "Reminder check failed");
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // აპლიკაცია ჩერდება
            }
        }
    }
}