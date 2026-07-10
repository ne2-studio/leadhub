using LeadHub.Infra.SpamAnalysis;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class SuspiciousKeywordRuleTests
{
    private readonly SuspiciousKeywordRule rule = new();

    private static Submission MakeSubmission(string message) =>
        new("sub-1", "form-1", DateTimeOffset.UtcNow, "1.2.3.4", "agent",
            new Dictionary<string, object?> { ["message"] = message }, SubmissionStatus.PendingReview, 0, Array.Empty<string>());

    [Fact]
    public void Evaluate_ShouldReturnNull_WhenNoKeywordPresent()
    {
        Assert.Null(rule.Evaluate(MakeSubmission("Hi, I'd like to book a consultation.")));
    }

    [Theory]
    [InlineData("We offer SEO services")]
    [InlineData("Buy backlinks cheap")]
    [InlineData("Interested in a guest post?")]
    [InlineData("Win big at our casino")]
    [InlineData("Huge jackpot waiting for you")]
    [InlineData("Claim your bonus now")]
    [InlineData("Invest in crypto today")]
    [InlineData("Boost your marketing campaign")]
    public void Evaluate_ShouldReturnHit_WhenKeywordPresent(string message)
    {
        var hit = rule.Evaluate(MakeSubmission(message));

        Assert.NotNull(hit);
        Assert.Equal(10, hit.Score);
        Assert.Equal("suspicious_keyword", hit.Reason);
    }
}
