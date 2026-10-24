using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>
/// Classifies lead text with TypeSafe Jev through OpenRouter's Decisions API
/// (https://openrouter.ai/docs/guides/community/jev): one yes/no (<c>noul</c>) question, "is this
/// lead spam?", answered with a probability. The model id comes from configuration. Never throws
/// for provider problems: every HTTP, timeout or parsing error becomes a failure result.
/// </summary>
public class JevSpamClassifier(HttpClient httpClient, IOptions<SpamClassifierOptions> options) : ISpamClassifier
{
    private const string QuestionKey = "is_spam";

    private static readonly object Question = new
    {
        type = "noul",
        instructions = "Is this contact-form submission spam rather than a genuine lead?",
        criteria = new Dictionary<string, string>
        {
            ["true"] = "Unsolicited promotion, SEO/backlink offers, scams, phishing, gibberish or automated bot content.",
            ["false"] = "A real person genuinely enquiring about or interested in the site owner's products or services."
        }
    };

    public async Task<Result<SpamClassification>> Classify(
        IReadOnlyList<KeyValuePair<string, string>> fields, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var request = new
        {
            model = settings.Model,
            state = BuildState(fields, settings.MaxInputChars),
            questions = new Dictionary<string, object> { [QuestionKey] = Question }
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync("alpha/decisions", request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<SpamClassification>($"HTTP {(int)response.StatusCode}: {body}");

            return Parse(body);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout surfaces as TaskCanceledException without our token being cancelled.
            return Result.Failure<SpamClassification>($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static Result<SpamClassification> Parse(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var probability = json.RootElement.GetProperty("answers").GetProperty(QuestionKey).GetProperty("noul").GetDouble();
            if (probability is < 0 or > 1)
                return Result.Failure<SpamClassification>($"Probability out of range: {probability}");

            return new SpamClassification(probability);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return Result.Failure<SpamClassification>($"Unexpected response shape: {ex.Message}");
        }
    }

    private static string BuildState(IReadOnlyList<KeyValuePair<string, string>> fields, int maxChars)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in fields)
            sb.Append(key).Append(": ").Append(value).Append('\n');

        return sb.Length > maxChars ? sb.ToString(0, maxChars) : sb.ToString().TrimEnd();
    }
}
