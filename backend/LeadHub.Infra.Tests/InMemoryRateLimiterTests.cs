using Microsoft.Extensions.Options;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests;

public class InMemoryRateLimiterTests
{
    private static InMemoryRateLimiter CreateLimiter(int permitLimit) =>
        new(Options.Create(new RateLimiterOptions { PermitLimit = permitLimit, WindowMinutes = 1 }));

    [Fact]
    public async Task EnsureAllowed_ShouldSucceed_WithinLimit()
    {
        var limiter = CreateLimiter(permitLimit: 2);
        var input = new RateLimitInput("contact", "1.2.3.4");

        Assert.True((await limiter.EnsureAllowed(input)).IsSuccess);
        Assert.True((await limiter.EnsureAllowed(input)).IsSuccess);
    }

    [Fact]
    public async Task EnsureAllowed_ShouldFail_WhenLimitExceeded()
    {
        var limiter = CreateLimiter(permitLimit: 1);
        var input = new RateLimitInput("contact", "1.2.3.4");

        Assert.True((await limiter.EnsureAllowed(input)).IsSuccess);
        var result = await limiter.EnsureAllowed(input);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.RateLimitExceeded, result.Error.Code);
    }

    [Fact]
    public async Task EnsureAllowed_ShouldTrackDifferentIpsIndependently()
    {
        var limiter = CreateLimiter(permitLimit: 1);

        Assert.True((await limiter.EnsureAllowed(new RateLimitInput("contact", "1.2.3.4"))).IsSuccess);
        Assert.True((await limiter.EnsureAllowed(new RateLimitInput("contact", "5.6.7.8"))).IsSuccess);
    }

    [Fact]
    public async Task EnsureAllowed_ShouldTrackDifferentFormsIndependently()
    {
        var limiter = CreateLimiter(permitLimit: 1);

        Assert.True((await limiter.EnsureAllowed(new RateLimitInput("contact", "1.2.3.4"))).IsSuccess);
        Assert.True((await limiter.EnsureAllowed(new RateLimitInput("newsletter", "1.2.3.4"))).IsSuccess);
    }
}
