namespace LeadHub.Ports.Output;

/// <summary>
/// Scores a submission for spam signals. Re-running on the same submission must be safe, so
/// analysis can be retried after a transient failure. Local rules are deterministic; optional
/// external signals (e.g. an LLM classifier) may vary slightly between runs and fail open.
/// </summary>
public interface ISpamAnalyzer
{
    Task<SpamVerdict> Analyze(Submission submission, CancellationToken cancellationToken = default);
}

public sealed record SpamVerdict(int Score, SubmissionStatus Status, IReadOnlyList<string> Reasons);
