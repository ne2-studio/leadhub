using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

public class JevOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/";
    public string Model { get; set; } = "typesafe/jev-1.14";
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxStateChars { get; set; } = 4000;
    public double SuspectedSpamProbability { get; set; } = 0.5;
    public double SpamProbability { get; set; } = 0.85;
    public int SuspectedSpamScore { get; set; } = 10;
    public int SpamScore { get; set; } = 20;
}

/// <summary>
/// Asks TypeSafe Jev (via OpenRouter's Decisions API, https://openrouter.ai/docs/guides/community/jev)
/// a single yes/no question — "is this lead spam?" — and turns the returned probability into a
/// score. Only visitor-filled text fields are sent (no IP, user agent or reserved fields). Fails
/// open: any HTTP or parsing error is logged and the rule simply doesn't contribute.
/// </summary>
public class JevSpamRule(
    HttpClient httpClient,
    IOptions<JevOptions> options,
    ILogger<JevSpamRule> logger) : IAsyncSpamRule
{
    private const string QuestionKey = "is_spam";

    public async Task<SpamRuleHit?> Evaluate(Submission submission, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var state = BuildState(submission, settings.MaxStateChars);
        if (state.Length == 0)
            return null;

        var request = new
        {
            model = settings.Model,
            state,
            questions = new Dictionary<string, object>
            {
                [QuestionKey] = new
                {
                    type = "noul",
                    instructions = "Is this contact-form submission spam rather than a genuine lead?",
                    criteria = new Dictionary<string, string>
                    {
                        ["true"] = "Unsolicited promotion, SEO/backlink offers, scams, phishing, gibberish or automated bot content.",
                        ["false"] = "A real person genuinely enquiring about or interested in the site owner's products or services."
                    }
                }
            }
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync("alpha/decisions", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Jev - Decision request failed for submission {SubmissionId} ({Status}): {Body}",
                    submission.Id, response.StatusCode, body);
                return null;
            }

            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var probability = json.RootElement.GetProperty("answers").GetProperty(QuestionKey).GetProperty("noul").GetDouble();

            return probability switch
            {
                _ when probability >= settings.SpamProbability => new SpamRuleHit(settings.SpamScore, "jev_spam"),
                _ when probability >= settings.SuspectedSpamProbability => new SpamRuleHit(settings.SuspectedSpamScore, "jev_suspected_spam"),
                _ => null
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Jev - Decision request errored for submission {SubmissionId}; skipping signal", submission.Id);
            return null;
        }
    }

    private static string BuildState(Submission submission, int maxChars)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in PayloadFields.VisibleTextFields(submission))
            sb.Append(key).Append(": ").Append(value).Append('\n');

        return sb.Length > maxChars ? sb.ToString(0, maxChars) : sb.ToString().TrimEnd();
    }
}
