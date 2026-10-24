using Microsoft.Extensions.Options;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>
/// Sums the score contributed by every registered <see cref="ISpamRule"/> and
/// <see cref="IAsyncSpamRule"/> and classifies the result against configurable thresholds. Local
/// rules are a pure function of the submission's stored data; async rules fail open, so re-running
/// is always safe on retry.
/// </summary>
public class RuleBasedSpamAnalyzer(
    IEnumerable<ISpamRule> rules,
    IEnumerable<IAsyncSpamRule> asyncRules,
    IOptions<SpamScoringOptions> options) : ISpamAnalyzer
{
    public async Task<SpamVerdict> Analyze(Submission submission, CancellationToken cancellationToken = default)
    {
        var hits = rules.Select(rule => rule.Evaluate(submission)).ToList();
        foreach (var asyncRule in asyncRules)
            hits.Add(await asyncRule.Evaluate(submission, cancellationToken));

        var matched = hits.Where(hit => hit != null).Select(hit => hit!).ToList();

        var score = matched.Sum(hit => hit.Score);
        var reasons = matched.Select(hit => hit.Reason).ToList();

        return new SpamVerdict(score, Classify(score, options.Value), reasons);
    }

    private static SubmissionStatus Classify(int score, SpamScoringOptions thresholds) => score switch
    {
        _ when score >= thresholds.SpamThreshold => SubmissionStatus.Spam,
        _ when score >= thresholds.SuspectedSpamThreshold => SubmissionStatus.SuspectedSpam,
        _ => SubmissionStatus.Ham
    };
}
