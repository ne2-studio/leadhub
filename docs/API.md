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
SPAM_DETECTED
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
* `_honeypot` is reserved for spam protection.

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

```http
400 Bad Request
```

```json
{
  "success": false,
  "error": {
    "code": "SPAM_DETECTED",
    "message": "Spam detected."
  }
}
```

```http
500 Internal Server Error
```

```json
{
  "success": false,
  "error": {
    "code": "NOTIFICATION_FAILED",
    "message": "Submission was stored but notification failed."
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
GET /api/admin/forms/{formId}/submissions?page=1&pageSize=50
```

### Query Parameters

| Name     | Required | Default |
| -------- | -------: | ------: |
| page     |       No |       1 |
| pageSize |       No |      50 |

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
        "preview": "Pedro - pedro@example.com"
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
    }
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
    "code": "SUBMISSION_NOT_FOUND",
    "message": "Submission not found."
  }
}
```

---

# HTTP Status Mapping

| Error Code                 | HTTP Status |
| -------------------------- | ----------: |
| FORM_NOT_FOUND             |         404 |
| SUBMISSION_NOT_FOUND       |         404 |
| SLUG_ALREADY_EXISTS        |         409 |
| INVALID_FORM_CONFIGURATION |         400 |
| INVALID_SUBMISSION_PAYLOAD |         400 |
| SPAM_DETECTED              |         400 |
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

| Field     | Required | Rules                      |
| --------- | -------: | -------------------------- |
| body      |      Yes | Must be a JSON object      |
| _honeypot |       No | Must be empty when present |

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
