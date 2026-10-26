using CSharpFunctionalExtensions;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>
/// An external model that judges whether lead text is spam. Kept behind this interface so the
/// provider or model can be swapped without touching scoring. A failure result means "couldn't
/// classify" (timeout, HTTP error, malformed answer) — callers fail open on it.
/// </summary>
public interface ISpamClassifier
{
    /// <param name="fields">Visitor-filled text fields only — never IP, user agent or reserved fields.</param>
    Task<Result<SpamClassification>> Classify(IReadOnlyList<KeyValuePair<string, string>> fields, CancellationToken cancellationToken);
}

/// <param name="SpamProbability">0 (genuine lead) to 1 (certainly spam).</param>
public sealed record SpamClassification(double SpamProbability);
