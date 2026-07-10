namespace LeadHub.Ports.Output;

/// <summary>
/// Hands a freshly stored submission off for asynchronous spam analysis. Enqueueing must not
/// block or fail the submit request — analysis happens out-of-band, after the response is sent.
/// </summary>
public interface ISubmissionAnalysisQueue
{
    ValueTask Enqueue(string submissionId, CancellationToken cancellationToken = default);
}
