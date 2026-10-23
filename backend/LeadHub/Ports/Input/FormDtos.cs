namespace LeadHub.Ports.Input;

public sealed record CreateFormInput(
    string OwnerId,
    string Name,
    string Slug,
    string? Description,
    bool NotificationsEnabled,
    string? NotificationEmail,
    string? ThankYouUrl
);

public sealed record UpdateFormInput(
    string OwnerId,
    string FormId,
    string Name,
    string Slug,
    string? Description,
    bool NotificationsEnabled,
    string? NotificationEmail,
    string? ThankYouUrl
);

public sealed record DeleteFormInput(string OwnerId, string FormId);

public sealed record GetFormInput(string OwnerId, string FormId);

public sealed record ListFormsInput(string OwnerId);

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
