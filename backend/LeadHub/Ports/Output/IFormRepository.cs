namespace LeadHub.Ports.Output;

public interface IFormRepository
{
    Task<Form?> GetById(string id);

    Task<Form?> GetBySlug(string slug);

    Task<bool> ExistsBySlug(string slug, string? excludingFormId = null);

    Task<IReadOnlyList<FormSummaryProjection>> List();

    Task Save(Form form);

    Task Delete(Form form);
}
