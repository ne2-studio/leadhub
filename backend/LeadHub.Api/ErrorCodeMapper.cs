namespace LeadHub.Api;

public static class ErrorCodeMapper
{
    public static (int StatusCode, string Code) Map(ErrorCode code) => code switch
    {
        ErrorCode.FormNotFound => (StatusCodes.Status404NotFound, "FORM_NOT_FOUND"),
        ErrorCode.SubmissionNotFound => (StatusCodes.Status404NotFound, "SUBMISSION_NOT_FOUND"),
        ErrorCode.SlugAlreadyExists => (StatusCodes.Status409Conflict, "SLUG_ALREADY_EXISTS"),
        ErrorCode.InvalidFormConfiguration => (StatusCodes.Status400BadRequest, "INVALID_FORM_CONFIGURATION"),
        ErrorCode.InvalidSubmissionPayload => (StatusCodes.Status400BadRequest, "INVALID_SUBMISSION_PAYLOAD"),
        ErrorCode.RateLimitExceeded => (StatusCodes.Status429TooManyRequests, "RATE_LIMIT_EXCEEDED"),
        ErrorCode.NotificationFailed => (StatusCodes.Status500InternalServerError, "NOTIFICATION_FAILED"),
        ErrorCode.Unauthorized => (StatusCodes.Status401Unauthorized, "UNAUTHORIZED"),
        ErrorCode.UnexpectedError => (StatusCodes.Status500InternalServerError, "UNEXPECTED_ERROR"),
        _ => (StatusCodes.Status500InternalServerError, "UNEXPECTED_ERROR")
    };
}
