namespace LeadHub.Ports.Output;

public sealed record Submission(
    string Id,
    string FormId,
    DateTimeOffset CreatedAt,
    string? IpAddress,
    string? UserAgent,
    IReadOnlyDictionary<string, object?> Payload
);

public sealed record SubmissionSummaryProjection(
    string Id,
    DateTimeOffset CreatedAt,
    string? IpAddress,
    string Preview
);
