using LeadHub.Ports.Output;

namespace LeadHub.Tests.Fakes;

public class FakeSubmissionAnalysisQueue : ISubmissionAnalysisQueue
{
    public List<string> EnqueuedSubmissionIds { get; } = new();

    public ValueTask Enqueue(string submissionId, CancellationToken cancellationToken = default)
    {
        EnqueuedSubmissionIds.Add(submissionId);
        return ValueTask.CompletedTask;
    }
}
