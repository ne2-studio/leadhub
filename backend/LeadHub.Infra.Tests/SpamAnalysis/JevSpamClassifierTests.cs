using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using LeadHub.Infra.SpamAnalysis;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class JevSpamClassifierTests
{
    private static readonly IReadOnlyList<KeyValuePair<string, string>> Fields =
        [new("name", "Pedro"), new("message", "Hello, please contact me.")];

    private static (JevSpamClassifier Classifier, StubHandler Handler) MakeClassifier(
        Func<HttpResponseMessage> respond, SpamClassifierOptions? options = null)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://openrouter.test/api/") };
        return (new JevSpamClassifier(client, Options.Create(options ?? new SpamClassifierOptions())), handler);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, object body) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Noul(object probability) => Json(HttpStatusCode.OK, new
    {
        id = "gen-dec-1",
        model = "typesafe/jev-1.14",
        answers = new { is_spam = new { type = "noul", noul = probability } },
        usage = new { input_tokens = 1, output_tokens = 1 }
    });

    [Fact]
    public async Task Classify_ShouldReturnNoulProbability()
    {
        var (classifier, _) = MakeClassifier(() => Noul(0.92));

        var result = await classifier.Classify(Fields, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.92, result.Value.SpamProbability);
    }

    [Fact]
    public async Task Classify_ShouldPostNoulDecision_WithConfiguredModel()
    {
        var (classifier, handler) = MakeClassifier(() => Noul(0.1), new SpamClassifierOptions { Model = "~typesafe/jev-latest" });

        await classifier.Classify(Fields, CancellationToken.None);

        Assert.Equal("https://openrouter.test/api/alpha/decisions", handler.LastRequestUri?.ToString());
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        Assert.Equal("~typesafe/jev-latest", root.GetProperty("model").GetString());
        Assert.Equal("name: Pedro\nmessage: Hello, please contact me.", root.GetProperty("state").GetString());
        var question = root.GetProperty("questions").GetProperty("is_spam");
        Assert.Equal("noul", question.GetProperty("type").GetString());
        Assert.True(question.GetProperty("criteria").TryGetProperty("true", out _));
        Assert.True(question.GetProperty("criteria").TryGetProperty("false", out _));
    }

    [Fact]
    public async Task Classify_ShouldTruncateState_ToMaxInputChars()
    {
        var (classifier, handler) = MakeClassifier(() => Noul(0.1), new SpamClassifierOptions { MaxInputChars = 10 });

        await classifier.Classify(Fields, CancellationToken.None);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("name: Pedr", body.RootElement.GetProperty("state").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.PaymentRequired)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task Classify_ShouldFail_WhenApiReturnsError(HttpStatusCode status)
    {
        var (classifier, _) = MakeClassifier(() => Json(status, new { error = new { code = (int)status, message = "nope" } }));

        Assert.True((await classifier.Classify(Fields, CancellationToken.None)).IsFailure);
    }

    [Fact]
    public async Task Classify_ShouldFail_WhenAnswerMissing()
    {
        var (classifier, _) = MakeClassifier(() => Json(HttpStatusCode.OK, new { answers = new { } }));

        Assert.True((await classifier.Classify(Fields, CancellationToken.None)).IsFailure);
    }

    [Fact]
    public async Task Classify_ShouldFail_WhenProbabilityNotNumber()
    {
        var (classifier, _) = MakeClassifier(() => Noul("high"));

        Assert.True((await classifier.Classify(Fields, CancellationToken.None)).IsFailure);
    }

    [Fact]
    public async Task Classify_ShouldFail_WhenProbabilityOutOfRange()
    {
        var (classifier, _) = MakeClassifier(() => Noul(1.5));

        Assert.True((await classifier.Classify(Fields, CancellationToken.None)).IsFailure);
    }

    [Fact]
    public async Task Classify_ShouldFail_WhenBodyNotJson()
    {
        var (classifier, _) = MakeClassifier(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("oops") });

        Assert.True((await classifier.Classify(Fields, CancellationToken.None)).IsFailure);
    }

    [Fact]
    public async Task Classify_ShouldFail_WhenRequestThrowsOrTimesOut()
    {
        var (network, _) = MakeClassifier(() => throw new HttpRequestException("boom"));
        var (timeout, _) = MakeClassifier(() => throw new TaskCanceledException("HttpClient.Timeout elapsed"));

        Assert.True((await network.Classify(Fields, CancellationToken.None)).IsFailure);
        Assert.True((await timeout.Classify(Fields, CancellationToken.None)).IsFailure);
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
