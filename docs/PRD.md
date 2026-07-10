# LeadHub — Product Requirements Document (PRD)

## Overview

LeadHub is a lightweight form backend designed for static websites.

It allows website owners to create forms, receive submissions, store them centrally, and optionally receive email notifications when a new submission arrives.

The primary use case is supporting multiple static websites (such as Exeal, Sibelclan, Zenkai, Gocres, and future projects) without relying on third-party form providers.

The product should prioritize simplicity, low maintenance, and fast setup over advanced marketing or CRM functionality.

---

# Goals

LeadHub must allow an authenticated administrator to:

* Create and manage forms.
* Receive submissions from external websites.
* Store all submissions.
* Browse and inspect submissions.
* Configure email notifications.
* Configure a thank-you URL for each form.

The system should work with any static website capable of making HTTP requests.

---

# User Roles

## Administrator

The administrator is the owner of the system.

Capabilities:

* Create forms.
* Edit forms.
* Delete forms.
* Browse submissions.
* Configure notifications.
* Configure thank-you URLs.

The MVP assumes a single administrator account.

No multi-user support is required.

---

# Core Concepts

## Form

A form represents a public endpoint capable of receiving submissions.

Examples:

* Contact form
* Newsletter signup
* Quote request
* Event registration
* Lead capture form

Every form is independent.

---

## Submission

A submission represents a single successful form submission.

Submissions store:

* Submitted data
* Timestamp
* IP address
* User-Agent
* Spam status, score, and reasons (see [Spam Protection](#spam-protection))

---

# Functional Requirements

## Form Management

Administrators must be able to:

* Create forms.
* Edit forms.
* Delete forms.
* View all forms.

### Form Configuration

Each form contains the following settings:

#### Basic Information

* Name
* Optional description

#### Email Notifications

* Enabled / Disabled
* Destination email address

#### Thank You URL

A URL where users should be redirected after a successful submission.

Examples:

* https://exeal.com/gracias
* https://sibelclan.com/thank-you

---

## Public Submission Endpoint

Each form exposes a public endpoint.

External websites submit data to this endpoint.

The endpoint must support arbitrary field structures.

Examples:

### Contact Form

```json
{
  "name": "Pedro",
  "email": "pedro@example.com",
  "message": "Hello"
}
```

### Newsletter Signup

```json
{
  "email": "pedro@example.com"
}
```

### Quote Request

```json
{
  "name": "Pedro",
  "company": "Exeal",
  "budget": "5000"
}
```

The system must not require predefined field schemas.

Fields should be accepted dynamically.

---

## Submission Storage

Every valid submission must be stored.

The system must preserve:

* Submission timestamp
* Submitted fields
* IP address
* User-Agent

No submission data should be lost after successful processing.

---

## Submission Browsing

Administrators must be able to inspect submissions.

### Form List View

Display:

* Form name
* Total submissions
* Last submission date

### Submission List View

Display:

* Submission date
* IP address
* Short preview of submitted data
* Spam status

Supports filtering by status: Ham, SuspectedSpam, Spam, PendingReview.

### Submission Detail View

Display:

* Complete submitted payload
* Timestamp
* IP address
* User-Agent
* Spam status, score, and reasons

---

## Email Notifications

LeadHub can notify administrators when a new submission arrives.

Notifications are configured per form.

### Configuration

Per form:

* Enabled / Disabled
* Destination email address

### Email Provider

Initial provider:

* Resend

### Notification Content

Each notification must include:

* Form name
* Submission date
* Submitted fields
* IP address
* User-Agent

---

## Thank You Redirect

Each form can define a thank-you URL.

After a successful submission, clients should be able to redirect users to that URL.

The configured URL should be exposed as part of the successful submission response.

---

## Spam Protection

### Classification

Every submission is automatically classified, asynchronously and after being stored, as one of:

* `PendingReview` — stored, analysis not yet complete.
* `Ham` — legitimate. Triggers email notification.
* `SuspectedSpam` — borderline. Stored only, no notification.
* `Spam` — high-confidence spam. Stored only, no notification.

Classification never blocks or delays the submitter's response — the thank-you redirect always
fires immediately after the submission is stored.

### Scoring

A submission's spam score is the sum of independent rule hits:

* Honeypot field filled (`+100`).
* Submitted text contains a URL (`+10`).
* Submitted text contains a suspicious keyword — SEO/backlink/casino/crypto/etc. (`+10`).
* A name-like field looks machine-generated, e.g. `RobertSkect` (`+5`).
* A message-like field exceeds 500 characters (`+5`).

Thresholds (configurable): score 0-9 is Ham, 10-19 is SuspectedSpam, 20+ is Spam.

### Honeypot

The honeypot field (`_honeypot`) is still supported as a hidden field real visitors never fill in,
but a filled value now contributes to the spam score instead of rejecting the request outright —
every submission is stored and goes through the same classification pipeline.

### Rate Limiting

The system must protect against excessive requests from the same IP address.

Administrators do not need configuration options in the MVP.

Reasonable defaults are sufficient.

---

# Administration Panel

## Dashboard

The dashboard should provide a high-level overview.

Display:

* Total forms
* Total submissions
* Recent submissions

The goal is quick visibility into system activity.

---

## Forms Section

Display all forms.

Columns:

* Name
* Notification status
* Total submissions
* Last submission date

Actions:

* Create
* Edit
* Delete
* View submissions

---

## Submissions Section

Allow browsing and inspecting submissions.

Features:

* List submissions
* Open submission details
* Navigate submissions by form
* Filter by spam status (Ham / SuspectedSpam / Spam / PendingReview)

No search, tagging, or export functionality is required in the MVP.

---

# Non-Goals

The following features are explicitly out of scope for the MVP:

* Multi-user support
* Teams
* Workspaces
* Webhooks
* CSV export
* Lead magnets
* Autoresponders
* Marketing automations
* CRM functionality
* Cloudflare Turnstile
* Visual form builders
* Analytics dashboards
* Submission tagging
* Submission search
* Third-party integrations beyond Resend

---

# Product Principles

1. Simplicity over flexibility.
2. Static-site-first.
3. Minimal configuration.
4. Schema-less form submissions.
5. Fast setup for new websites.
6. Self-hosted by default.
7. Low operational overhead.
8. Clear path for future automation features.
