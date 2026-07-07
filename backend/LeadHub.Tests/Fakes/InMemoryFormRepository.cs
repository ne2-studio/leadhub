using LeadHub.Ports.Output;

namespace LeadHub.Tests.Fakes;

public class InMemoryFormRepository : IFormRepository
{
    private readonly Dictionary<string, Form> storage = new();

    public Task<Form?> GetById(string id) => Task.FromResult(storage.GetValueOrDefault(id));

    public Task<Form?> GetBySlug(string slug) =>
        Task.FromResult(storage.Values.SingleOrDefault(f => f.Slug == slug));

    public Task<bool> ExistsBySlug(string slug, string? excludingFormId = null) =>
        Task.FromResult(storage.Values.Any(f => f.Slug == slug && f.Id != excludingFormId));

    public Task<IReadOnlyList<FormSummaryProjection>> List()
    {
        IReadOnlyList<FormSummaryProjection> summaries = storage.Values
            .Select(f => new FormSummaryProjection(f.Id, f.Name, f.Slug, f.NotificationsEnabled, 0, null))
            .ToList();

        return Task.FromResult(summaries);
    }

    public Task Save(Form form)
    {
        storage[form.Id] = form;
        return Task.CompletedTask;
    }

    public Task Delete(Form form)
    {
        storage.Remove(form.Id);
        return Task.CompletedTask;
    }
}
