namespace LeadHub.Application;

/// <summary>
/// Fields prefixed with "_" (e.g. the honeypot trap) are internal bookkeeping submitted alongside
/// real form data. They're kept on the stored submission so spam analysis can see them, but must
/// never reach an administrator's inbox or the admin UI.
/// </summary>
internal static class ReservedFields
{
    public static IReadOnlyDictionary<string, object?> Strip(IReadOnlyDictionary<string, object?> payload) =>
        payload.Where(kv => !kv.Key.StartsWith('_')).ToDictionary(kv => kv.Key, kv => kv.Value);
}
