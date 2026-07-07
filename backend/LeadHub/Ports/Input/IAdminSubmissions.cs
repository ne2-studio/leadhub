using CSharpFunctionalExtensions;

namespace LeadHub.Ports.Input;

/// <summary>
/// Use cases available to the authenticated administrator for browsing submissions.
/// </summary>
public interface IAdminSubmissions
{
    Task<Result<PaginatedResult<SubmissionSummary>, Error>> ListSubmissions(ListSubmissionsInput input);

    Task<Result<SubmissionDetails, Error>> GetSubmission(GetSubmissionInput input);
}
