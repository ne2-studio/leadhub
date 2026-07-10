using System.Threading.Channels;
using LeadHub.Ports.Output;

namespace LeadHub.Infra;

/// <summary>
/// In-process FIFO queue backing <see cref="ISubmissionAnalysisQueue"/>. Registered as a
/// Singleton (the channel must survive across requests) and consumed exclusively by
/// <see cref="SubmissionAnalysisBackgroundService"/> via the internal <see cref="Reader"/>.
/// </summary>
public class SubmissionAnalysisQueue : ISubmissionAnalysisQueue
{
    private readonly Channel<string> channel = Channel.CreateUnbounded<string>();

    public ValueTask Enqueue(string submissionId, CancellationToken cancellationToken = default) =>
        channel.Writer.WriteAsync(submissionId, cancellationToken);

    public IAsyncEnumerable<string> Reader(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}
