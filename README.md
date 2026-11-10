# XOFormPro

A backend-less **form-to-email** service (Web3Forms-style). Point any HTML form at the
submit endpoint with a public **access key** and submissions arrive by email and in the
dashboard — no server code on the customer's side.

Live: https://xoformpro.xeniaone.com · code/namespace name: `FormSubmissionPro`

## Stack

| Layer | Tech |
|-------|------|
| Dashboard | Vite + React 19 + TypeScript + Tailwind 4 (`web/`) |
| API | ASP.NET Core 9 (`api/FormSubmissionPro.Api/`) |
| Database | PostgreSQL via EF Core (snake_case, `fsp_` table prefix) |
| Embed script | `web/public/forms.js` — AJAX submit, tick box, captcha injection |

Multi-tenant: an **organization** (workspace) owns forms, lists and submissions. Users log in
with email + password (JWT bearer). Owners can invite team members.

## Features

- Forms **and** lists (newsletter / waitlist sign-ups) sharing one submit pipeline
- Email notification per submission (Microsoft Graph or Amazon SES; console in dev)
- Spam: honeypot, domain allow-lists, "I'm not a robot" tick box with signed single-use
  tokens, hCaptcha / Cloudflare Turnstile (auto-injected by `forms.js`)
- Submissions inbox: search, filters, star / read, bulk actions, CSV / JSON export
- Pro: file uploads, webhooks (durable outbox with retries; Slack/Discord formatting),
  autoresponder, REST API (`/v1`, API keys)
- Password reset, email verification, team invites, dark mode, mobile layout

## Run locally

Prerequisites: .NET 9 SDK, Node 20+, PostgreSQL.

**1. Database connection** (kept out of git):

```bash
cd api/FormSubmissionPro.Api
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=formsubmissionpro;Username=postgres;Password=..."
```

**2. API** — creates / migrates the database on startup:

```bash
dotnet run --launch-profile http      # http://localhost:5074
```

**3. Dashboard:**

```bash
cd web
npm install
npm run dev                            # http://localhost:5173 (proxies backend paths to :5074)
```

Register a workspace, create a form, copy its snippet. With no email provider configured,
emails are written to the API log.

## Submit API

```
POST /submit                 # access_key in the body
POST /submit/{accessKey}     # access_key in the URL
```

Accepts `application/x-www-form-urlencoded`, `multipart/form-data` or `application/json`.
Browser form posts get a redirect / hosted success page; AJAX callers get JSON.

**Reserved fields** (not shown in the email body): `access_key`, `subject`, `from_name`,
`replyto`, `redirect`, `botcheck` (honeypot), `ccemail`, captcha responses, `not_robot`,
`_xfp_token`. `email` is shown *and* used as Reply-To.

```bash
curl -X POST http://localhost:5074/submit \
  -H "Content-Type: application/json" \
  -d '{"access_key":"<KEY>","name":"Jane","email":"jane@x.com","message":"Hi"}'
```

Drop-in embed (AJAX submit, success message, spam checks configured from the dashboard):

```html
<script src="https://xoformpro.xeniaone.com/forms.js" async></script>
<form action="https://xoformpro.xeniaone.com/submit" method="POST">
  <input type="hidden" name="access_key" value="<KEY>">
  ...
</form>
```

## Project layout

```
api/FormSubmissionPro.Api/
├─ Domain/          Organization, User, Form, Submission, Webhook, ApiKey, tokens, ...
├─ Data/            AppDbContext
├─ Migrations/      EF Core migrations (applied on startup)
├─ Features/        Controllers: Submit, FormConfig, Forms, Submissions, Account, Auth,
│                   ApiKeys, Webhooks, V1 (public REST API)
├─ Services/        Auth, Email, Spam, Captcha, Files, Webhooks, Retention, Security
└─ Infrastructure/  TenantContext (org from JWT), GlobalExceptionHandler
web/
├─ public/forms.js  embed script served to customers' sites
└─ src/             api.ts client, pages/, components/
```

## Deploy

See [DEPLOYMENT.md](DEPLOYMENT.md). Short version: `./publish.ps1`, copy `./publish` to the
IIS site. Secrets come from a git-ignored `appsettings.Production.json` or environment variables.
