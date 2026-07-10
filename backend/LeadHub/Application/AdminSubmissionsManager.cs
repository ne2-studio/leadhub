using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using LeadHub.Ports.Input;
using LeadHub.Ports.Output;

namespace LeadHub.Application;

public class AdminSubmissionsManager(
    ILogger<AdminSubmissionsManager> logger,
    IFormRepository formRepository,
    ISubmissionRepository submissionRepository) : IAdminSubmissions
{
    public async Task<Result<PaginatedResult<SubmissionSummary>, Error>> ListSubmissions(ListSubmissionsInput input)
    {
        var form = await formRepository.GetById(input.FormId);
        if (form == null)
            return Result.Failure<PaginatedResult<SubmissionSummary>, Error>(Errors.FormNotFound(input.FormId));

        var page = input.Page < 1 ? 1 : input.Page;
        var pageSize = input.PageSize < 1 ? 50 : input.PageSize;

        var result = await submissionRepository.ListByForm(input.FormId, page, pageSize, input.Status);

        IReadOnlyList<SubmissionSummary> items = result.Items.Select(ToSummary).ToList();
        var paginated = new PaginatedResult<SubmissionSummary>(items, result.Page, result.PageSize, result.TotalItems);
        return Result.Success<PaginatedResult<SubmissionSummary>, Error>(paginated);
    }

    public async Task<Result<SubmissionDetails, Error>> GetSubmission(GetSubmissionInput input)
    {
        var submission = await submissionRepository.GetById(input.SubmissionId);
        if (submission == null)
            return Result.Failure<SubmissionDetails, Error>(Errors.SubmissionNotFound(input.SubmissionId));

        logger.LogInformation("GetSubmission - Fetched submission {SubmissionId}", input.SubmissionId);

        var details = new SubmissionDetails(
            submission.Id, submission.FormId, submission.CreatedAt, submission.IpAddress, submission.UserAgent,
            ReservedFields.Strip(submission.Payload), submission.Status, submission.SpamScore, submission.SpamReasons);
        return Result.Success<SubmissionDetails, Error>(details);
    }

    private static SubmissionSummary ToSummary(SubmissionSummaryProjection projection) =>
        new(projection.Id, projection.CreatedAt, projection.IpAddress, projection.Preview, projection.Status);
}
