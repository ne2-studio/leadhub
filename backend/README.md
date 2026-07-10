# LeadHub — Backend

Lightweight form backend for static websites: create forms, receive submissions at a public
per-form endpoint, browse them from an authenticated admin API, and optionally notify an owner by
email. ASP.NET Core (.NET 10) ports & adapters, PostgreSQL via Dapper + FluentMigrator, JWT bearer
auth, and Serilog — scaffolded from and following the conventions in
[`docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md). The application contract is defined in
[`docs/CONTRACT.md`](../docs/CONTRACT.md); the HTTP surface in [`docs/API.md`](../docs/API.md).

Notable pieces:

- `Result<T, Error>` / `UnitResult<Error>` (CSharpFunctionalExtensions) for every expected failure
  path — typed error codes, no exceptions for control flow.
- An output port per external effect (`IClock`, `IIdGenerator`, `IFormRepository`,
  `ISubmissionRepository`, `IEmailNotificationSender`, `ISpamAnalyzer`, `ISubmissionAnalysisQueue`,
  `IRateLimiter`).
- A caching **decorator** (`CachedFormRepository`) composed at the DI root, registered `Singleton`
  as a deliberate exception to the default `Scoped` lifetime — `GetBySlug` is hit on every public
  submission.
- The **Null Object pattern** for feature-flagged behavior (`IEmailNotificationSender` swaps between
  `ResendEmailNotificationSender` and `NullEmailNotificationSender` based on
  `Features:EmailNotifications:Enabled`, decided once in `ServiceRegistration`).
- Asynchronous spam analysis: the public submit use case stores every submission as
  `PendingReview` and enqueues it (`ISubmissionAnalysisQueue`, an in-process channel) for a
  background worker (`SubmissionAnalysisBackgroundService`) to score deterministically via a set of
  independent `ISpamRule`s (honeypot filled, URL/keyword/name-pattern/length heuristics) and
  classify into `Ham` / `SuspectedSpam` / `Spam`. Only `Ham` triggers the email notification.
  Failures retry with backoff. An in-memory, per-form/per-IP rate limiter (`InMemoryRateLimiter`)
  still runs synchronously in the submit path, on top of the infra-level ASP.NET Core rate limiter
  applied to the public route.
- Two-tier testing: hand-written fakes for core/application logic (`LeadHub.Tests`), NSubstitute
  mocks for the infra decorator and plain unit tests for the other infra adapters
  (`LeadHub.Infra.Tests`).

## Architecture

```
LeadHub        — domain core (use cases, ports)
LeadHub.Infra  — adapters (PostgreSQL, caching decorator, Resend, spam/rate-limit)
LeadHub.Api    — HTTP entry point (controllers, JWT validation)
```

## API endpoints

Admin endpoints require a valid JWT (`Authorization: Bearer <token>`); the public submit endpoint
does not. Full request/response shapes are documented in [`docs/API.md`](../docs/API.md).

| Method   | Path                                        | Auth      | Description                    |
|----------|---------------------------------------------|-----------|--------------------------------|
| `GET`    | `/api/admin/forms`                          | Required  | List forms                     |
| `POST`   | `/api/admin/forms`                          | Required  | Create a form                  |
| `GET`    | `/api/admin/forms/{formId}`                 | Required  | Get a form                     |
| `PUT`    | `/api/admin/forms/{formId}`                 | Required  | Update a form                  |
| `DELETE` | `/api/admin/forms/{formId}`                 | Required  | Delete a form                  |
| `GET`    | `/api/admin/forms/{formId}/submissions`     | Required  | List a form's submissions      |
| `GET`    | `/api/admin/submissions/{submissionId}`     | Required  | Get submission details         |
| `POST`   | `/api/forms/{formSlug}/submit`              | Public    | Submit form data (rate-limited)|
| `GET`    | `/health`                                   | Public    | Liveness check (rate-limited)  |

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)
- [Docker](https://www.docker.com/products/docker-desktop)

### Run PostgreSQL locally

```bash
docker run --name pg-leadhub \
  -e POSTGRES_USER=devuser \
  -e POSTGRES_PASSWORD=devpass \
  -e POSTGRES_DB=leadhub \
  -p 5432:5432 \
  -d postgres:16
```

To stop and remove the container:

```bash
docker stop pg-leadhub && docker rm pg-leadhub
```

### Run the API

```bash
dotnet run --project LeadHub.Api
```

The API will be available at http://localhost:5050. Migrations run automatically at startup.

### Email notifications (optional)

Notifications are off by default. To send real emails via [Resend](https://resend.com):

```json
{
  "Features": { "EmailNotifications": { "Enabled": true } },
  "Resend": { "ApiKey": "re_...", "FromAddress": "LeadHub <you@yourdomain.com>" }
}
```

Set these via `appsettings.Development.json`, user secrets, or environment variables
(`Features__EmailNotifications__Enabled`, `Resend__ApiKey`). With the flag off, submissions are
still stored — only the outbound API call is skipped (Null Object pattern).

### Run tests

```bash
dotnet test
```

## Docker

Build the image:

```bash
docker build . -t leadhub-api
```

Run the container:

```bash
docker run --name leadhub-api \
  -e "ConnectionStrings__DefaultConnection=Host=host.docker.internal;Port=5432;Database=leadhub;Username=devuser;Password=devpass" \
  -p 5050:8080 \
  leadhub-api
```

## License

MIT © [Exeal](https://www.exeal.com)
