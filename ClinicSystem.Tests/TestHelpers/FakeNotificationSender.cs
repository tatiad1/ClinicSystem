using ClinicSystem.Domain.Interfaces;

namespace ClinicSystem.Tests.TestHelpers
{
    public record SentMessage(string To, string Subject, string Body);

    // ყალბი გამგზავნი: არაფერს აგზავნის, მხოლოდ იმახსოვრებს, რა უნდა გაეგზავნა
    public class FakeNotificationSender : INotificationSender
    {
        public List<SentMessage> Sent { get; } = new();

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            Sent.Add(new SentMessage(to, subject, body));
            return Task.CompletedTask;
        }
    }
}