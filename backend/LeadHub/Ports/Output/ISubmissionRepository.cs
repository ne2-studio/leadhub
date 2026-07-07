namespace LeadHub.Ports.Output;

public interface ISubmissionRepository
{
    Task Save(Submission submission);

    Task<Submission?> GetById(string id);

    Task<PaginatedResult<SubmissionSummaryProjection>> ListByForm(string formId, int page, int pageSize);

    Task<int> CountByForm(string formId);

    Task<DateTimeOffset?> GetLastSubmissionDate(string formId);
}
