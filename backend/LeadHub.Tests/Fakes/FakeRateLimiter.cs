using CSharpFunctionalExtensions;
using LeadHub.Ports.Output;

namespace LeadHub.Tests.Fakes;

public class FakeRateLimiter : IRateLimiter
{
    public bool Allow { get; set; } = true;

    public Task<UnitResult<Error>> EnsureAllowed(RateLimitInput input) =>
        Task.FromResult(Allow ? UnitResult.Success<Error>() : UnitResult.Failure(Errors.RateLimitExceeded()));
}
