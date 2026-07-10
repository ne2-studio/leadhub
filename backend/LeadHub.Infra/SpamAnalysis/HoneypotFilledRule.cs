using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>
/// Flags submissions whose hidden "_honeypot" field carries a value — a field real users never
/// fill in, but naive bots do.
/// </summary>
public class HoneypotFilledRule : ISpamRule
{
    private const string HoneypotField = "_honeypot";

    public SpamRuleHit? Evaluate(Submission submission)
    {
        if (submission.Payload.TryGetValue(HoneypotField, out var value) && HasValue(value))
            return new SpamRuleHit(100, "honeypot_filled");

        return null;
    }

    private static bool HasValue(object? value) => value switch
    {
        null => false,
        string s => !string.IsNullOrEmpty(s),
        _ => true
    };
}
