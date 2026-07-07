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

    [Fact]
    public async Task ListSubmissions_ShouldFail_WhenFormDoesNotExist()
    {
        var result = await manager.ListSubmissions(new ListSubmissionsInput(FormId, 1, 50));

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
                new Dictionary<string, object?> { ["name"] = $"Person {i}" }));
        }

        var result = await manager.ListSubmissions(new ListSubmissionsInput(FormId, 1, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(5, result.Value.TotalItems);
    }

    [Fact]
    public async Task GetSubmission_ShouldFail_WhenSubmissionDoesNotExist()
    {
        var result = await manager.GetSubmission(new GetSubmissionInput("missing"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.SubmissionNotFound, result.Error.Code);
    }

    [Fact]
    public async Task GetSubmission_ShouldReturnFullPayload()
    {
        var payload = new Dictionary<string, object?> { ["name"] = "Pedro", ["email"] = "pedro@example.com" };
        await submissionRepository.Save(new Submission("sub-1", FormId, DateTimeOffset.UtcNow, "1.2.3.4", "agent", payload));

        var result = await manager.GetSubmission(new GetSubmissionInput("sub-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(FormId, result.Value.FormId);
        Assert.Equal("Pedro", result.Value.Payload["name"]);
    }
}
