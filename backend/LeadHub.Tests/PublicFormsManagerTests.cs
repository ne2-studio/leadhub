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
    private readonly FakeSpamProtection spamProtection;
    private readonly FakeRateLimiter rateLimiter;
    private readonly SpyEmailNotificationSender emailSender;
    private readonly StaticClock clock;
    private readonly PublicFormsManager manager;

    public PublicFormsManagerTests()
    {
        formRepository = new InMemoryFormRepository();
        submissionRepository = new InMemorySubmissionRepository();
        spamProtection = new FakeSpamProtection();
        rateLimiter = new FakeRateLimiter();
        emailSender = new SpyEmailNotificationSender();
        clock = new StaticClock();

        manager = new PublicFormsManager(
            NullLogger<PublicFormsManager>.Instance,
            formRepository,
            submissionRepository,
            spamProtection,
            rateLimiter,
            emailSender,
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
    public async Task SubmitForm_ShouldStoreSubmission_AndReturnThankYouUrl()
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
    }

    [Fact]
    public async Task SubmitForm_ShouldStripReservedFields_BeforeStoring()
    {
        await SeedForm();

        var payload = new Dictionary<string, object?> { ["name"] = "Pedro", ["_internal"] = "ignored" };
        await manager.SubmitForm(new SubmitFormInput(FormSlug, payload, "1.2.3.4", "agent"));

        var stored = await submissionRepository.GetById(GeneratedId);
        Assert.NotNull(stored);
        Assert.False(stored.Payload.ContainsKey("_internal"));
        Assert.True(stored.Payload.ContainsKey("name"));
    }

    [Fact]
    public async Task SubmitForm_ShouldFail_WhenSpamDetected_AndNotStoreSubmission()
    {
        await SeedForm();
        spamProtection.IsSpam = true;

        var result = await manager.SubmitForm(new SubmitFormInput(
            FormSlug, new Dictionary<string, object?> { ["_honeypot"] = "bot" }, "1.2.3.4", "agent"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.SpamDetected, result.Error.Code);
        Assert.Null(await submissionRepository.GetById(GeneratedId));
        Assert.Empty(emailSender.SentNotifications);
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
    }

    [Fact]
    public async Task SubmitForm_ShouldSendNotification_WhenEnabled()
    {
        await SeedForm(notificationsEnabled: true, notificationEmail: "owner@example.com");

        await manager.SubmitForm(new SubmitFormInput(
            FormSlug, new Dictionary<string, object?> { ["name"] = "Pedro" }, "1.2.3.4", "agent"));

        Assert.Single(emailSender.SentNotifications);
        Assert.Equal("owner@example.com", emailSender.SentNotifications[0].To);
    }

    [Fact]
    public async Task SubmitForm_ShouldNotSendNotification_WhenDisabled()
    {
        await SeedForm(notificationsEnabled: false);

        await manager.SubmitForm(new SubmitFormInput(
            FormSlug, new Dictionary<string, object?> { ["name"] = "Pedro" }, "1.2.3.4", "agent"));

        Assert.Empty(emailSender.SentNotifications);
    }

    [Fact]
    public async Task SubmitForm_ShouldKeepStoredSubmission_WhenNotificationFails()
    {
        await SeedForm(notificationsEnabled: true, notificationEmail: "owner@example.com");
        emailSender.ShouldFail = true;

        var result = await manager.SubmitForm(new SubmitFormInput(
            FormSlug, new Dictionary<string, object?> { ["name"] = "Pedro" }, "1.2.3.4", "agent"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotificationFailed, result.Error.Code);

        // The submission must remain persisted even though the use case reports failure —
        // notification is a side effect, not the source of truth.
        var stored = await submissionRepository.GetById(GeneratedId);
        Assert.NotNull(stored);
    }
}
