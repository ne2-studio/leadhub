using LeadHub.Infra.SpamAnalysis;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class MessageContainsUrlRuleTests
{
    private readonly MessageContainsUrlRule rule = new();

    private static Submission MakeSubmission(string message) =>
        new("sub-1", "form-1", DateTimeOffset.UtcNow, "1.2.3.4", "agent",
            new Dictionary<string, object?> { ["message"] = message }, SubmissionStatus.PendingReview, 0, Array.Empty<string>());

    [Fact]
    public void Evaluate_ShouldReturnNull_WhenNoUrlPresent()
    {
        Assert.Null(rule.Evaluate(MakeSubmission("Hello, I'd like a quote please.")));
    }

    [Theory]
    [InlineData("Check out http://spam.example")]
    [InlineData("Visit https://spam.example for more")]
    [InlineData("Go to www.spam.example today")]
    public void Evaluate_ShouldReturnHit_WhenUrlMarkerPresent(string message)
    {
        var hit = rule.Evaluate(MakeSubmission(message));

        Assert.NotNull(hit);
        Assert.Equal(10, hit.Score);
        Assert.Equal("message_contains_url", hit.Reason);
    }
}
