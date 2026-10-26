using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>
/// A scoring signal that needs I/O (e.g. an external classifier). Same contract as
/// <see cref="ISpamRule"/>: return a hit or null. Implementations must fail open — return null
/// instead of throwing when the external dependency is unavailable — so classification never
/// stalls on a third party.
/// </summary>
public interface IAsyncSpamRule
{
    Task<SpamRuleHit?> Evaluate(Submission submission, CancellationToken cancellationToken);
}
