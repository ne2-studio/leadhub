using LeadHub.Ports.Output;

namespace LeadHub.Tests.Fakes;

public class InMemorySubmissionRepository : ISubmissionRepository
{
    private readonly Dictionary<string, Submission> storage = new();

    public Task Save(Submission submission)
    {
        storage[submission.Id] = submission;
        return Task.CompletedTask;
    }

    public Task<Submission?> GetById(string id) => Task.FromResult(storage.GetValueOrDefault(id));

    public Task<PaginatedResult<SubmissionSummaryProjection>> ListByForm(string formId, int page, int pageSize, SubmissionStatus? status)
    {
        var matching = storage.Values
            .Where(s => s.FormId == formId)
            .Where(s => status == null || s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        var items = matching
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubmissionSummaryProjection(s.Id, s.CreatedAt, s.IpAddress, "preview", s.Status))
            .ToList();

        IReadOnlyList<SubmissionSummaryProjection> readOnlyItems = items;
        return Task.FromResult(new PaginatedResult<SubmissionSummaryProjection>(readOnlyItems, page, pageSize, matching.Count));
    }

    public Task<int> CountByForm(string formId) =>
        Task.FromResult(storage.Values.Count(s => s.FormId == formId));

    public Task<DateTimeOffset?> GetLastSubmissionDate(string formId)
    {
        var dates = storage.Values.Where(s => s.FormId == formId).Select(s => s.CreatedAt).ToList();
        return Task.FromResult(dates.Count > 0 ? dates.Max() : (DateTimeOffset?)null);
    }

    public Task UpdateAnalysis(string submissionId, SpamVerdict verdict)
    {
        if (storage.TryGetValue(submissionId, out var submission))
            storage[submissionId] = submission with { Status = verdict.Status, SpamScore = verdict.Score, SpamReasons = verdict.Reasons };

        return Task.CompletedTask;
    }
}
