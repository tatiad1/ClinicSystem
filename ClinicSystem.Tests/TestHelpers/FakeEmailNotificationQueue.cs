using ClinicSystem.Application.Interfaces;

namespace ClinicSystem.Tests.TestHelpers
{
    public sealed class FakeEmailNotificationQueue : IEmailNotificationQueue
    {
        public ValueTask EnqueueAsync(
            string to,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
