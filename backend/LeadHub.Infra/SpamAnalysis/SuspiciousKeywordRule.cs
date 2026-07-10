using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>Flags submissions mentioning common SEO/scam/gambling spam vocabulary.</summary>
public class SuspiciousKeywordRule : ISpamRule
{
    private static readonly string[] Keywords =
        ["seo", "backlink", "guest post", "casino", "jackpot", "bonus", "crypto", "marketing campaign"];

    public SpamRuleHit? Evaluate(Submission submission)
    {
        var hasKeyword = PayloadFields.AllTextValues(submission)
            .Any(value => Keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

        return hasKeyword ? new SpamRuleHit(10, "suspicious_keyword") : null;
    }
}
