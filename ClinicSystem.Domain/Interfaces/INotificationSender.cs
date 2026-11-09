namespace ClinicSystem.Domain.Interfaces
{
    public interface INotificationSender
    {
        Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    }
}