namespace LeadHub.Ports.Output;

public sealed record Form(
    string Id,
    string Name,
    string Slug,
    string? Description,
    bool NotificationsEnabled,
    string? NotificationEmail,
    string? ThankYouUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public sealed record FormSummaryProjection(
    string Id,
    string Name,
    string Slug,
    bool NotificationsEnabled,
    int SubmissionCount,
    DateTimeOffset? LastSubmissionAt
);
