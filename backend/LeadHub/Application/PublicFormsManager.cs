using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using LeadHub.Ports.Input;
using LeadHub.Ports.Output;

namespace LeadHub.Application;

public class PublicFormsManager(
    ILogger<PublicFormsManager> logger,
    IFormRepository formRepository,
    ISubmissionRepository submissionRepository,
    ISpamProtection spamProtection,
    IRateLimiter rateLimiter,
    IEmailNotificationSender emailSender,
    IIdGenerator idGenerator,
    IClock clock) : IPublicForms
{
    public async Task<Result<SubmitFormOutput, Error>> SubmitForm(SubmitFormInput input)
    {
        var form = await formRepository.GetBySlug(input.FormSlug);
        if (form == null)
        {
            logger.LogWarning("SubmitForm - Form with slug {Slug} not found", input.FormSlug);
            return Result.Failure<SubmitFormOutput, Error>(Errors.FormNotFound(input.FormSlug));
        }

        if (input.Payload == null)
            return Result.Failure<SubmitFormOutput, Error>(Errors.InvalidSubmissionPayload("Submission payload must be a JSON object."));

        var spamCheck = await spamProtection.EnsureNotSpam(
            new SpamCheckInput(input.FormSlug, input.Payload, input.IpAddress, input.UserAgent));
        if (spamCheck.IsFailure)
        {
            logger.LogWarning("SubmitForm - Spam detected for form {Slug}", input.FormSlug);
            return Result.Failure<SubmitFormOutput, Error>(spamCheck.Error);
        }

        var rateLimitCheck = await rateLimiter.EnsureAllowed(new RateLimitInput(input.FormSlug, input.IpAddress));
        if (rateLimitCheck.IsFailure)
        {
            logger.LogWarning("SubmitForm - Rate limit exceeded for form {Slug} from {Ip}", input.FormSlug, input.IpAddress);
            return Result.Failure<SubmitFormOutput, Error>(rateLimitCheck.Error);
        }

        var storedPayload = StripReservedFields(input.Payload);
        var submission = new Submission(idGenerator.NewId(), form.Id, clock.UtcNow, input.IpAddress, input.UserAgent, storedPayload);
        await submissionRepository.Save(submission);

        logger.LogInformation("SubmitForm - Stored submission {SubmissionId} for form {Slug}", submission.Id, input.FormSlug);

        if (form.NotificationsEnabled && !string.IsNullOrWhiteSpace(form.NotificationEmail))
        {
            var notification = await emailSender.SendSubmissionNotification(new SendSubmissionNotificationInput(
                form.NotificationEmail, form.Name, submission.CreatedAt, submission.Payload, submission.IpAddress, submission.UserAgent));

            if (notification.IsFailure)
            {
                // Submission persistence is the source of truth; the notification is a side effect
                // that must not roll back the already-stored submission.
                logger.LogError("SubmitForm - Notification failed for form {Slug}: {Error}", input.FormSlug, notification.Error.Message);
                return Result.Failure<SubmitFormOutput, Error>(notification.Error);
            }
        }

        return Result.Success<SubmitFormOutput, Error>(new SubmitFormOutput(true, form.ThankYouUrl));
    }

    private static IReadOnlyDictionary<string, object?> StripReservedFields(IReadOnlyDictionary<string, object?> payload) =>
        payload.Where(kv => !kv.Key.StartsWith('_')).ToDictionary(kv => kv.Key, kv => kv.Value);
}
