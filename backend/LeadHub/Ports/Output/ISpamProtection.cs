using CSharpFunctionalExtensions;

namespace LeadHub.Ports.Output;

public interface ISpamProtection
{
    Task<UnitResult<Error>> EnsureNotSpam(SpamCheckInput input);
}

public sealed record SpamCheckInput(
    string FormSlug,
    IReadOnlyDictionary<string, object?> Payload,
    string? IpAddress,
    string? UserAgent
);
