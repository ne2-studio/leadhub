using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using ModelContextProtocol;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;

namespace LeadHub.Api.Mcp;

public static class McpServerSetup
{
    public const string Path = "/mcp";

    public static readonly Type[] ToolTypes = [typeof(FormsTools), typeof(SubmissionsTools)];

    /// <summary>
    /// Registers the MCP server. Its tools mirror the admin REST endpoints one to one and call the same input ports
    /// (<c>IAdminForms</c>, <c>IAdminSubmissions</c>), so no business logic lives here.
    /// </summary>
    public static IServiceCollection AddLeadHubMcp(this IServiceCollection services)
    {
        // Same JSON shape as the REST API: enums by name ("Spam") and empty fields as null instead of omitted.
        var serializerOptions = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };
        serializerOptions.Converters.Add(new JsonStringEnumConverter());

        services
            .AddMcpServer(options => options.ServerInfo = new Implementation { Name = "leadhub", Version = "1.0.0" })
            .WithHttpTransport(options => options.Stateless = true)
            .WithTools((IEnumerable<Type>)ToolTypes, serializerOptions)
            .WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                try
                {
                    return await next(context, cancellationToken);
                }
                catch (ToolErrorException exception)
                {
                    return exception.ToCallToolResult();
                }
            }));
        return services;
    }

    /// <summary>
    /// Publishes the OAuth protected resource metadata (RFC 9728) so MCP clients discover the same OIDC provider the
    /// admin panel uses. Token validation stays with the JwtBearer scheme.
    /// </summary>
    public static AuthenticationBuilder AddLeadHubMcpChallenge(this AuthenticationBuilder builder, IConfiguration configuration) =>
        builder.AddMcp(options =>
        {
            var resourceUrl = configuration["Mcp:ResourceUrl"];
            options.ResourceMetadata = new ProtectedResourceMetadata
            {
                Resource = string.IsNullOrEmpty(resourceUrl) ? null : resourceUrl,
                AuthorizationServers = { configuration["Auth:Authority"]! },
                ScopesSupported = configuration.GetSection("Mcp:Scopes").Get<List<string>>() ?? ["openid", "profile", "offline_access"],
                ResourceName = "LeadHub",
            };
        });

    public static IEndpointConventionBuilder MapLeadHubMcp(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapMcp(Path).RequireAuthorization();
}
