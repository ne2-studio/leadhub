using CSharpFunctionalExtensions;
using LeadHub.Ports.Output;

namespace LeadHub.Infra;

/// <summary>
/// No-op stand-in for IEmailNotificationSender, swapped in via the "Features:EmailNotifications:Enabled"
/// config flag (Null Object pattern) — e.g. for local development without a Resend API key.
/// </summary>
public class NullEmailNotificationSender : IEmailNotificationSender
{
    public Task<UnitResult<Error>> SendSubmissionNotification(SendSubmissionNotificationInput input) =>
        Task.FromResult(UnitResult.Success<Error>());
}
