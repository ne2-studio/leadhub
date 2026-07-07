using System.Net;
using System.Net.Http.Json;
using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LeadHub.Ports.Output;

namespace LeadHub.Infra;

public class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "LeadHub <onboarding@resend.dev>";
}

/// <summary>
/// Sends submission notification emails via the Resend API (https://resend.com).
/// </summary>
public class ResendEmailNotificationSender(
    HttpClient httpClient,
    IOptions<ResendOptions> options,
    ILogger<ResendEmailNotificationSender> logger) : IEmailNotificationSender
{
    public async Task<UnitResult<Error>> SendSubmissionNotification(SendSubmissionNotificationInput input)
    {
        var request = new
        {
            from = options.Value.FromAddress,
            to = new[] { input.To },
            subject = $"New submission — {input.FormName}",
            html = BuildHtml(input)
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync("emails", request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogError("Resend - Failed to send notification ({Status}): {Body}", response.StatusCode, body);
                return UnitResult.Failure(Errors.NotificationFailed("Submission was stored but notification failed."));
            }

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resend - Unexpected error sending notification");
            return UnitResult.Failure(Errors.NotificationFailed("Submission was stored but notification failed."));
        }
    }

    private static string BuildHtml(SendSubmissionNotificationInput input)
    {
        var sb = new StringBuilder();
        sb.Append("<h2>New submission — ").Append(WebUtility.HtmlEncode(input.FormName)).Append("</h2>");
        sb.Append("<p><strong>Submitted at:</strong> ").Append(input.SubmittedAt.ToString("u")).Append("</p>");

        if (!string.IsNullOrWhiteSpace(input.IpAddress))
            sb.Append("<p><strong>IP address:</strong> ").Append(WebUtility.HtmlEncode(input.IpAddress)).Append("</p>");

        if (!string.IsNullOrWhiteSpace(input.UserAgent))
            sb.Append("<p><strong>User agent:</strong> ").Append(WebUtility.HtmlEncode(input.UserAgent)).Append("</p>");

        sb.Append("<table cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">");
        foreach (var (key, value) in input.Payload)
        {
            sb.Append("<tr><td style=\"border:1px solid #ccc\"><strong>").Append(WebUtility.HtmlEncode(key)).Append("</strong></td>")
              .Append("<td style=\"border:1px solid #ccc\">").Append(WebUtility.HtmlEncode(value?.ToString() ?? string.Empty)).Append("</td></tr>");
        }
        sb.Append("</table>");

        return sb.ToString();
    }
}
