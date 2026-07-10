using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using LeadHub.Ports.Input;
using LeadHub.Ports.Output;

namespace LeadHub.Application;

public class PublicFormsManager(
    ILogger<PublicFormsManager> logger,
    IFormRepository formRepository,
    ISubmissionRepository submissionRepository,
    IRateLimiter rateLimiter,
    ISubmissionAnalysisQueue analysisQueue,
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

        var rateLimitCheck = await rateLimiter.EnsureAllowed(new RateLimitInput(input.FormSlug, input.IpAddress));
        if (rateLimitCheck.IsFailure)
        {
            logger.LogWarning("SubmitForm - Rate limit exceeded for form {Slug} from {Ip}", input.FormSlug, input.IpAddress);
            return Result.Failure<SubmitFormOutput, Error>(rateLimitCheck.Error);
        }

        // The raw payload (including reserved "_"-prefixed fields such as the honeypot trap) is
        // kept as-is so the async spam analysis job can inspect it; it's stripped only at display
        // boundaries (admin UI, email notifications) by ReservedFields.Strip.
        var submission = new Submission(
            idGenerator.NewId(), form.Id, clock.UtcNow, input.IpAddress, input.UserAgent, input.Payload,
            SubmissionStatus.PendingReview, 0, Array.Empty<string>());
        await submissionRepository.Save(submission);

        logger.LogInformation(
            "SubmitForm - Stored submission {SubmissionId} for form {Slug}, pending spam analysis", submission.Id, input.FormSlug);

        await analysisQueue.Enqueue(submission.Id);

        // The visitor always gets the normal success flow — spam classification never blocks it.
        return Result.Success<SubmitFormOutput, Error>(new SubmitFormOutput(true, form.ThankYouUrl));
    }
}
