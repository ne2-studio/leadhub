using LeadHub.Ports.Output;

namespace LeadHub.Tests.Fakes;

public class FakeSpamAnalyzer : ISpamAnalyzer
{
    public SpamVerdict Verdict { get; set; } = new(0, SubmissionStatus.Ham, Array.Empty<string>());

    public Task<SpamVerdict> Analyze(Submission submission, CancellationToken cancellationToken = default) => Task.FromResult(Verdict);
}
