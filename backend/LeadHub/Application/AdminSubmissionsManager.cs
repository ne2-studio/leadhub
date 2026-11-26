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
    public const string MarkedByAdminReason = "marked_by_admin";

    public async Task<Result<PaginatedResult<SubmissionSummary>, Error>> ListSubmissions(ListSubmissionsInput input)
    {
        var form = await formRepository.GetOwnedById(input.OwnerId, input.FormId);
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
        var submission = await GetOwnedSubmission(input.OwnerId, input.SubmissionId);
        if (submission.IsFailure)
            return Result.Failure<SubmissionDetails, Error>(submission.Error);

        logger.LogInformation("GetSubmission - Fetched submission {SubmissionId}", input.SubmissionId);
        return Result.Success<SubmissionDetails, Error>(ToDetails(submission.Value));
    }

    /// <summary>
    /// A manual verdict overrides the automatic status but keeps the score and reasons as evidence, adding
    /// <see cref="MarkedByAdminReason"/>. It sends no notification: marking a submission as ham is a review, not a new lead.
    /// </summary>
    public async Task<Result<SubmissionDetails, Error>> MarkSubmission(MarkSubmissionInput input)
    {
        var submission = await GetOwnedSubmission(input.OwnerId, input.SubmissionId);
        if (submission.IsFailure)
            return Result.Failure<SubmissionDetails, Error>(submission.Error);

        var status = input.IsSpam ? SubmissionStatus.Spam : SubmissionStatus.Ham;
        IReadOnlyList<string> reasons = submission.Value.SpamReasons.Contains(MarkedByAdminReason)
            ? submission.Value.SpamReasons
            : [.. submission.Value.SpamReasons, MarkedByAdminReason];
        var marked = submission.Value with { Status = status, SpamReasons = reasons };

        await submissionRepository.UpdateAnalysis(marked.Id, new SpamVerdict(marked.SpamScore, marked.Status, marked.SpamReasons));

        logger.LogInformation("MarkSubmission - Marked submission {SubmissionId} as {Status}", marked.Id, status);
        return Result.Success<SubmissionDetails, Error>(ToDetails(marked));
    }

    private async Task<Result<Submission, Error>> GetOwnedSubmission(string ownerId, string submissionId)
    {
        var submission = await submissionRepository.GetById(submissionId);
        if (submission == null)
            return Result.Failure<Submission, Error>(Errors.SubmissionNotFound(submissionId));

        // Someone else's submission is reported as not found, so its existence isn't leaked.
        var form = await formRepository.GetOwnedById(ownerId, submission.FormId);
        if (form == null)
            return Result.Failure<Submission, Error>(Errors.SubmissionNotFound(submissionId));

        return Result.Success<Submission, Error>(submission);
    }

    private static SubmissionDetails ToDetails(Submission submission) =>
        new(submission.Id, submission.FormId, submission.CreatedAt, submission.IpAddress, submission.UserAgent,
            ReservedFields.Strip(submission.Payload), submission.Status, submission.SpamScore, submission.SpamReasons);

    private static SubmissionSummary ToSummary(SubmissionSummaryProjection projection) =>
        new(projection.Id, projection.CreatedAt, projection.IpAddress, projection.Preview, projection.Status);
}
