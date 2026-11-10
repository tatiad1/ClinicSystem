namespace ClinicSystem.Application.Interfaces
{
    public interface IEmailNotificationQueue
    {
        ValueTask EnqueueAsync(
            string to,
            string subject,
            string body,
            CancellationToken cancellationToken = default);
    }
}
