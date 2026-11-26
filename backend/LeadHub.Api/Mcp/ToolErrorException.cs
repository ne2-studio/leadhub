using CSharpFunctionalExtensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LeadHub.Api.Mcp;

/// <summary>
/// A use-case failure, reported to the agent as a tool error with the status and code the REST endpoint would answer,
/// e.g. <c>404 FORM_NOT_FOUND: Form 'x' not found.</c>
/// </summary>
public sealed class ToolErrorException(Error error) : Exception(error.Message)
{
    public CallToolResult ToCallToolResult()
    {
        var (statusCode, code) = ErrorCodeMapper.Map(error.Code);
        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = $"{statusCode} {code}: {error.Message}" }],
        };
    }
}

public static class ToolResultExtensions
{
    public static T OrThrow<T>(this Result<T, Error> result) =>
        result.IsSuccess ? result.Value : throw new ToolErrorException(result.Error);

    public static void OrThrow(this UnitResult<Error> result)
    {
        if (result.IsFailure) throw new ToolErrorException(result.Error);
    }

    /// <summary>Same rule as the controllers: the OIDC subject is the owner every admin use case is scoped to.</summary>
    public static string OwnerId<TParams>(this RequestContext<TParams> context) =>
        context.User?.GetUserId() ?? throw new ToolErrorException(Errors.Unauthorized());
}
