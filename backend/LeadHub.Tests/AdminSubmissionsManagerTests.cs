using Microsoft.Extensions.Logging.Abstractions;
using LeadHub.Application;
using LeadHub.Ports.Output;
using LeadHub.Ports.Input;
using LeadHub.Tests.Fakes;

namespace LeadHub.Tests;

public class AdminSubmissionsManagerTests
{
    private const string FormId = "form-1";

    private readonly InMemoryFormRepository formRepository;
    private readonly InMemorySubmissionRepository submissionRepository;
    private readonly AdminSubmissionsManager manager;

    public AdminSubmissionsManagerTests()
    {
        formRepository = new InMemoryFormRepository();
        submissionRepository = new InMemorySubmissionRepository();
        manager = new AdminSubmissionsManager(NullLogger<AdminSubmissionsManager>.Instance, formRepository, submissionRepository);
    }

    private static Submission MakeSubmission(
        string id, DateTimeOffset createdAt, IReadOnlyDictionary<string, object?>? payload = null,
        SubmissionStatus status = SubmissionStatus.Ham, int score = 0, IReadOnlyList<string>? reasons = null) =>
        new(id, FormId, createdAt, "1.2.3.4", "agent", payload ?? new Dictionary<string, object?> { ["name"] = "Person" },
            status, score, reasons ?? Array.Empty<string>());

    [Fact]
    public async Task ListSubmissions_ShouldFail_WhenFormDoesNotExist()
    {
        var result = await manager.ListSubmissions(new ListSubmissionsInput(FormId, 1, 50, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.FormNotFound, result.Error.Code);
    }

    [Fact]
    public async Task ListSubmissions_ShouldReturnPaginatedResults()
    {
        await formRepository.Save(new Form(FormId, "Contact", "contact", null, false, null, null, DateTimeOffset.UtcNow, null));

        for (var i = 0; i < 5; i++)
        {
            await submissionRepository.Save(new Submission(
                $"sub-{i}", FormId, DateTimeOffset.UtcNow.AddMinutes(i), "1.2.3.4", "agent",
                new Dictionary<string, object?> { ["name"] = $"Person {i}" }, SubmissionStatus.Ham, 0, Array.Empty<string>()));
        }

        var result = await manager.ListSubmissions(new ListSubmissionsInput(FormId, 1, 2, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(5, result.Value.TotalItems);
    }

    [Fact]
    public async Task ListSubmissions_ShouldFilterByStatus()
    {
        await formRepository.Save(new Form(FormId, "Contact", "contact", null, false, null, null, DateTimeOffset.UtcNow, null));

        await submissionRepository.Save(MakeSubmission("sub-ham", DateTimeOffset.UtcNow, status: SubmissionStatus.Ham));
        await submissionRepository.Save(MakeSubmission("sub-spam", DateTimeOffset.UtcNow, status: SubmissionStatus.Spam, score: 25));

        var result = await manager.ListSubmissions(new ListSubmissionsInput(FormId, 1, 50, SubmissionStatus.Spam));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("sub-spam", result.Value.Items[0].Id);
        Assert.Equal(SubmissionStatus.Spam, result.Value.Items[0].Status);
    }

    [Fact]
    public async Task GetSubmission_ShouldFail_WhenSubmissionDoesNotExist()
    {
        var result = await manager.GetSubmission(new GetSubmissionInput("missing"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.SubmissionNotFound, result.Error.Code);
    }

    [Fact]
    public async Task GetSubmission_ShouldReturnFullPayload_AndSpamVerdict()
    {
        var payload = new Dictionary<string, object?> { ["name"] = "Pedro", ["email"] = "pedro@example.com" };
        await submissionRepository.Save(new Submission(
            "sub-1", FormId, DateTimeOffset.UtcNow, "1.2.3.4", "agent", payload,
            SubmissionStatus.Spam, 25, ["message_contains_url", "message_too_long"]));

        var result = await manager.GetSubmission(new GetSubmissionInput("sub-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(FormId, result.Value.FormId);
        Assert.Equal("Pedro", result.Value.Payload["name"]);
        Assert.Equal(SubmissionStatus.Spam, result.Value.Status);
        Assert.Equal(25, result.Value.SpamScore);
        Assert.Equal(["message_contains_url", "message_too_long"], result.Value.SpamReasons);
    }

    [Fact]
    public async Task GetSubmission_ShouldStripReservedFields_FromPayload()
    {
        var payload = new Dictionary<string, object?> { ["name"] = "Pedro", ["_honeypot"] = "" };
        await submissionRepository.Save(new Submission(
            "sub-1", FormId, DateTimeOffset.UtcNow, "1.2.3.4", "agent", payload, SubmissionStatus.Ham, 0, Array.Empty<string>()));

        var result = await manager.GetSubmission(new GetSubmissionInput("sub-1"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Payload.ContainsKey("_honeypot"));
    }
}
