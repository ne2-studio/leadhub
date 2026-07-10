using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>
/// A single, independent scoring signal. Adding a new rule means adding a new class and
/// registering it in ServiceRegistration — existing rules are never touched.
/// </summary>
public interface ISpamRule
{
    SpamRuleHit? Evaluate(Submission submission);
}

public sealed record SpamRuleHit(int Score, string Reason);
