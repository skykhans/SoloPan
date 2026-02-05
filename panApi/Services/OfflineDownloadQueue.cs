using System.Threading.Channels;

namespace PanSystem.Services
{
    public record OfflineDownloadJob(int TaskId);

    public class OfflineDownloadQueue
    {
        private readonly Channel<OfflineDownloadJob> _channel = Channel.CreateUnbounded<OfflineDownloadJob>();

        public ValueTask EnqueueAsync(OfflineDownloadJob job)
        {
            return _channel.Writer.WriteAsync(job);
        }

        public async ValueTask<OfflineDownloadJob> DequeueAsync(CancellationToken cancellationToken)
        {
            var job = await _channel.Reader.ReadAsync(cancellationToken);
            return job;
        }
    }
}
