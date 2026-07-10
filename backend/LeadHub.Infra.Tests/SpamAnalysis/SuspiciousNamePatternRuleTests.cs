using LeadHub.Infra.SpamAnalysis;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class SuspiciousNamePatternRuleTests
{
    private readonly SuspiciousNamePatternRule rule = new();

    private static Submission MakeSubmission(string name) =>
        new("sub-1", "form-1", DateTimeOffset.UtcNow, "1.2.3.4", "agent",
            new Dictionary<string, object?> { ["name"] = name }, SubmissionStatus.PendingReview, 0, Array.Empty<string>());

    [Theory]
    [InlineData("Pedro")]
    [InlineData("Pedro Pardal")]
    [InlineData("Anne-Marie")]
    public void Evaluate_ShouldReturnNull_ForOrdinaryNames(string name)
    {
        Assert.Null(rule.Evaluate(MakeSubmission(name)));
    }

    [Theory]
    [InlineData("RobertSkect")]
    [InlineData("Danielepife")]
    [InlineData("StephenSkectGM")]
    public void Evaluate_ShouldReturnHit_ForBotLikeNames(string name)
    {
        var hit = rule.Evaluate(MakeSubmission(name));

        Assert.NotNull(hit);
        Assert.Equal(5, hit.Score);
        Assert.Equal("suspicious_name_pattern", hit.Reason);
    }
}
