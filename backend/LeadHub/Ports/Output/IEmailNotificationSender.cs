using CSharpFunctionalExtensions;

namespace LeadHub.Ports.Output;

/// <summary>
/// Sends the notification email for a new submission. Toggled between a real Resend-backed adapter
/// and a no-op via the "Features:EmailNotifications:Enabled" config flag, composed in
/// LeadHub.Infra.ServiceRegistration — use-case code never checks the flag itself.
/// </summary>
public interface IEmailNotificationSender
{
    Task<UnitResult<Error>> SendSubmissionNotification(SendSubmissionNotificationInput input);
}

public sealed record SendSubmissionNotificationInput(
    string To,
    string FormName,
    DateTimeOffset SubmittedAt,
    IReadOnlyDictionary<string, object?> Payload,
    string? IpAddress,
    string? UserAgent
);
