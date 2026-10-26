using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

/// <summary>
/// Submissions are schema-less, so rules that care about a particular kind of field (a message, a
/// name) match by key fragment rather than a fixed key name.
/// </summary>
internal static class PayloadFields
{
    public static IEnumerable<string> AllTextValues(Submission submission) =>
        submission.Payload.Values.OfType<string>().Where(v => !string.IsNullOrEmpty(v));

    public static IEnumerable<string> ValuesForKeysContaining(Submission submission, string keyFragment) =>
        submission.Payload
            .Where(kv => kv.Key.Contains(keyFragment, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Value as string)
            .Where(v => !string.IsNullOrEmpty(v))!;

    /// <summary>Text fields the visitor actually filled in — reserved "_" fields (honeypot) excluded.</summary>
    public static IEnumerable<KeyValuePair<string, string>> VisibleTextFields(Submission submission) =>
        submission.Payload
            .Where(kv => !kv.Key.StartsWith('_') && kv.Value is string { Length: > 0 })
            .Select(kv => new KeyValuePair<string, string>(kv.Key, (string)kv.Value!));
}
