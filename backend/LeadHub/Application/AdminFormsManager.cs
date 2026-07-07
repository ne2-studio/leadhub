using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using LeadHub.Ports.Input;
using LeadHub.Ports.Output;

namespace LeadHub.Application;

public class AdminFormsManager(
    ILogger<AdminFormsManager> logger,
    IFormRepository formRepository,
    IIdGenerator idGenerator,
    IClock clock) : IAdminForms
{
    public async Task<Result<FormDetails, Error>> CreateForm(CreateFormInput input)
    {
        logger.LogInformation("CreateForm - Creating form with slug {Slug}", input.Slug);

        var validation = Validate(input.Name, input.Slug, input.NotificationsEnabled, input.NotificationEmail, input.ThankYouUrl);
        if (validation.IsFailure)
            return Result.Failure<FormDetails, Error>(validation.Error);

        if (await formRepository.ExistsBySlug(input.Slug))
            return Result.Failure<FormDetails, Error>(Errors.SlugAlreadyExists(input.Slug));

        var form = new Form(
            idGenerator.NewId(),
            input.Name,
            input.Slug,
            input.Description,
            input.NotificationsEnabled,
            input.NotificationEmail,
            input.ThankYouUrl,
            clock.UtcNow,
            null);

        await formRepository.Save(form);

        logger.LogInformation("CreateForm - Created form {FormId} with slug {Slug}", form.Id, form.Slug);
        return Result.Success<FormDetails, Error>(ToDetails(form));
    }

    public async Task<Result<FormDetails, Error>> UpdateForm(UpdateFormInput input)
    {
        logger.LogInformation("UpdateForm - Updating form {FormId}", input.FormId);

        var existing = await formRepository.GetById(input.FormId);
        if (existing == null)
            return Result.Failure<FormDetails, Error>(Errors.FormNotFound(input.FormId));

        var validation = Validate(input.Name, input.Slug, input.NotificationsEnabled, input.NotificationEmail, input.ThankYouUrl);
        if (validation.IsFailure)
            return Result.Failure<FormDetails, Error>(validation.Error);

        if (await formRepository.ExistsBySlug(input.Slug, input.FormId))
            return Result.Failure<FormDetails, Error>(Errors.SlugAlreadyExists(input.Slug));

        var updated = existing with
        {
            Name = input.Name,
            Slug = input.Slug,
            Description = input.Description,
            NotificationsEnabled = input.NotificationsEnabled,
            NotificationEmail = input.NotificationEmail,
            ThankYouUrl = input.ThankYouUrl,
            UpdatedAt = clock.UtcNow
        };

        await formRepository.Save(updated);

        logger.LogInformation("UpdateForm - Updated form {FormId}", updated.Id);
        return Result.Success<FormDetails, Error>(ToDetails(updated));
    }

    public async Task<UnitResult<Error>> DeleteForm(DeleteFormInput input)
    {
        logger.LogInformation("DeleteForm - Deleting form {FormId}", input.FormId);

        var existing = await formRepository.GetById(input.FormId);
        if (existing == null)
            return UnitResult.Failure(Errors.FormNotFound(input.FormId));

        await formRepository.Delete(existing);

        logger.LogInformation("DeleteForm - Deleted form {FormId}", input.FormId);
        return UnitResult.Success<Error>();
    }

    public async Task<Result<IReadOnlyList<FormSummary>, Error>> ListForms()
    {
        var forms = await formRepository.List();
        IReadOnlyList<FormSummary> summaries = forms.Select(ToSummary).ToList();
        return Result.Success<IReadOnlyList<FormSummary>, Error>(summaries);
    }

    public async Task<Result<FormDetails, Error>> GetForm(GetFormInput input)
    {
        var form = await formRepository.GetById(input.FormId);
        if (form == null)
            return Result.Failure<FormDetails, Error>(Errors.FormNotFound(input.FormId));

        return Result.Success<FormDetails, Error>(ToDetails(form));
    }

    private static UnitResult<Error> Validate(
        string name, string slug, bool notificationsEnabled, string? notificationEmail, string? thankYouUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            return UnitResult.Failure(Errors.InvalidFormConfiguration("Name is required."));

        if (string.IsNullOrWhiteSpace(slug) || !SlugValidator.IsValid(slug))
            return UnitResult.Failure(Errors.InvalidFormConfiguration(
                "Slug must be a non-empty, URL-safe value (lowercase letters, numbers, hyphens)."));

        if (notificationsEnabled && string.IsNullOrWhiteSpace(notificationEmail))
            return UnitResult.Failure(Errors.InvalidFormConfiguration(
                "Notification email is required when notifications are enabled."));

        if (!string.IsNullOrWhiteSpace(notificationEmail) && !EmailValidator.IsValid(notificationEmail))
            return UnitResult.Failure(Errors.InvalidFormConfiguration("Notification email must be a valid email address."));

        if (!string.IsNullOrWhiteSpace(thankYouUrl) && !UrlValidator.IsValid(thankYouUrl))
            return UnitResult.Failure(Errors.InvalidFormConfiguration("Thank-you URL must be a valid absolute URL."));

        return UnitResult.Success<Error>();
    }

    private static FormDetails ToDetails(Form form) =>
        new(form.Id, form.Name, form.Slug, form.Description, form.NotificationsEnabled,
            form.NotificationEmail, form.ThankYouUrl, form.CreatedAt, form.UpdatedAt);

    private static FormSummary ToSummary(FormSummaryProjection projection) =>
        new(projection.Id, projection.Name, projection.Slug, projection.NotificationsEnabled,
            projection.SubmissionCount, projection.LastSubmissionAt);
}
