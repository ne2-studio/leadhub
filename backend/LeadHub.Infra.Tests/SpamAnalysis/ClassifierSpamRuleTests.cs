using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using LeadHub.Infra.SpamAnalysis;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class ClassifierSpamRuleTests
{
    private readonly ISpamClassifier classifier = Substitute.For<ISpamClassifier>();

    private ClassifierSpamRule MakeRule() =>
        new(classifier, Options.Create(new SpamClassifierOptions()), NullLogger<ClassifierSpamRule>.Instance);

    private static Submission MakeSubmission(IReadOnlyDictionary<string, object?> payload) =>
        new("sub-1", "form-1", DateTimeOffset.UtcNow, "1.2.3.4", "agent", payload, SubmissionStatus.PendingReview, 0, Array.Empty<string>());

    private static readonly Submission Lead = MakeSubmission(new Dictionary<string, object?>
    {
        ["name"] = "Pedro",
        ["message"] = "Hello, please contact me.",
        ["_honeypot"] = "trap",
        ["age"] = 42
    });

    private void ClassifierReturns(Result<SpamClassification> result) =>
        classifier.Classify(Arg.Any<IReadOnlyList<KeyValuePair<string, string>>>(), Arg.Any<CancellationToken>()).Returns(result);

    [Theory]
    [InlineData(0.95, 20, "classifier_spam")]
    [InlineData(0.85, 20, "classifier_spam")]
    [InlineData(0.6, 10, "classifier_suspected_spam")]
    [InlineData(0.5, 10, "classifier_suspected_spam")]
    public async Task Evaluate_ShouldReturnHit_WhenScoreAboveThreshold(double score, int points, string reason)
    {
        ClassifierReturns(new SpamClassification(true, score, "promo"));

        var hit = await MakeRule().Evaluate(Lead, CancellationToken.None);

        Assert.NotNull(hit);
        Assert.Equal(points, hit.Score);
        Assert.Equal(reason, hit.Reason);
    }

    [Fact]
    public async Task Evaluate_ShouldReturnNull_WhenScoreLow()
    {
        ClassifierReturns(new SpamClassification(false, 0.1, "genuine enquiry"));

        Assert.Null(await MakeRule().Evaluate(Lead, CancellationToken.None));
    }

    [Fact]
    public async Task Evaluate_ShouldMarkUnclassifiedWithZeroPoints_WhenClassifierFails()
    {
        ClassifierReturns(Result.Failure<SpamClassification>("timeout"));

        var hit = await MakeRule().Evaluate(Lead, CancellationToken.None);

        Assert.NotNull(hit);
        Assert.Equal(0, hit.Score);
        Assert.Equal("classifier_unavailable", hit.Reason);
    }

    [Fact]
    public async Task Evaluate_ShouldSendOnlyVisibleTextFields()
    {
        ClassifierReturns(new SpamClassification(false, 0, "ok"));

        await MakeRule().Evaluate(Lead, CancellationToken.None);

        await classifier.Received(1).Classify(
            Arg.Is<IReadOnlyList<KeyValuePair<string, string>>>(fields =>
                fields.Select(f => f.Key).SequenceEqual(new[] { "name", "message" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Evaluate_ShouldSkipClassifier_WhenNoVisibleText()
    {
        var hit = await MakeRule().Evaluate(MakeSubmission(new Dictionary<string, object?> { ["_honeypot"] = "bot" }), CancellationToken.None);

        Assert.Null(hit);
        await classifier.DidNotReceiveWithAnyArgs().Classify(default!, default);
    }
}
