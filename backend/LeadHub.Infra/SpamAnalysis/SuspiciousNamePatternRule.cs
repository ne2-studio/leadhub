using System.Text.RegularExpressions;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>
/// Flags name-like fields that look machine-generated: a run-on word with no space, and either an
/// embedded capital letter (e.g. "RobertSkect") or an unusually long single token
/// (e.g. "Danielepife") — real full names almost always contain a space.
/// </summary>
public partial class SuspiciousNamePatternRule : ISpamRule
{
    private const int SuspiciousLength = 10;

    public SpamRuleHit? Evaluate(Submission submission)
    {
        var isSuspicious = PayloadFields.ValuesForKeysContaining(submission, "name").Any(name =>
            !name.Contains(' ') && (EmbeddedCapital().IsMatch(name) || name.Length > SuspiciousLength));

        return isSuspicious ? new SpamRuleHit(5, "suspicious_name_pattern") : null;
    }

    [GeneratedRegex("[a-z][A-Z]")]
    private static partial Regex EmbeddedCapital();
}
