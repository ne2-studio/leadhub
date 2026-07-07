using System.Collections.Concurrent;
using LeadHub.Ports.Output;

namespace LeadHub.Infra;

/// <summary>
/// Caching decorator over IFormRepository, composed as a Singleton in ServiceRegistration so the
/// cache survives across requests (the deliberate exception to the default Scoped lifetime) — forms
/// change rarely but GetBySlug is hit on every public submission.
/// </summary>
public class CachedFormRepository(IFormRepository repository) : IFormRepository
{
    private readonly ConcurrentDictionary<string, Form> _byId = new();
    private readonly ConcurrentDictionary<string, Form> _bySlug = new();
    private readonly ConcurrentDictionary<string, string> _idToSlug = new();

    public async Task<Form?> GetById(string id)
    {
        if (_byId.TryGetValue(id, out var cached))
            return cached;

        var form = await repository.GetById(id);
        if (form != null)
            Cache(form);

        return form;
    }

    public async Task<Form?> GetBySlug(string slug)
    {
        if (_bySlug.TryGetValue(slug, out var cached))
            return cached;

        var form = await repository.GetBySlug(slug);
        if (form != null)
            Cache(form);

        return form;
    }

    public Task<bool> ExistsBySlug(string slug, string? excludingFormId = null) =>
        repository.ExistsBySlug(slug, excludingFormId);

    public Task<IReadOnlyList<FormSummaryProjection>> List() => repository.List();

    public async Task Save(Form form)
    {
        await repository.Save(form);
        Cache(form);
    }

    public async Task Delete(Form form)
    {
        await repository.Delete(form);
        _byId.TryRemove(form.Id, out _);
        _bySlug.TryRemove(form.Slug, out _);
        _idToSlug.TryRemove(form.Id, out _);
    }

    private void Cache(Form form)
    {
        // A form's slug can change on update, so the previous slug entry must be evicted —
        // otherwise it would keep serving stale data under the old slug forever.
        if (_idToSlug.TryGetValue(form.Id, out var previousSlug) && previousSlug != form.Slug)
            _bySlug.TryRemove(previousSlug, out _);

        _byId[form.Id] = form;
        _bySlug[form.Slug] = form;
        _idToSlug[form.Id] = form.Slug;
    }
}
