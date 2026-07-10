using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>Flags excessively long submitted text, typical of pasted spam blocks.</summary>
public class MessageTooLongRule : ISpamRule
{
    private const int MaxLength = 500;

    public SpamRuleHit? Evaluate(Submission submission)
    {
        var candidates = PayloadFields.ValuesForKeysContaining(submission, "message").ToList();
        if (candidates.Count == 0)
            candidates = PayloadFields.AllTextValues(submission).ToList();

        return candidates.Any(value => value.Length > MaxLength) ? new SpamRuleHit(5, "message_too_long") : null;
    }
}
