using Microsoft.Extensions.Options;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>
/// Sums the score contributed by every registered <see cref="ISpamRule"/> and classifies the
/// result against configurable thresholds. Purely a function of the submission's stored data, so
/// it's deterministic and safe to re-run on retry.
/// </summary>
public class RuleBasedSpamAnalyzer(IEnumerable<ISpamRule> rules, IOptions<SpamScoringOptions> options) : ISpamAnalyzer
{
    public SpamVerdict Analyze(Submission submission)
    {
        var hits = rules.Select(rule => rule.Evaluate(submission)).Where(hit => hit != null).Select(hit => hit!).ToList();

        var score = hits.Sum(hit => hit.Score);
        var reasons = hits.Select(hit => hit.Reason).ToList();

        return new SpamVerdict(score, Classify(score, options.Value), reasons);
    }

    private static SubmissionStatus Classify(int score, SpamScoringOptions thresholds) => score switch
    {
        _ when score >= thresholds.SpamThreshold => SubmissionStatus.Spam,
        _ when score >= thresholds.SuspectedSpamThreshold => SubmissionStatus.SuspectedSpam,
        _ => SubmissionStatus.Ham
    };
}
