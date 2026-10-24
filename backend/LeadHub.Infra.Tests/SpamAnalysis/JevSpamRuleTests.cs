using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using LeadHub.Infra.SpamAnalysis;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class JevSpamRuleTests
{
    private static Submission MakeSubmission(IReadOnlyDictionary<string, object?> payload) =>
        new("sub-1", "form-1", DateTimeOffset.UtcNow, "1.2.3.4", "agent", payload, SubmissionStatus.PendingReview, 0, Array.Empty<string>());

    private static readonly Submission Lead = MakeSubmission(new Dictionary<string, object?>
    {
        ["name"] = "Pedro",
        ["message"] = "Hello, please contact me.",
        ["_honeypot"] = "trap"
    });

    private static (JevSpamRule Rule, StubHandler Handler) MakeRule(Func<HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://openrouter.test/api/") };
        return (new JevSpamRule(client, Options.Create(new JevOptions()), NullLogger<JevSpamRule>.Instance), handler);
    }

    private static HttpResponseMessage Noul(double probability) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = "typesafe/jev-1.14",
                answers = new { is_spam = new { type = "noul", noul = probability } },
                usage = new { input_tokens = 1, output_tokens = 1 }
            }),
            Encoding.UTF8, "application/json")
    };

    [Theory]
    [InlineData(0.9, 20, "jev_spam")]
    [InlineData(0.85, 20, "jev_spam")]
    [InlineData(0.6, 10, "jev_suspected_spam")]
    public async Task Evaluate_ShouldReturnHit_WhenProbabilityAboveThreshold(double probability, int score, string reason)
    {
        var (rule, _) = MakeRule(() => Noul(probability));

        var hit = await rule.Evaluate(Lead, CancellationToken.None);

        Assert.NotNull(hit);
        Assert.Equal(score, hit.Score);
        Assert.Equal(reason, hit.Reason);
    }

    [Fact]
    public async Task Evaluate_ShouldReturnNull_WhenProbabilityLow()
    {
        var (rule, _) = MakeRule(() => Noul(0.1));

        Assert.Null(await rule.Evaluate(Lead, CancellationToken.None));
    }

    [Fact]
    public async Task Evaluate_ShouldSendNoulQuestion_WithVisibleFieldsOnly()
    {
        var (rule, handler) = MakeRule(() => Noul(0.1));

        await rule.Evaluate(Lead, CancellationToken.None);

        Assert.Equal("https://openrouter.test/api/alpha/decisions", handler.LastRequestUri?.ToString());
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("typesafe/jev-1.14", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("noul", body.RootElement.GetProperty("questions").GetProperty("is_spam").GetProperty("type").GetString());
        var state = body.RootElement.GetProperty("state").GetString()!;
        Assert.Contains("message: Hello, please contact me.", state);
        Assert.DoesNotContain("trap", state);
        Assert.DoesNotContain("1.2.3.4", state);
    }

    [Fact]
    public async Task Evaluate_ShouldSkipCall_WhenNoVisibleText()
    {
        var (rule, handler) = MakeRule(() => Noul(0.9));

        var hit = await rule.Evaluate(MakeSubmission(new Dictionary<string, object?> { ["_honeypot"] = "bot" }), CancellationToken.None);

        Assert.Null(hit);
        Assert.Null(handler.LastRequestUri);
    }

    [Fact]
    public async Task Evaluate_ShouldFailOpen_WhenApiReturnsError()
    {
        var (rule, _) = MakeRule(() => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        Assert.Null(await rule.Evaluate(Lead, CancellationToken.None));
    }

    [Fact]
    public async Task Evaluate_ShouldFailOpen_WhenRequestThrows()
    {
        var (rule, _) = MakeRule(() => throw new HttpRequestException("boom"));

        Assert.Null(await rule.Evaluate(Lead, CancellationToken.None));
    }

    [Fact]
    public async Task Evaluate_ShouldFailOpen_WhenResponseMalformed()
    {
        var (rule, _) = MakeRule(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

        Assert.Null(await rule.Evaluate(Lead, CancellationToken.None));
    }

    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond();
        }
    }
}
