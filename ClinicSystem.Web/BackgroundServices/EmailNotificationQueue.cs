using System.Threading.Channels;
using ClinicSystem.Application.Interfaces;

namespace ClinicSystem.Web.BackgroundServices
{
    public sealed record QueuedEmail(string To, string Subject, string Body);

    public sealed class EmailNotificationQueue : IEmailNotificationQueue
    {
        private readonly Channel<QueuedEmail> _channel = Channel.CreateUnbounded<QueuedEmail>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        public ValueTask EnqueueAsync(
            string to,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(new QueuedEmail(to, subject, body), cancellationToken);
        }

        public IAsyncEnumerable<QueuedEmail> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
