namespace LeadHub.Ports.Input;

public sealed record ListSubmissionsInput(string FormId, int Page, int PageSize);

public sealed record SubmissionSummary(
    string Id,
    DateTimeOffset CreatedAt,
    string? IpAddress,
    string Preview
);

public sealed record GetSubmissionInput(string SubmissionId);

public sealed record SubmissionDetails(
    string Id,
    string FormId,
    DateTimeOffset CreatedAt,
    string? IpAddress,
    string? UserAgent,
    IReadOnlyDictionary<string, object?> Payload
);
