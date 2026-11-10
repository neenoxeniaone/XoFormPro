# FormSubmissionPro

A backend-less **form-to-email** SaaS (a Web3Forms-style product). Point any HTML form at
the submit endpoint with a public **access key** and submissions arrive by email — no
server code on the user's side.

## Stack

| Layer | Tech |
|-------|------|
| Frontend (dashboard) | Vite + React + TypeScript + Tailwind |
| API | ASP.NET Core 9 Web API (C#) |
| Database | PostgreSQL via EF Core (Npgsql) |

Multi-tenant from day one. **Auth and billing are deferred** — the tenant is selected via
the `X-Org-Id` header (a stub created on the onboarding screen), to be replaced by real
auth without changing callers.

## Prerequisites

- .NET 9 SDK
- Node 20+
- PostgreSQL running locally (connection string in `api/FormSubmissionPro.Api/appsettings.json`)

## Run

**1. API** (creates/migrates the database automatically on first run):

```bash
cd api/FormSubmissionPro.Api
dotnet run --launch-profile http      # http://localhost:5074
```

**2. Dashboard:**

```bash
cd web
npm install
npm run dev                            # http://localhost:5173
```

Open http://localhost:5173, create a workspace, create a form, and copy the HTML snippet.
Submission emails are printed to the **API console** (dev email sink — no SMTP needed).

## Submit API

```
POST /submit                 # access_key in the body
POST /submit/{accessKey}     # access_key in the URL
```

Accepts `application/x-www-form-urlencoded`, `multipart/form-data`, or `application/json`.

**Reserved fields** (everything else is forwarded to the email verbatim):
`access_key`, `subject`, `from_name`, `email` (→ reply-to), `replyto`, `redirect`,
`botcheck` (honeypot), `ccemail`.

Example:

```bash
curl -X POST http://localhost:5074/submit \
  -H "Content-Type: application/json" \
  -d '{"access_key":"<KEY>","name":"Jane","email":"jane@x.com","message":"Hi"}'
```

## Project layout

```
api/FormSubmissionPro.Api/
├─ Domain/           Organization, Form, Submission
├─ Data/             AppDbContext + EF migrations
├─ Features/         Submit, Forms, Submissions, Organizations
├─ Services/Email/   IEmailSender → ConsoleEmailSender (swap to Resend/SMTP for prod)
└─ Infrastructure/   TenantContext (X-Org-Id auth stub)
web/src/
├─ api.ts            API client
├─ pages/            Onboarding, Forms, FormDetail, Submissions
└─ components/       UI primitives, CopyBlock
```

## Status

**Done & verified:** access keys, submit (JSON + form-encoded), honeypot spam capture,
email rendering, forms CRUD, submissions list with pagination/spam filter, dashboard UI.

**Next (Pro features):** hCaptcha/Turnstile verification, file uploads, webhooks &
integrations (Slack/Discord/Sheets/Zapier), autoresponder, then real auth + billing.
