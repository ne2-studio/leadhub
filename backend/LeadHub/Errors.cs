namespace LeadHub;

public enum ErrorCode
{
    FormNotFound,
    SubmissionNotFound,
    SlugAlreadyExists,
    InvalidFormConfiguration,
    InvalidSubmissionPayload,
    SpamDetected,
    RateLimitExceeded,
    NotificationFailed,
    Unauthorized,
    UnexpectedError
}

public sealed record Error(ErrorCode Code, string Message)
{
    public override string ToString() => Message;
}

public static class Errors
{
    public static Error FormNotFound(string formId) => new(ErrorCode.FormNotFound, $"Form '{formId}' not found.");
    public static Error SubmissionNotFound(string submissionId) => new(ErrorCode.SubmissionNotFound, $"Submission '{submissionId}' not found.");
    public static Error SlugAlreadyExists(string slug) => new(ErrorCode.SlugAlreadyExists, $"Slug '{slug}' is already in use.");
    public static Error InvalidFormConfiguration(string message) => new(ErrorCode.InvalidFormConfiguration, message);
    public static Error InvalidSubmissionPayload(string message) => new(ErrorCode.InvalidSubmissionPayload, message);
    public static Error SpamDetected() => new(ErrorCode.SpamDetected, "Spam detected.");
    public static Error RateLimitExceeded() => new(ErrorCode.RateLimitExceeded, "Too many requests.");
    public static Error NotificationFailed(string message) => new(ErrorCode.NotificationFailed, message);
    public static Error Unauthorized() => new(ErrorCode.Unauthorized, "Unauthorized.");
    public static Error UnexpectedError(string message) => new(ErrorCode.UnexpectedError, message);
}
