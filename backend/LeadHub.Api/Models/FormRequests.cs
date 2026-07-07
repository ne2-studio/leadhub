namespace LeadHub.Api.Models;

public sealed record CreateFormRequest(
    string Name,
    string Slug,
    string? Description,
    bool NotificationsEnabled,
    string? NotificationEmail,
    string? ThankYouUrl
);

public sealed record UpdateFormRequest(
    string Name,
    string Slug,
    string? Description,
    bool NotificationsEnabled,
    string? NotificationEmail,
    string? ThankYouUrl
);
