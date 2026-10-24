using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using LeadHub.Infra.SpamAnalysis;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class OpenRouterSpamClassifierTests
{
    private static readonly IReadOnlyList<KeyValuePair<string, string>> Fields =
        [new("name", "Pedro"), new("message", "Hello, please contact me.")];

    private static (OpenRouterSpamClassifier Classifier, StubHandler Handler) MakeClassifier(
        Func<HttpResponseMessage> respond, SpamClassifierOptions? options = null)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://openrouter.test/api/v1/") };
        return (new OpenRouterSpamClassifier(client, Options.Create(options ?? new SpamClassifierOptions())), handler);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, object body) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Completion(string content) => Json(HttpStatusCode.OK, new
    {
        id = "gen-1",
        model = "some/routed-model",
        choices = new[] { new { index = 0, finish_reason = "stop", message = new { role = "assistant", content } } }
    });

    [Fact]
    public async Task Classify_ShouldParseStructuredVerdict()
    {
        var (classifier, _) = MakeClassifier(() => Completion("""{"is_spam":true,"score":0.92,"reason":"SEO offer"}"""));

        var result = await classifier.Classify(Fields, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new SpamClassification(true, 0.92, "SEO offer"), result.Value);
    }

    [Fact]
    public async Task Classify_ShouldPostChatCompletion_WithConfiguredModelAndJsonSchema()
    {
        var (classifier, handler) = MakeClassifier(
            () => Completion("""{"is_spam":false,"score":0.1,"reason":"ok"}"""),
            new SpamClassifierOptions { Model = "acme/other-model" });

        await classifier.Classify(Fields, CancellationToken.None);

        Assert.Equal("https://openrouter.test/api/v1/chat/completions", handler.LastRequestUri?.ToString());
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        Assert.Equal("acme/other-model", root.GetProperty("model").GetString());
        Assert.Equal(0, root.GetProperty("temperature").GetInt32());
        Assert.Equal("json_schema", root.GetProperty("response_format").GetProperty("type").GetString());
        Assert.True(root.GetProperty("response_format").GetProperty("json_schema").GetProperty("strict").GetBoolean());
        Assert.True(root.GetProperty("provider").GetProperty("require_parameters").GetBoolean());
        var messages = root.GetProperty("messages");
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("name: Pedro\nmessage: Hello, please contact me.", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task Classify_ShouldTruncateInput_ToMaxInputChars()
    {
        var (classifier, handler) = MakeClassifier(
            () => Completion("""{"is_spam":false,"score":0.1,"reason":"ok"}"""),
            new SpamClassifierOptions { MaxInputChars = 10 });

        await classifier.Classify(Fields, CancellationToken.None);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("name: Pedr", body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString());
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

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"is_spam":true}""")]
    [InlineData("""{"is_spam":true,"score":"high","reason":"x"}""")]
    [InlineData("""{"is_spam":true,"score":1.5,"reason":"x"}""")]
    [InlineData("")]
    public async Task Classify_ShouldFail_WhenCompletionContentInvalid(string content)
    {
        var (classifier, _) = MakeClassifier(() => Completion(content));

        Assert.True((await classifier.Classify(Fields, CancellationToken.None)).IsFailure);
    }

    [Fact]
    public async Task Classify_ShouldFail_WhenNoChoices()
    {
        var (classifier, _) = MakeClassifier(() => Json(HttpStatusCode.OK, new { choices = Array.Empty<object>() }));

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
