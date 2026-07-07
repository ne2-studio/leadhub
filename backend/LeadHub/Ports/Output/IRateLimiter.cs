using CSharpFunctionalExtensions;

namespace LeadHub.Ports.Output;

public interface IRateLimiter
{
    Task<UnitResult<Error>> EnsureAllowed(RateLimitInput input);
}

public sealed record RateLimitInput(
    string FormSlug,
    string? IpAddress
);
