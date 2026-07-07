namespace LeadHub.Ports.Input;

public sealed record CreateFormInput(
    string Name,
    string Slug,
    string? Description,
    bool NotificationsEnabled,
    string? NotificationEmail,
    string? ThankYouUrl
);

public sealed record UpdateFormInput(
    string FormId,
    string Name,
    string Slug,
    string? Description,
    bool NotificationsEnabled,
    string? NotificationEmail,
    string? ThankYouUrl
);

public sealed record DeleteFormInput(string FormId);

public sealed record GetFormInput(string FormId);

public sealed record FormDetails(
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

public sealed record FormSummary(
    string Id,
    string Name,
    string Slug,
    bool NotificationsEnabled,
    int SubmissionCount,
    DateTimeOffset? LastSubmissionAt
);
