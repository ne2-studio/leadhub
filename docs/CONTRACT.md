# LeadHub Application Contract

## Error Handling

All use cases return:

```csharp
Result<T>
```

A `Result<T>` represents either:

* `Success(T value)`
* `Failure(Error error)`

Errors must be explicit and typed.

Example errors:

```csharp
FormNotFound
SlugAlreadyExists
InvalidFormConfiguration
InvalidSubmissionPayload
SpamDetected
RateLimitExceeded
NotificationFailed
Unauthorized
UnexpectedError
```

---

# Primary Ports

## IAdminForms

Use cases available to the authenticated administrator.

```csharp
public interface IAdminForms
{
    Task<Result<FormDetails>> CreateForm(CreateFormInput input);

    Task<Result<FormDetails>> UpdateForm(UpdateFormInput input);

    Task<Result<Unit>> DeleteForm(DeleteFormInput input);

    Task<Result<IReadOnlyList<FormSummary>>> ListForms();

    Task<Result<FormDetails>> GetForm(GetFormInput input);
}
```

---

## IAdminSubmissions

Use cases for browsing received submissions.

```csharp
public interface IAdminSubmissions
{
    Task<Result<PaginatedResult<SubmissionSummary>>> ListSubmissions(ListSubmissionsInput input);

    Task<Result<SubmissionDetails>> GetSubmission(GetSubmissionInput input);
}
```

---

## IPublicForms

Use cases available to external websites.

```csharp
public interface IPublicForms
{
    Task<Result<SubmitFormOutput>> SubmitForm(SubmitFormInput input);
}
```

---

# Use Case Contracts

## Create Form

```csharp
public sealed record CreateFormInput(
    string Name,
    string Slug,
    string? Description,
    bool NotificationsEnabled,
    string? NotificationEmail,
    string? ThankYouUrl
);
```

```csharp
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
```

Validation:

* `Name` is required.
* `Slug` is required and unique.
* `NotificationEmail` is required when notifications are enabled.
* `ThankYouUrl`, when present, must be a valid URL.

Possible errors:

```csharp
SlugAlreadyExists
InvalidFormConfiguration
```

---

## Update Form

```csharp
public sealed record UpdateFormInput(
    string FormId,
    string Name,
    string Slug,
    string? Description,
    bool NotificationsEnabled,
    string? NotificationEmail,
    string? ThankYouUrl
);
```

Returns:

```csharp
Result<FormDetails>
```

Possible errors:

```csharp
FormNotFound
SlugAlreadyExists
InvalidFormConfiguration
```

---

## Delete Form

```csharp
public sealed record DeleteFormInput(
    string FormId
);
```

Returns:

```csharp
Result<Unit>
```

Possible errors:

```csharp
FormNotFound
```

---

## List Forms

```csharp
public sealed record FormSummary(
    string Id,
    string Name,
    string Slug,
    bool NotificationsEnabled,
    int SubmissionCount,
    DateTimeOffset? LastSubmissionAt
);
```

Returns:

```csharp
Result<IReadOnlyList<FormSummary>>
```

---

## Get Form

```csharp
public sealed record GetFormInput(
    string FormId
);
```

Returns:

```csharp
Result<FormDetails>
```

Possible errors:

```csharp
FormNotFound
```

---

# Submissions

## Submit Form

```csharp
public sealed record SubmitFormInput(
    string FormSlug,
    IReadOnlyDictionary<string, object?> Payload,
    string? IpAddress,
    string? UserAgent
);
```

```csharp
public sealed record SubmitFormOutput(
    bool Accepted,
    string? RedirectUrl
);
```

Behavior:

1. Find form by slug.
2. Validate payload.
3. Check honeypot.
4. Check rate limit.
5. Store submission.
6. Send notification email if enabled.
7. Return configured thank-you URL.

Rules:

* Payload must be a JSON object.
* Fields are dynamic.
* Fields starting with `_` are reserved.
* `_honeypot` is reserved for spam protection.
* If `_honeypot` has a value, reject the submission.
* Rejected spam submissions are not stored.
* Failed email notification must not delete the stored submission.

Possible errors:

```csharp
FormNotFound
InvalidSubmissionPayload
SpamDetected
RateLimitExceeded
NotificationFailed
UnexpectedError
```

---

## List Submissions

```csharp
public sealed record ListSubmissionsInput(
    string FormId,
    int Page,
    int PageSize
);
```

```csharp
public sealed record SubmissionSummary(
    string Id,
    DateTimeOffset CreatedAt,
    string? IpAddress,
    string Preview
);
```

```csharp
public sealed record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems
);
```

Returns:

```csharp
Result<PaginatedResult<SubmissionSummary>>
```

Possible errors:

```csharp
FormNotFound
```

---

## Get Submission

```csharp
public sealed record GetSubmissionInput(
    string SubmissionId
);
```

```csharp
public sealed record SubmissionDetails(
    string Id,
    string FormId,
    DateTimeOffset CreatedAt,
    string? IpAddress,
    string? UserAgent,
    IReadOnlyDictionary<string, object?> Payload
);
```

Returns:

```csharp
Result<SubmissionDetails>
```

Possible errors:

```csharp
SubmissionNotFound
```

---

# Secondary Ports

These are the capabilities required by the application layer.

## IFormRepository

Persistence for forms.

```csharp
public interface IFormRepository
{
    Task<Form?> GetById(string id);

    Task<Form?> GetBySlug(string slug);

    Task<bool> ExistsBySlug(string slug, string? excludingFormId = null);

    Task<IReadOnlyList<FormSummaryProjection>> List();

    Task Save(Form form);

    Task Delete(Form form);
}
```

---

## ISubmissionRepository

Persistence for submissions.

```csharp
public interface ISubmissionRepository
{
    Task Save(Submission submission);

    Task<Submission?> GetById(string id);

    Task<PaginatedResult<SubmissionSummaryProjection>> ListByForm(
        string formId,
        int page,
        int pageSize
    );

    Task<int> CountByForm(string formId);

    Task<DateTimeOffset?> GetLastSubmissionDate(string formId);
}
```

---

## IEmailNotificationSender

Sends notification emails.

Initial implementation:

* Resend

```csharp
public interface IEmailNotificationSender
{
    Task<Result<Unit>> SendSubmissionNotification(SendSubmissionNotificationInput input);
}
```

```csharp
public sealed record SendSubmissionNotificationInput(
    string To,
    string FormName,
    DateTimeOffset SubmittedAt,
    IReadOnlyDictionary<string, object?> Payload,
    string? IpAddress,
    string? UserAgent
);
```

---

## ISpamProtection

Detects spam submissions.

```csharp
public interface ISpamProtection
{
    Task<Result<Unit>> EnsureNotSpam(SpamCheckInput input);
}
```

```csharp
public sealed record SpamCheckInput(
    string FormSlug,
    IReadOnlyDictionary<string, object?> Payload,
    string? IpAddress,
    string? UserAgent
);
```

Possible errors:

```csharp
SpamDetected
```

---

## IRateLimiter

Protects public submission endpoints.

```csharp
public interface IRateLimiter
{
    Task<Result<Unit>> EnsureAllowed(RateLimitInput input);
}
```

```csharp
public sealed record RateLimitInput(
    string FormSlug,
    string? IpAddress
);
```

Possible errors:

```csharp
RateLimitExceeded
```

---

## IClock

Provides current time.

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

---

## IIdGenerator

Generates IDs.

```csharp
public interface IIdGenerator
{
    string NewId();
}
```

---

# Application Flow

## Submit Form Flow

```text
External website
    -> IPublicForms.SubmitForm
        -> IFormRepository.GetBySlug
        -> ISpamProtection.EnsureNotSpam
        -> IRateLimiter.EnsureAllowed
        -> ISubmissionRepository.Save
        -> IEmailNotificationSender.SendSubmissionNotification
        -> SubmitFormOutput
```

Important rule:

```text
Submission persistence is the source of truth.
Email notification is a side effect.
```

If notification fails after the submission has been stored, the use case may return `NotificationFailed`, but the submission must remain persisted.

---

# Out of Scope

The application contract does not define:

* HTTP routes
* Database schema
* ORM mappings
* Authentication implementation
* UI components
* Deployment
* Infrastructure
* Framework choices
