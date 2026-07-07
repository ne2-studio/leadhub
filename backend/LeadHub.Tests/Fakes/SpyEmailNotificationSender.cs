using CSharpFunctionalExtensions;
using LeadHub.Ports.Output;

namespace LeadHub.Tests.Fakes;

public class SpyEmailNotificationSender : IEmailNotificationSender
{
    public List<SendSubmissionNotificationInput> SentNotifications { get; } = new();
    public bool ShouldFail { get; set; }

    public Task<UnitResult<Error>> SendSubmissionNotification(SendSubmissionNotificationInput input)
    {
        SentNotifications.Add(input);
        return Task.FromResult(ShouldFail
            ? UnitResult.Failure(Errors.NotificationFailed("Submission was stored but notification failed."))
            : UnitResult.Success<Error>());
    }
}
