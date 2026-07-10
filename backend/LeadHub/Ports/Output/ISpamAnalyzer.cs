namespace LeadHub.Ports.Output;

/// <summary>
/// Scores a submission for spam signals. Must be deterministic: the same submission always
/// produces the same verdict, so analysis can be safely retried after a transient failure.
/// </summary>
public interface ISpamAnalyzer
{
    SpamVerdict Analyze(Submission submission);
}

public sealed record SpamVerdict(int Score, SubmissionStatus Status, IReadOnlyList<string> Reasons);
