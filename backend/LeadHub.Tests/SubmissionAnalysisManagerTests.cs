using Microsoft.Extensions.Logging.Abstractions;
using LeadHub.Application;
using LeadHub.Ports.Output;
using LeadHub.Ports.Input;
using LeadHub.Tests.Fakes;

namespace LeadHub.Tests;

public class SubmissionAnalysisManagerTests
{
    private const string FormId = "form-1";
    private const string SubmissionId = "sub-1";

    private readonly InMemoryFormRepository formRepository;
    private readonly InMemorySubmissionRepository submissionRepository;
    private readonly FakeSpamAnalyzer spamAnalyzer;
    private readonly SpyEmailNotificationSender emailSender;
    private readonly SubmissionAnalysisManager manager;

    public SubmissionAnalysisManagerTests()
    {
        formRepository = new InMemoryFormRepository();
        submissionRepository = new InMemorySubmissionRepository();
        spamAnalyzer = new FakeSpamAnalyzer();
        emailSender = new SpyEmailNotificationSender();

        manager = new SubmissionAnalysisManager(
            NullLogger<SubmissionAnalysisManager>.Instance, submissionRepository, formRepository, spamAnalyzer, emailSender);
    }

    private Task SeedForm(bool notificationsEnabled = false, string? notificationEmail = null) =>
        formRepository.Save(new Form(FormId, "Contact", "contact", null, notificationsEnabled, notificationEmail, null, DateTimeOffset.UtcNow, null));

    private Task SeedSubmission(IReadOnlyDictionary<string, object?>? payload = null) =>
        submissionRepository.Save(new Submission(
            SubmissionId, FormId, DateTimeOffset.UtcNow, "1.2.3.4", "agent",
            payload ?? new Dictionary<string, object?> { ["name"] = "Pedro" }, SubmissionStatus.PendingReview, 0, Array.Empty<string>()));

    [Fact]
    public async Task AnalyzeSubmission_ShouldFail_WhenSubmissionDoesNotExist()
    {
        var result = await manager.AnalyzeSubmission(new AnalyzeSubmissionInput("missing"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.SubmissionNotFound, result.Error.Code);
    }

    [Fact]
    public async Task AnalyzeSubmission_ShouldPersistVerdict()
    {
        await SeedSubmission();
        spamAnalyzer.Verdict = new SpamVerdict(25, SubmissionStatus.Spam, ["message_contains_url", "message_too_long"]);

        var result = await manager.AnalyzeSubmission(new AnalyzeSubmissionInput(SubmissionId));

        Assert.True(result.IsSuccess);
        var stored = await submissionRepository.GetById(SubmissionId);
        Assert.NotNull(stored);
        Assert.Equal(SubmissionStatus.Spam, stored.Status);
        Assert.Equal(25, stored.SpamScore);
        Assert.Equal(["message_contains_url", "message_too_long"], stored.SpamReasons);
    }

    [Fact]
    public async Task AnalyzeSubmission_ShouldSendNotification_WhenHam_AndNotificationsEnabled()
    {
        await SeedForm(notificationsEnabled: true, notificationEmail: "owner@example.com");
        await SeedSubmission();
        spamAnalyzer.Verdict = new SpamVerdict(0, SubmissionStatus.Ham, Array.Empty<string>());

        var result = await manager.AnalyzeSubmission(new AnalyzeSubmissionInput(SubmissionId));

        Assert.True(result.IsSuccess);
        Assert.Single(emailSender.SentNotifications);
        Assert.Equal("owner@example.com", emailSender.SentNotifications[0].To);
    }

    [Theory]
    [InlineData(SubmissionStatus.SuspectedSpam)]
    [InlineData(SubmissionStatus.Spam)]
    public async Task AnalyzeSubmission_ShouldNotSendNotification_WhenNotHam(SubmissionStatus status)
    {
        await SeedForm(notificationsEnabled: true, notificationEmail: "owner@example.com");
        await SeedSubmission();
        spamAnalyzer.Verdict = new SpamVerdict(15, status, ["suspicious_keyword"]);

        var result = await manager.AnalyzeSubmission(new AnalyzeSubmissionInput(SubmissionId));

        Assert.True(result.IsSuccess);
        Assert.Empty(emailSender.SentNotifications);
    }

    [Fact]
    public async Task AnalyzeSubmission_ShouldNotSendNotification_WhenHam_ButNotificationsDisabled()
    {
        await SeedForm(notificationsEnabled: false);
        await SeedSubmission();
        spamAnalyzer.Verdict = new SpamVerdict(0, SubmissionStatus.Ham, Array.Empty<string>());

        await manager.AnalyzeSubmission(new AnalyzeSubmissionInput(SubmissionId));

        Assert.Empty(emailSender.SentNotifications);
    }

    [Fact]
    public async Task AnalyzeSubmission_ShouldStripReservedFields_FromNotificationPayload()
    {
        await SeedForm(notificationsEnabled: true, notificationEmail: "owner@example.com");
        await SeedSubmission(new Dictionary<string, object?> { ["name"] = "Pedro", ["_honeypot"] = "" });
        spamAnalyzer.Verdict = new SpamVerdict(0, SubmissionStatus.Ham, Array.Empty<string>());

        await manager.AnalyzeSubmission(new AnalyzeSubmissionInput(SubmissionId));

        Assert.False(emailSender.SentNotifications[0].Payload.ContainsKey("_honeypot"));
    }

    [Fact]
    public async Task AnalyzeSubmission_ShouldFail_WhenNotificationFails_ButKeepPersistedVerdict()
    {
        await SeedForm(notificationsEnabled: true, notificationEmail: "owner@example.com");
        await SeedSubmission();
        spamAnalyzer.Verdict = new SpamVerdict(0, SubmissionStatus.Ham, Array.Empty<string>());
        emailSender.ShouldFail = true;

        var result = await manager.AnalyzeSubmission(new AnalyzeSubmissionInput(SubmissionId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotificationFailed, result.Error.Code);

        // Verdict must survive even though the use case reports failure (so a job retry doesn't
        // rescore) — notification is a side effect, not the source of truth.
        var stored = await submissionRepository.GetById(SubmissionId);
        Assert.NotNull(stored);
        Assert.Equal(SubmissionStatus.Ham, stored.Status);
    }
}
