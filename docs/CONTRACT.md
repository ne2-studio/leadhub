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
SubmissionNotFound
SlugAlreadyExists
InvalidFormConfiguration
InvalidSubmissionPayload
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
3. Check rate limit.
4. Store submission with `Status = PendingReview`.
5. Enqueue an asynchronous spam analysis job (`ISubmissionAnalysisQueue`).
6. Return configured thank-you URL.

Rules:

* Payload must be a JSON object.
* Fields are dynamic.
* Fields starting with `_` are reserved (e.g. `_honeypot`) and are stored as-is so the async spam
  analysis job can inspect them — they are stripped only at display boundaries (admin UI, email
  notifications), never at persistence.
* Spam classification never blocks or delays the submit response — the visitor always gets the
  normal success flow. Email notification and any future webhook/integration triggers happen
  later, from the analysis job, only for a `Ham` verdict. See [Submission Analysis](#submission-analysis).

Possible errors:

```csharp
FormNotFound
InvalidSubmissionPayload
RateLimitExceeded
UnexpectedError
```

---

## List Submissions

```csharp
public sealed record ListSubmissionsInput(
    string FormId,
    int Page,
    int PageSize,
    SubmissionStatus? Status
);
```

`Status` is an optional filter (`Ham` / `SuspectedSpam` / `Spam` / `PendingReview`); `null` returns submissions of every status.

```csharp
public sealed record SubmissionSummary(
    string Id,
    DateTimeOffset CreatedAt,
    string? IpAddress,
    string Preview,
    SubmissionStatus Status
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
    IReadOnlyDictionary<string, object?> Payload,
    SubmissionStatus Status,
    int SpamScore,
    IReadOnlyList<string> SpamReasons
);
```

`Payload` has reserved (`_`-prefixed) fields stripped before being returned to the admin UI.

Returns:

```csharp
Result<SubmissionDetails>
```

Possible errors:

```csharp
SubmissionNotFound
```

---

## Submission Analysis

The background job that scores a pending submission for spam. Invoked by the queue worker, not by
a controller.

```csharp
public interface ISubmissionAnalysis
{
    Task<Result<Unit>> AnalyzeSubmission(AnalyzeSubmissionInput input);
}
```

```csharp
public sealed record AnalyzeSubmissionInput(string SubmissionId);
```

Behavior:

1. Load the submission.
2. Run `ISpamAnalyzer.Analyze` to produce a `SpamVerdict`.
3. Persist the verdict via `ISubmissionRepository.UpdateAnalysis`.
4. If the verdict's status is `Ham` and the form has notifications enabled, send the email
   notification (payload with reserved fields stripped).

Possible errors:

```csharp
SubmissionNotFound
NotificationFailed
```

A failure result (including an unhandled exception) tells the queue worker to retry. Retrying is
safe: analysis is deterministic, and re-recording the same verdict is idempotent.

```csharp
public enum SubmissionStatus
{
    PendingReview,
    Ham,
    SuspectedSpam,
    Spam
}
```

```csharp
public sealed record SpamVerdict(
    int Score,
    SubmissionStatus Status,
    IReadOnlyList<string> Reasons
);
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
        int pageSize,
        SubmissionStatus? status
    );

    Task<int> CountByForm(string formId);

    Task<DateTimeOffset?> GetLastSubmissionDate(string formId);

    Task UpdateAnalysis(string submissionId, SpamVerdict verdict);
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

## ISpamAnalyzer

Scores a submission for spam signals and classifies it. Deterministic: the same submission always
produces the same verdict.

```csharp
public interface ISpamAnalyzer
{
    SpamVerdict Analyze(Submission submission);
}
```

The default implementation (`RuleBasedSpamAnalyzer`) sums the score contributed by every
registered `ISpamRule` (honeypot filled, message contains a URL, suspicious keyword, suspicious
name pattern, message too long) and classifies the total against configurable thresholds
(`SuspectedSpamThreshold`, `SpamThreshold`). New rules are added without modifying existing ones.

---

## ISubmissionAnalysisQueue

Hands a freshly stored submission off for asynchronous spam analysis.

```csharp
public interface ISubmissionAnalysisQueue
{
    ValueTask Enqueue(string submissionId, CancellationToken cancellationToken = default);
}
```

Enqueueing must not block or fail the submit request. The default implementation is an in-process
FIFO queue drained by a background worker that retries a failed analysis attempt (with backoff) up
to a configured limit before giving up and logging.

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
        -> IRateLimiter.EnsureAllowed
        -> ISubmissionRepository.Save (Status = PendingReview)
        -> ISubmissionAnalysisQueue.Enqueue
        -> SubmitFormOutput
```

The visitor always receives the normal success flow regardless of eventual spam classification —
analysis happens afterward, out of the request path.

## Submission Analysis Flow

```text
Queue worker
    -> ISubmissionAnalysis.AnalyzeSubmission
        -> ISubmissionRepository.GetById
        -> ISpamAnalyzer.Analyze
        -> ISubmissionRepository.UpdateAnalysis
        -> IEmailNotificationSender.SendSubmissionNotification   (only if Status == Ham)
```

Important rule:

```text
Verdict persistence is the source of truth.
Email notification is a side effect.
```

If notification fails after the verdict has been stored, the use case returns `NotificationFailed`
and the queue worker retries the whole job; the verdict remains persisted either way, and
re-analysis is safe because scoring is deterministic.

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
