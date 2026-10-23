namespace LeadHub.Ports.Output;

public interface IFormRepository
{
    /// <summary>Unscoped lookup for system flows (e.g. submission analysis). Admin use cases must use GetOwnedById.</summary>
    Task<Form?> GetById(string id);

    Task<Form?> GetOwnedById(string ownerId, string id);

    Task<Form?> GetBySlug(string slug);

    Task<bool> ExistsBySlug(string slug, string? excludingFormId = null);

    Task<IReadOnlyList<FormSummaryProjection>> List(string ownerId);

    Task Save(Form form);

    Task Delete(Form form);
}
