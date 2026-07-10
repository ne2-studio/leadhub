using LeadHub.Infra.SpamAnalysis;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class MessageTooLongRuleTests
{
    private readonly MessageTooLongRule rule = new();

    private static Submission MakeSubmission(IReadOnlyDictionary<string, object?> payload) =>
        new("sub-1", "form-1", DateTimeOffset.UtcNow, "1.2.3.4", "agent", payload, SubmissionStatus.PendingReview, 0, Array.Empty<string>());

    [Fact]
    public void Evaluate_ShouldReturnNull_WhenMessageIsShort()
    {
        var submission = MakeSubmission(new Dictionary<string, object?> { ["message"] = "Hello there." });

        Assert.Null(rule.Evaluate(submission));
    }

    [Fact]
    public void Evaluate_ShouldReturnHit_WhenMessageExceeds500Characters()
    {
        var submission = MakeSubmission(new Dictionary<string, object?> { ["message"] = new string('a', 501) });

        var hit = rule.Evaluate(submission);

        Assert.NotNull(hit);
        Assert.Equal(5, hit.Score);
        Assert.Equal("message_too_long", hit.Reason);
    }

    [Fact]
    public void Evaluate_ShouldFallBackToAnyField_WhenNoMessageKeyPresent()
    {
        var submission = MakeSubmission(new Dictionary<string, object?> { ["comments"] = new string('b', 501) });

        var hit = rule.Evaluate(submission);

        Assert.NotNull(hit);
        Assert.Equal("message_too_long", hit.Reason);
    }
}
