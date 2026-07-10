using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>Flags submissions where any submitted text contains a URL — common in link-drop spam.</summary>
public class MessageContainsUrlRule : ISpamRule
{
    private static readonly string[] Markers = ["http://", "https://", "www."];

    public SpamRuleHit? Evaluate(Submission submission)
    {
        var hasUrl = PayloadFields.AllTextValues(submission)
            .Any(value => Markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase)));

        return hasUrl ? new SpamRuleHit(10, "message_contains_url") : null;
    }
}
