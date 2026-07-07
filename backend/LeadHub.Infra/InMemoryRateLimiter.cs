using System.Collections.Concurrent;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using LeadHub.Ports.Output;

namespace LeadHub.Infra;

public class RateLimiterOptions
{
    public int PermitLimit { get; set; } = 10;
    public int WindowMinutes { get; set; } = 1;
}

/// <summary>
/// In-memory fixed-window rate limiter keyed by (form slug, IP), registered Singleton in
/// ServiceRegistration so the counters survive across requests — a deliberate exception to the
/// default Scoped lifetime. This is a business-rule limiter distinct from the infra-level,
/// ASP.NET Core rate limiter applied to the public submit route in Program.cs.
/// </summary>
public class InMemoryRateLimiter(IOptions<RateLimiterOptions> options) : IRateLimiter
{
    private readonly ConcurrentDictionary<string, (int Count, DateTimeOffset WindowStart)> _windows = new();
    private readonly RateLimiterOptions _options = options.Value;

    public Task<UnitResult<Error>> EnsureAllowed(RateLimitInput input)
    {
        var key = $"{input.FormSlug}:{input.IpAddress ?? "unknown"}";
        var now = DateTimeOffset.UtcNow;

        var entry = _windows.AddOrUpdate(
            key,
            _ => (1, now),
            (_, existing) => now - existing.WindowStart > TimeSpan.FromMinutes(_options.WindowMinutes)
                ? (1, now)
                : (existing.Count + 1, existing.WindowStart));

        return Task.FromResult(entry.Count > _options.PermitLimit
            ? UnitResult.Failure(Errors.RateLimitExceeded())
            : UnitResult.Success<Error>());
    }
}
