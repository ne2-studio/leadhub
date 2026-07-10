using LeadHub.Ports.Output;

namespace LeadHub.Ports.Input;

public sealed record ListSubmissionsInput(string FormId, int Page, int PageSize, SubmissionStatus? Status);

public sealed record SubmissionSummary(
    string Id,
    DateTimeOffset CreatedAt,
    string? IpAddress,
    string Preview,
    SubmissionStatus Status
);

public sealed record GetSubmissionInput(string SubmissionId);

public sealed record SubmissionDetails(
    string Id,
    string FormId,
    DateTimeOffset CreatedAt,
    string? IpAddress,
    string? UserAgent,
    IReadOnlyDictionary<string, object?> Payload,
    SubmissionStatus Status,
    int SpamScore,
    IReadOnlyList<string> SpamReasons
);
