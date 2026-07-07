using CSharpFunctionalExtensions;
using LeadHub.Ports.Output;

namespace LeadHub.Infra;

/// <summary>
/// Rejects submissions whose reserved "_honeypot" field carries a value — a hidden form field real
/// users never fill in, but naive bots do.
/// </summary>
public class HoneypotSpamProtection : ISpamProtection
{
    private const string HoneypotField = "_honeypot";

    public Task<UnitResult<Error>> EnsureNotSpam(SpamCheckInput input)
    {
        if (input.Payload.TryGetValue(HoneypotField, out var value) && HasValue(value))
            return Task.FromResult(UnitResult.Failure(Errors.SpamDetected()));

        return Task.FromResult(UnitResult.Success<Error>());
    }

    private static bool HasValue(object? value) => value switch
    {
        null => false,
        string s => !string.IsNullOrEmpty(s),
        _ => true
    };
}
