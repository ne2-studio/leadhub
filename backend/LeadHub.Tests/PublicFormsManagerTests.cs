using Microsoft.Extensions.Logging.Abstractions;
using LeadHub.Application;
using LeadHub.Ports.Output;
using LeadHub.Ports.Input;
using LeadHub.Tests.Fakes;

namespace LeadHub.Tests;

public class PublicFormsManagerTests
{
    private const string GeneratedId = "sub-1";
    private const string FormId = "form-1";
    private const string FormSlug = "contact";

    private readonly InMemoryFormRepository formRepository;
    private readonly InMemorySubmissionRepository submissionRepository;
    private readonly FakeRateLimiter rateLimiter;
    private readonly FakeSubmissionAnalysisQueue analysisQueue;
    private readonly StaticClock clock;
    private readonly PublicFormsManager manager;

    public PublicFormsManagerTests()
    {
        formRepository = new InMemoryFormRepository();
        submissionRepository = new InMemorySubmissionRepository();
        rateLimiter = new FakeRateLimiter();
        analysisQueue = new FakeSubmissionAnalysisQueue();
        clock = new StaticClock();

        manager = new PublicFormsManager(
            NullLogger<PublicFormsManager>.Instance,
            formRepository,
            submissionRepository,
            rateLimiter,
            analysisQueue,
            new StaticIdGenerator(GeneratedId),
            clock);
    }

    private Task SeedForm(bool notificationsEnabled = false, string? notificationEmail = null, string? thankYouUrl = null) =>
        formRepository.Save(new Form(
            FormId, "Contact", FormSlug, null, notificationsEnabled, notificationEmail, thankYouUrl, clock.UtcNow, null));

    [Fact]
    public async Task SubmitForm_ShouldFail_WhenFormDoesNotExist()
    {
        var result = await manager.SubmitForm(new SubmitFormInput(
            "missing", new Dictionary<string, object?> { ["name"] = "Pedro" }, "1.2.3.4", "agent"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.FormNotFound, result.Error.Code);
    }

    [Fact]
    public async Task SubmitForm_ShouldStoreSubmission_AsPendingReview_AndReturnThankYouUrl()
    {
        await SeedForm(thankYouUrl: "https://example.com/thanks");

        var payload = new Dictionary<string, object?> { ["name"] = "Pedro", ["email"] = "pedro@example.com" };
        var result = await manager.SubmitForm(new SubmitFormInput(FormSlug, payload, "1.2.3.4", "agent"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Accepted);
        Assert.Equal("https://example.com/thanks", result.Value.RedirectUrl);

        var stored = await submissionRepository.GetById(GeneratedId);
        Assert.NotNull(stored);
        Assert.Equal("Pedro", stored.Payload["name"]);
        Assert.Equal(SubmissionStatus.PendingReview, stored.Status);
        Assert.Equal(0, stored.SpamScore);
        Assert.Empty(stored.SpamReasons);
    }

    [Fact]
    public async Task SubmitForm_ShouldEnqueueAnalysisJob()
    {
        await SeedForm();

        await manager.SubmitForm(new SubmitFormInput(
            FormSlug, new Dictionary<string, object?> { ["name"] = "Pedro" }, "1.2.3.4", "agent"));

        Assert.Equal([GeneratedId], analysisQueue.EnqueuedSubmissionIds);
    }

    [Fact]
    public async Task SubmitForm_ShouldKeepReservedFields_ForLaterSpamAnalysis()
    {
        await SeedForm();

        var payload = new Dictionary<string, object?> { ["name"] = "Pedro", ["_honeypot"] = "bot-value" };
        await manager.SubmitForm(new SubmitFormInput(FormSlug, payload, "1.2.3.4", "agent"));

        var stored = await submissionRepository.GetById(GeneratedId);
        Assert.NotNull(stored);
        Assert.Equal("bot-value", stored.Payload["_honeypot"]);
    }

    [Fact]
    public async Task SubmitForm_ShouldFail_WhenRateLimited()
    {
        await SeedForm();
        rateLimiter.Allow = false;

        var result = await manager.SubmitForm(new SubmitFormInput(
            FormSlug, new Dictionary<string, object?> { ["name"] = "Pedro" }, "1.2.3.4", "agent"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.RateLimitExceeded, result.Error.Code);
        Assert.Null(await submissionRepository.GetById(GeneratedId));
        Assert.Empty(analysisQueue.EnqueuedSubmissionIds);
    }
}
