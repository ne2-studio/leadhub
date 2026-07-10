using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using LeadHub.Ports.Input;
using LeadHub.Ports.Output;

namespace LeadHub.Application;

/// <summary>
/// Runs the background analysis job: scores a pending submission, records the verdict, and — only
/// for a Ham verdict — sends the owner's email notification. Returning a failure (e.g. the
/// notification failing to send) signals the queue worker to retry; re-running is safe because
/// analysis is deterministic and updating the verdict is idempotent.
/// </summary>
public class SubmissionAnalysisManager(
    ILogger<SubmissionAnalysisManager> logger,
    ISubmissionRepository submissionRepository,
    IFormRepository formRepository,
    ISpamAnalyzer spamAnalyzer,
    IEmailNotificationSender emailSender) : ISubmissionAnalysis
{
    public async Task<UnitResult<Error>> AnalyzeSubmission(AnalyzeSubmissionInput input)
    {
        var submission = await submissionRepository.GetById(input.SubmissionId);
        if (submission == null)
        {
            logger.LogWarning("AnalyzeSubmission - Submission {SubmissionId} not found", input.SubmissionId);
            return UnitResult.Failure(Errors.SubmissionNotFound(input.SubmissionId));
        }

        var verdict = spamAnalyzer.Analyze(submission);
        await submissionRepository.UpdateAnalysis(submission.Id, verdict);

        logger.LogInformation(
            "AnalyzeSubmission - Submission {SubmissionId} classified as {Status} (score {Score})",
            submission.Id, verdict.Status, verdict.Score);

        if (verdict.Status != SubmissionStatus.Ham)
            return UnitResult.Success<Error>();

        var form = await formRepository.GetById(submission.FormId);
        if (form is not { NotificationsEnabled: true } || string.IsNullOrWhiteSpace(form.NotificationEmail))
            return UnitResult.Success<Error>();

        var notification = await emailSender.SendSubmissionNotification(new SendSubmissionNotificationInput(
            form.NotificationEmail, form.Name, submission.CreatedAt, ReservedFields.Strip(submission.Payload),
            submission.IpAddress, submission.UserAgent));

        if (notification.IsFailure)
        {
            logger.LogError(
                "AnalyzeSubmission - Notification failed for submission {SubmissionId}: {Error}",
                submission.Id, notification.Error.Message);
            return UnitResult.Failure(notification.Error);
        }

        return UnitResult.Success<Error>();
    }
}
