using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LeadHub.Api.Mcp;
using LeadHub.Application;
using LeadHub.Ports.Input;
using LeadHub.Ports.Output;
using LeadHub.Tests.Fakes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LeadHub.Api.Tests;

public sealed class McpServerTests : IAsyncLifetime
{
    private const string Authority = "https://auth.example.test";
    private const string ResourceUrl = "https://leadhub.example.test/mcp";

    private readonly InMemorySubmissionRepository submissions = new();
    private WebApplication app = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Authority"] = Authority,
            ["Mcp:ResourceUrl"] = ResourceUrl,
        });

        builder.Services.AddSingleton<IFormRepository, InMemoryFormRepository>();
        builder.Services.AddSingleton<ISubmissionRepository>(submissions);
        builder.Services.AddSingleton<IClock, StaticClock>();
        builder.Services.AddSingleton<IIdGenerator, SequentialIdGenerator>();
        builder.Services.AddScoped<IAdminForms, AdminFormsManager>();
        builder.Services.AddScoped<IAdminSubmissions, AdminSubmissionsManager>();
        builder.Services.AddLeadHubMcp();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestBearerHandler.SchemeName;
                options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestBearerHandler>(TestBearerHandler.SchemeName, _ => { })
            .AddLeadHubMcpChallenge(builder.Configuration);
        builder.Services.AddAuthorization();

        app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapLeadHubMcp();
        await app.StartAsync();
    }

    public async Task DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public void Every_admin_endpoint_has_an_mcp_tool()
    {
        var adminEndpoints = typeof(Controllers.AdminFormsController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && t.GetCustomAttribute<AuthorizeAttribute>() is not null)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Count(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());
        var tools = McpServerSetup.ToolTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Count(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);

        Assert.Equal(8, adminEndpoints);
        Assert.Equal(adminEndpoints, tools);
        Assert.Equal(tools, app.Services.GetServices<McpServerTool>().Count());
    }

    [Fact]
    public async Task Lists_the_tools_with_read_only_and_destructive_hints()
    {
        await using var client = await Connect("owner-a");

        var tools = await client.ListToolsAsync();

        Assert.Equal(
            [
                "create_form", "delete_form", "get_form", "get_submission", "list_forms", "list_submissions",
                "mark_submission", "update_form",
            ],
            tools.Select(t => t.Name).Order());
        Assert.All(tools.Where(t => t.Name.StartsWith("list_") || t.Name.StartsWith("get_")),
            t => Assert.True(t.ProtocolTool.Annotations?.ReadOnlyHint));
        Assert.True(tools.Single(t => t.Name == "delete_form").ProtocolTool.Annotations?.DestructiveHint);
        Assert.Contains("\"Spam\"", tools.Single(t => t.Name == "list_submissions").JsonSchema.GetRawText());
    }

    [Fact]
    public async Task Anonymous_requests_are_challenged_with_protected_resource_metadata()
    {
        var http = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, McpServerSetup.Path)
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        using var response = await http.SendAsync(request);
        using var metadata = await http.GetAsync("/.well-known/oauth-protected-resource/mcp");
        using var json = JsonDocument.Parse(await metadata.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("resource_metadata=", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
        Assert.Equal(Authority, json.RootElement.GetProperty("authorization_servers")[0].GetString());
        Assert.Equal(ResourceUrl, json.RootElement.GetProperty("resource").GetString());
    }

    [Fact]
    public async Task A_form_can_be_created_edited_and_deleted_through_tools_and_is_scoped_to_its_owner()
    {
        await using var owner = await Connect("owner-a");
        await using var other = await Connect("owner-b");

        var created = await Call(owner, "create_form", new()
        {
            ["name"] = "Taller",
            ["slug"] = "exeal-taller",
            ["notificationsEnabled"] = true,
            ["notificationEmail"] = "owner@example.com",
            ["thankYouUrl"] = "https://www.example.com/taller/gracias/",
        });
        var formId = created.GetProperty("id").GetString();
        var updated = await Call(owner, "update_form", new() { ["formId"] = formId, ["description"] = "Talleres gratuitos" });

        Assert.Equal("exeal-taller", created.GetProperty("slug").GetString());
        Assert.Equal("Talleres gratuitos", updated.GetProperty("description").GetString());
        Assert.Equal("Taller", updated.GetProperty("name").GetString());
        Assert.Equal("owner@example.com", updated.GetProperty("notificationEmail").GetString());
        Assert.Equal("https://www.example.com/taller/gracias/", updated.GetProperty("thankYouUrl").GetString());
        Assert.Single((await Call(owner, "list_forms")).EnumerateArray());
        Assert.Empty((await Call(other, "list_forms")).EnumerateArray());
        Assert.Equal("Taller", (await Call(owner, "get_form", new() { ["formId"] = formId })).GetProperty("name").GetString());

        await Call(owner, "delete_form", new() { ["formId"] = formId });
        Assert.Empty((await Call(owner, "list_forms")).EnumerateArray());
    }

    [Fact]
    public async Task Update_form_keeps_omitted_fields_and_clears_optional_ones_given_as_empty()
    {
        await using var owner = await Connect("owner-a");
        var formId = await CreateForm(owner, description: "Antigua");

        var updated = await Call(owner, "update_form", new()
        {
            ["formId"] = formId,
            ["description"] = "",
            ["thankYouUrl"] = "",
            ["notificationsEnabled"] = false,
        });

        Assert.Equal(JsonValueKind.Null, updated.GetProperty("description").ValueKind);
        Assert.Equal(JsonValueKind.Null, updated.GetProperty("thankYouUrl").ValueKind);
        Assert.False(updated.GetProperty("notificationsEnabled").GetBoolean());
        Assert.Equal("contact", updated.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task Submissions_can_be_filtered_read_and_marked_as_spam_or_ham()
    {
        await using var owner = await Connect("owner-a");
        var formId = await CreateForm(owner);
        await submissions.Save(MakeSubmission("sub-ham", formId!, SubmissionStatus.Ham));
        await submissions.Save(MakeSubmission("sub-spam", formId!, SubmissionStatus.Spam));

        var spam = await Call(owner, "list_submissions", new() { ["formId"] = formId, ["status"] = "Spam" });
        var detail = await Call(owner, "get_submission", new() { ["submissionId"] = "sub-ham" });
        var markedSpam = await Call(owner, "mark_submission", new() { ["submissionId"] = "sub-ham", ["isSpam"] = true });
        var markedHam = await Call(owner, "mark_submission", new() { ["submissionId"] = "sub-spam", ["isSpam"] = false });

        Assert.Equal("sub-spam", spam.GetProperty("items").EnumerateArray().Single().GetProperty("id").GetString());
        Assert.Equal("Spam", spam.GetProperty("items")[0].GetProperty("status").GetString());
        Assert.Equal("Pedro", detail.GetProperty("payload").GetProperty("name").GetString());
        Assert.Equal("Spam", markedSpam.GetProperty("status").GetString());
        Assert.Equal("Ham", markedHam.GetProperty("status").GetString());
        Assert.Equal("sub-ham", (await Call(owner, "list_submissions", new() { ["formId"] = formId, ["status"] = "Spam" }))
            .GetProperty("items").EnumerateArray().Single().GetProperty("id").GetString());
    }

    [Fact]
    public async Task Use_case_errors_are_reported_as_tool_errors_with_the_rest_status_and_code()
    {
        await using var owner = await Connect("owner-a");
        await CreateForm(owner);

        var duplicated = await owner.CallToolAsync("create_form", new Dictionary<string, object?> { ["name"] = "Otro", ["slug"] = "contact" });
        var invalid = await owner.CallToolAsync("create_form", new Dictionary<string, object?>
        {
            ["name"] = "Otro", ["slug"] = "otro", ["notificationsEnabled"] = true,
        });
        var missing = await owner.CallToolAsync("get_submission", new Dictionary<string, object?> { ["submissionId"] = "nope" });

        Assert.True(duplicated.IsError);
        Assert.StartsWith("409 SLUG_ALREADY_EXISTS:", Text(duplicated));
        Assert.True(invalid.IsError);
        Assert.StartsWith("400 INVALID_FORM_CONFIGURATION:", Text(invalid));
        Assert.True(missing.IsError);
        Assert.StartsWith("404 SUBMISSION_NOT_FOUND:", Text(missing));
    }

    [Fact]
    public async Task Forms_and_submissions_of_other_owners_are_reported_as_not_found()
    {
        await using var owner = await Connect("owner-a");
        await using var intruder = await Connect("owner-b");
        var formId = await CreateForm(owner);
        await submissions.Save(MakeSubmission("sub-1", formId!, SubmissionStatus.Ham));

        var form = await intruder.CallToolAsync("update_form", new Dictionary<string, object?> { ["formId"] = formId, ["name"] = "Mío" });
        var list = await intruder.CallToolAsync("list_submissions", new Dictionary<string, object?> { ["formId"] = formId });
        var mark = await intruder.CallToolAsync("mark_submission", new Dictionary<string, object?> { ["submissionId"] = "sub-1", ["isSpam"] = true });

        Assert.StartsWith("404 FORM_NOT_FOUND:", Text(form));
        Assert.StartsWith("404 FORM_NOT_FOUND:", Text(list));
        Assert.StartsWith("404 SUBMISSION_NOT_FOUND:", Text(mark));
        Assert.Equal(SubmissionStatus.Ham, (await submissions.GetById("sub-1"))!.Status);
    }

    [Fact]
    public async Task A_token_without_subject_is_rejected()
    {
        await using var client = await Connect(TestBearerHandler.NoSubject);

        var result = await client.CallToolAsync("list_forms", new Dictionary<string, object?>());

        Assert.True(result.IsError);
        Assert.StartsWith("401 UNAUTHORIZED:", Text(result));
    }

    private static async Task<string?> CreateForm(McpClient client, string? description = null) =>
        (await Call(client, "create_form", new()
        {
            ["name"] = "Contact",
            ["slug"] = "contact",
            ["description"] = description,
            ["thankYouUrl"] = "https://example.com/thanks",
        })).GetProperty("id").GetString();

    private static Submission MakeSubmission(string id, string formId, SubmissionStatus status) =>
        new(id, formId, DateTimeOffset.UtcNow, "1.2.3.4", "agent",
            new Dictionary<string, object?> { ["name"] = "Pedro", ["_honeypot"] = "" }, status, 0, []);

    private async Task<McpClient> Connect(string userId)
    {
        var http = app.GetTestClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", userId);
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, McpServerSetup.Path), TransportMode = HttpTransportMode.StreamableHttp },
            http,
            ownsHttpClient: true);
        return await McpClient.CreateAsync(transport);
    }

    private static async Task<JsonElement> Call(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments ?? []);
        Assert.True(result.IsError is not true, Text(result));
        return JsonDocument.Parse(Text(result)).RootElement;
    }

    private static string Text(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    private sealed class SequentialIdGenerator : IIdGenerator
    {
        private int next;

        public string NewId() => $"id-{Interlocked.Increment(ref next)}";
    }

    /// <summary>Treats the bearer token as the OIDC subject, standing in for a validated access token.</summary>
    private sealed class TestBearerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "TestBearer";
        public const string NoSubject = "no-subject";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith("Bearer ")) return Task.FromResult(AuthenticateResult.NoResult());

            var subject = header["Bearer ".Length..];
            Claim[] claims = subject == NoSubject ? [new Claim("name", "machine")] : [new Claim(ClaimTypes.NameIdentifier, subject)];
            var identity = new ClaimsIdentity(claims, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }
}
