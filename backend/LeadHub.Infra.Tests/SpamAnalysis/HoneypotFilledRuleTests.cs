using LeadHub.Infra.SpamAnalysis;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class HoneypotFilledRuleTests
{
    private readonly HoneypotFilledRule rule = new();

    private static Submission MakeSubmission(IReadOnlyDictionary<string, object?> payload) =>
        new("sub-1", "form-1", DateTimeOffset.UtcNow, "1.2.3.4", "agent", payload, SubmissionStatus.PendingReview, 0, Array.Empty<string>());

    [Fact]
    public void Evaluate_ShouldReturnNull_WhenHoneypotIsAbsent()
    {
        var hit = rule.Evaluate(MakeSubmission(new Dictionary<string, object?> { ["name"] = "Pedro" }));

        Assert.Null(hit);
    }

    [Fact]
    public void Evaluate_ShouldReturnNull_WhenHoneypotIsEmpty()
    {
        var hit = rule.Evaluate(MakeSubmission(new Dictionary<string, object?> { ["name"] = "Pedro", ["_honeypot"] = "" }));

        Assert.Null(hit);
    }

    [Fact]
    public void Evaluate_ShouldReturnHit_WhenHoneypotHasValue()
    {
        var hit = rule.Evaluate(MakeSubmission(new Dictionary<string, object?> { ["name"] = "Pedro", ["_honeypot"] = "bot" }));

        Assert.NotNull(hit);
        Assert.Equal(100, hit.Score);
        Assert.Equal("honeypot_filled", hit.Reason);
    }
}
