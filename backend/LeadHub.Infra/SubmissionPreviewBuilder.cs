namespace LeadHub.Infra;

/// <summary>
/// Builds the short preview string shown in the submission list view, from whichever scalar
/// string fields the (schema-less) payload happens to contain.
/// </summary>
internal static class SubmissionPreviewBuilder
{
    private const int MaxLength = 140;

    public static string Build(IReadOnlyDictionary<string, object?> payload)
    {
        var parts = payload
            .Where(kv => kv.Value is string s && !string.IsNullOrWhiteSpace(s))
            .Take(3)
            .Select(kv => (string)kv.Value!);

        var preview = string.Join(" - ", parts);
        if (string.IsNullOrEmpty(preview))
            return "(no preview available)";

        return preview.Length > MaxLength ? preview[..MaxLength] + "…" : preview;
    }
}
