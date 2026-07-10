# LeadHub API Contract

## Overview

This document defines the HTTP API for LeadHub MVP.

The API is derived from the application use case contract.

All request and response bodies use JSON.

All timestamps are returned in UTC using ISO 8601 format.

---

# Authentication

## Public Endpoints

Public submission endpoints do not require authentication.

## Admin Endpoints

All admin endpoints require authentication.

```http
Authorization: Bearer <token>
```

---

# Response Format

## Success

```json
{
  "success": true,
  "data": {}
}
```

## Failure

```json
{
  "success": false,
  "error": {
    "code": "FORM_NOT_FOUND",
    "message": "Form not found."
  }
}
```

---

# Error Codes

```text
FORM_NOT_FOUND
SUBMISSION_NOT_FOUND
SLUG_ALREADY_EXISTS
INVALID_FORM_CONFIGURATION
INVALID_SUBMISSION_PAYLOAD
RATE_LIMIT_EXCEEDED
NOTIFICATION_FAILED
UNAUTHORIZED
UNEXPECTED_ERROR
```

---

# Public API

## Submit Form

Creates a new submission for a form.

```http
POST /api/forms/{formSlug}/submit
Content-Type: application/json
```

### Request Body

```json
{
  "name": "Pedro",
  "email": "pedro@example.com",
  "message": "Hello",
  "_honeypot": ""
}
```

Rules:

* Body must be a JSON object.
* Arbitrary fields are accepted.
* Fields prefixed with `_` are reserved.
* `_honeypot` is reserved for spam detection — a filled honeypot no longer rejects the request; it
  contributes to the submission's spam score instead (see [Spam Analysis](#spam-analysis)).

The response is always the same success shape regardless of eventual spam classification — spam
analysis happens asynchronously after the response is sent and never blocks or changes it.

### Success Response

```http
200 OK
```

```json
{
  "success": true,
  "data": {
    "accepted": true,
    "redirectUrl": "https://example.com/thank-you"
  }
}
```

### Error Responses

```http
404 Not Found
```

```json
{
  "success": false,
  "error": {
    "code": "FORM_NOT_FOUND",
    "message": "Form not found."
  }
}
```

```http
400 Bad Request
```

```json
{
  "success": false,
  "error": {
    "code": "INVALID_SUBMISSION_PAYLOAD",
    "message": "Invalid submission payload."
  }
}
```

```http
429 Too Many Requests
```

```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many requests."
  }
}
```

---

# Admin API

## List Forms

Returns all forms.

```http
GET /api/admin/forms
```

### Success Response

```json
{
  "success": true,
  "data": [
    {
      "id": "01K1ABCDEF123456789",
      "name": "Contact Form",
      "slug": "contact",
      "notificationsEnabled": true,
      "submissionCount": 42,
      "lastSubmissionAt": "2026-07-07T08:00:00Z"
    }
  ]
}
```

---

## Get Form

Returns one form.

```http
GET /api/admin/forms/{formId}
```

### Success Response

```json
{
  "success": true,
  "data": {
    "id": "01K1ABCDEF123456789",
    "name": "Contact Form",
    "slug": "contact",
    "description": "Main website contact form",
    "notificationsEnabled": true,
    "notificationEmail": "owner@example.com",
    "thankYouUrl": "https://example.com/thank-you",
    "createdAt": "2026-07-01T10:00:00Z",
    "updatedAt": "2026-07-07T08:00:00Z"
  }
}
```

### Error Response

```http
404 Not Found
```

```json
{
  "success": false,
  "error": {
    "code": "FORM_NOT_FOUND",
    "message": "Form not found."
  }
}
```

---

## Create Form

Creates a new form.

```http
POST /api/admin/forms
Content-Type: application/json
```

### Request Body

```json
{
  "name": "Contact Form",
  "slug": "contact",
  "description": "Main website contact form",
  "notificationsEnabled": true,
  "notificationEmail": "owner@example.com",
  "thankYouUrl": "https://example.com/thank-you"
}
```

### Success Response

```http
201 Created
```

```json
{
  "success": true,
  "data": {
    "id": "01K1ABCDEF123456789",
    "name": "Contact Form",
    "slug": "contact",
    "description": "Main website contact form",
    "notificationsEnabled": true,
    "notificationEmail": "owner@example.com",
    "thankYouUrl": "https://example.com/thank-you",
    "createdAt": "2026-07-07T08:00:00Z",
    "updatedAt": null
  }
}
```

### Error Responses

```http
409 Conflict
```

```json
{
  "success": false,
  "error": {
    "code": "SLUG_ALREADY_EXISTS",
    "message": "Slug already exists."
  }
}
```

```http
400 Bad Request
```

```json
{
  "success": false,
  "error": {
    "code": "INVALID_FORM_CONFIGURATION",
    "message": "Invalid form configuration."
  }
}
```

---

## Update Form

Updates an existing form.

```http
PUT /api/admin/forms/{formId}
Content-Type: application/json
```

### Request Body

```json
{
  "name": "Contact Form",
  "slug": "contact",
  "description": "Updated description",
  "notificationsEnabled": true,
  "notificationEmail": "owner@example.com",
  "thankYouUrl": "https://example.com/thank-you"
}
```

### Success Response

```json
{
  "success": true,
  "data": {
    "id": "01K1ABCDEF123456789",
    "name": "Contact Form",
    "slug": "contact",
    "description": "Updated description",
    "notificationsEnabled": true,
    "notificationEmail": "owner@example.com",
    "thankYouUrl": "https://example.com/thank-you",
    "createdAt": "2026-07-01T10:00:00Z",
    "updatedAt": "2026-07-07T08:00:00Z"
  }
}
```

### Error Responses

```http
404 Not Found
```

```json
{
  "success": false,
  "error": {
    "code": "FORM_NOT_FOUND",
    "message": "Form not found."
  }
}
```

```http
409 Conflict
```

```json
{
  "success": false,
  "error": {
    "code": "SLUG_ALREADY_EXISTS",
    "message": "Slug already exists."
  }
}
```

```http
400 Bad Request
```

```json
{
  "success": false,
  "error": {
    "code": "INVALID_FORM_CONFIGURATION",
    "message": "Invalid form configuration."
  }
}
```

---

## Delete Form

Deletes an existing form.

```http
DELETE /api/admin/forms/{formId}
```

### Success Response

```json
{
  "success": true,
  "data": null
}
```

### Error Response

```http
404 Not Found
```

```json
{
  "success": false,
  "error": {
    "code": "FORM_NOT_FOUND",
    "message": "Form not found."
  }
}
```

---

# Submissions API

## List Submissions

Returns paginated submissions for a form.

```http
GET /api/admin/forms/{formId}/submissions?page=1&pageSize=50&status=Spam
```

### Query Parameters

| Name     | Required | Default | Notes                                                        |
| -------- | -------: | ------: | ------------------------------------------------------------- |
| page     |       No |       1 |                                                                 |
| pageSize |       No |      50 |                                                                 |
| status   |       No |    none | `Ham` \| `SuspectedSpam` \| `Spam` \| `PendingReview`; omit for all |

### Success Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "01K1SUBMISSION123",
        "createdAt": "2026-07-07T09:00:00Z",
        "ipAddress": "1.2.3.4",
        "preview": "Pedro - pedro@example.com",
        "status": "Ham"
      }
    ],
    "page": 1,
    "pageSize": 50,
    "totalItems": 234
  }
}
```

### Error Response

```http
404 Not Found
```

```json
{
  "success": false,
  "error": {
    "code": "FORM_NOT_FOUND",
    "message": "Form not found."
  }
}
```

---

## Get Submission

Returns full submission details.

```http
GET /api/admin/submissions/{submissionId}
```

### Success Response

```json
{
  "success": true,
  "data": {
    "id": "01K1SUBMISSION123",
    "formId": "01K1ABCDEF123456789",
    "createdAt": "2026-07-07T09:00:00Z",
    "ipAddress": "1.2.3.4",
    "userAgent": "Mozilla/5.0",
    "payload": {
      "name": "Pedro",
      "email": "pedro@example.com",
      "message": "Hello"
    },
    "status": "Spam",
    "spamScore": 25,
    "spamReasons": ["message_contains_url", "suspicious_keyword", "message_too_long"]
  }
}
```

`payload` has reserved (`_`-prefixed) fields, such as `_honeypot`, stripped.

### Error Response

```http
404 Not Found
```

```json
{
  "success": false,
  "error": {
    "code": "SUBMISSION_NOT_FOUND",
    "message": "Submission not found."
  }
}
```

---

# Spam Analysis

Every submission is scored asynchronously, immediately after being stored, by a background job —
it never delays the submit response. The verdict updates the submission's `status`, `spamScore`,
and `spamReasons`.

## Status values

| Status          | Meaning                                                          |
| ---------------- | ---------------------------------------------------------------- |
| `PendingReview`  | Stored, not yet analyzed (analysis runs moments after submit).    |
| `Ham`            | Legitimate. Email notification (and future integrations) fire.   |
| `SuspectedSpam`  | Borderline score. Stored only; no notification sent.              |
| `Spam`           | High score (e.g. honeypot filled). Stored only; no notification. |

## Scoring rules

| Reason                     | Score | Trigger                                              |
| --------------------------- | ----: | ----------------------------------------------------- |
| `honeypot_filled`           |  +100 | Hidden `_honeypot` field has a value.                  |
| `message_contains_url`      |   +10 | Submitted text contains `http://`, `https://`, or `www.`. |
| `suspicious_keyword`        |   +10 | Submitted text contains SEO/scam/gambling vocabulary.  |
| `suspicious_name_pattern`   |    +5 | A name-like field looks machine-generated (run-on, no space). |
| `message_too_long`          |    +5 | A message-like field exceeds 500 characters.           |

## Classification thresholds (configurable)

```text
Score 0-9    -> Ham
Score 10-19  -> SuspectedSpam
Score >=20   -> Spam
```

New scoring rules can be added independently, without changing existing ones.

---

# HTTP Status Mapping

| Error Code                 | HTTP Status |
| -------------------------- | ----------: |
| FORM_NOT_FOUND             |         404 |
| SUBMISSION_NOT_FOUND       |         404 |
| SLUG_ALREADY_EXISTS        |         409 |
| INVALID_FORM_CONFIGURATION |         400 |
| INVALID_SUBMISSION_PAYLOAD |         400 |
| RATE_LIMIT_EXCEEDED        |         429 |
| NOTIFICATION_FAILED        |         500 |
| UNAUTHORIZED               |         401 |
| UNEXPECTED_ERROR           |         500 |

---

# Field Validation

## Create / Update Form

| Field                |    Required | Rules                                   |
| -------------------- | ----------: | --------------------------------------- |
| name                 |         Yes | Non-empty string                        |
| slug                 |         Yes | Non-empty string, unique                |
| description          |          No | Nullable string                         |
| notificationsEnabled |         Yes | Boolean                                 |
| notificationEmail    | Conditional | Required when notifications are enabled |
| thankYouUrl          |          No | Nullable valid URL                      |

## Submit Form

| Field     | Required | Rules                                                     |
| --------- | -------: | ---------------------------------------------------------- |
| body      |      Yes | Must be a JSON object                                     |
| _honeypot |       No | Should be empty; a value scores `+100` (see Spam Analysis) |

---

# Notes

The HTTP API is an adapter over the application contract.

Application use cases remain the source of truth:

```text
IAdminForms
IAdminSubmissions
IPublicForms
```

The API must not leak persistence concerns, ORM entities, database schemas, or infrastructure-specific details.
