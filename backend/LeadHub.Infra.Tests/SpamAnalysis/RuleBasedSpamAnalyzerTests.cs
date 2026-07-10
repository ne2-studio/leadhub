using Microsoft.Extensions.Options;
using LeadHub.Infra.SpamAnalysis;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class RuleBasedSpamAnalyzerTests
{
    private static RuleBasedSpamAnalyzer MakeAnalyzer(SpamScoringOptions? options = null) =>
        new(
            [new HoneypotFilledRule(), new MessageContainsUrlRule(), new SuspiciousKeywordRule(), new SuspiciousNamePatternRule(), new MessageTooLongRule()],
            Options.Create(options ?? new SpamScoringOptions()));

    private static Submission MakeSubmission(IReadOnlyDictionary<string, object?> payload) =>
        new("sub-1", "form-1", DateTimeOffset.UtcNow, "1.2.3.4", "agent", payload, SubmissionStatus.PendingReview, 0, Array.Empty<string>());

    [Fact]
    public void Analyze_ShouldClassifyAsHam_WhenNoRuleMatches()
    {
        var analyzer = MakeAnalyzer();
        var submission = MakeSubmission(new Dictionary<string, object?> { ["name"] = "Pedro", ["message"] = "Hello, please contact me." });

        var verdict = analyzer.Analyze(submission);

        Assert.Equal(0, verdict.Score);
        Assert.Equal(SubmissionStatus.Ham, verdict.Status);
        Assert.Empty(verdict.Reasons);
    }

    [Fact]
    public void Analyze_ShouldClassifyAsSuspectedSpam_BetweenThresholds()
    {
        var analyzer = MakeAnalyzer();
        var submission = MakeSubmission(new Dictionary<string, object?>
        {
            ["name"] = "Pedro",
            ["message"] = "We offer great SEO backlinks."
        });

        var verdict = analyzer.Analyze(submission);

        Assert.Equal(10, verdict.Score);
        Assert.Equal(SubmissionStatus.SuspectedSpam, verdict.Status);
        Assert.Equal(["suspicious_keyword"], verdict.Reasons);
    }

    [Fact]
    public void Analyze_ShouldCombineReasons_AcrossMultipleMatchingRules()
    {
        var analyzer = MakeAnalyzer();
        var submission = MakeSubmission(new Dictionary<string, object?>
        {
            ["name"] = "Pedro",
            ["message"] = "Check http://spam.example - " + new string('a', 501)
        });

        var verdict = analyzer.Analyze(submission);

        Assert.Equal(15, verdict.Score);
        Assert.Equal(SubmissionStatus.SuspectedSpam, verdict.Status);
        Assert.Equal(["message_contains_url", "message_too_long"], verdict.Reasons);
    }

    [Fact]
    public void Analyze_ShouldClassifyAsSpam_WhenHoneypotFilled()
    {
        var analyzer = MakeAnalyzer();
        var submission = MakeSubmission(new Dictionary<string, object?> { ["name"] = "Pedro", ["_honeypot"] = "bot" });

        var verdict = analyzer.Analyze(submission);

        Assert.Equal(100, verdict.Score);
        Assert.Equal(SubmissionStatus.Spam, verdict.Status);
        Assert.Equal(["honeypot_filled"], verdict.Reasons);
    }

    [Fact]
    public void Analyze_ShouldRespectConfiguredThresholds()
    {
        var analyzer = MakeAnalyzer(new SpamScoringOptions { SuspectedSpamThreshold = 5, SpamThreshold = 10 });
        var submission = MakeSubmission(new Dictionary<string, object?> { ["name"] = "Pedro", ["message"] = "SEO backlinks" });

        var verdict = analyzer.Analyze(submission);

        Assert.Equal(10, verdict.Score);
        Assert.Equal(SubmissionStatus.Spam, verdict.Status);
    }
}
