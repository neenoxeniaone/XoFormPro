# Deploying XOFormPro

Production runs on IIS at the **root** of `https://xoformpro.xeniaone.com`. The ASP.NET API
also serves the React app, so it is **one** IIS site: same origin, no CORS setup for the
dashboard, no absolute API URL in the frontend.

```
https://xoformpro.xeniaone.com/             -> React app (index.html)
https://xoformpro.xeniaone.com/assets/...   -> React JS/CSS (wwwroot/assets)
https://xoformpro.xeniaone.com/forms.js     -> embed script customers include
https://xoformpro.xeniaone.com/api/...      -> dashboard API (JWT)
https://xoformpro.xeniaone.com/submit       -> public form endpoint
https://xoformpro.xeniaone.com/v1/...       -> public REST API (API keys)
https://xoformpro.xeniaone.com/form-config  -> per-key config read by forms.js
https://xoformpro.xeniaone.com/form-token   -> signed form tokens for forms.js
https://xoformpro.xeniaone.com/uploads/...  -> uploaded files (always served as downloads)
https://xoformpro.xeniaone.com/health       -> health check (DB connectivity)
```

> If anything sits in front of the app (a reverse proxy, the Vite dev server, a CDN),
> **every** backend path above needs forwarding. A missing rule returns the SPA's
> `index.html` with a 200 rather than a 404, so a feature silently stops working.
> `forms.js` logs a `[XoFormPro]` console warning when this happens. When adding a
> backend path, update `web/vite.config.ts`, `web/public/web.config`, and this table.

> The visible brand is **XOFormPro**; the code project / namespace stay `FormSubmissionPro`.

## Prerequisites on the host
- **.NET 9 Hosting Bundle** (ASP.NET Core Module for IIS).
- **PostgreSQL** reachable from the host.

## 1. Secrets — never in git

The repository is public. `appsettings.json` and `web.config` contain **no** secrets.
Production values come from either of these (environment variables win if both are set):

**Option A — `appsettings.Production.json`** next to the published DLL (git-ignored;
`dotnet publish` copies it from `api/FormSubmissionPro.Api/` if it exists there):

```json
{
  "ConnectionStrings": { "Postgres": "Host=...;Port=5432;Database=...;Username=...;Password=..." },
  "Jwt": { "SigningKey": "<a long, stable random secret>" }
}
```

**Option B — environment variables** on the host / app pool (`__` = nesting):

```
ConnectionStrings__Postgres = Host=...;Port=5432;Database=...;Username=...;Password=...
Jwt__SigningKey             = <a long, stable random secret>
```

`App__BaseUrl` (used in password-reset / invite links) is set in the committed `web.config`.

The app **refuses to start** outside Development without `Jwt:SigningKey`. Changing the key
signs everyone out (existing tokens stop validating) and invalidates outstanding form tokens.

Email transport (Microsoft Graph or Amazon SES) is configured in the database, row `Id = 1`
of `fsp_email_configurations` — not in config files. With no credentials there, mail is
only written to the log.

Optional: `Retention__FreeDays` / `Retention__ProDays` enable automatic deletion of old
submissions. Both default to `0` = keep forever.

## 2. Build + publish (one command)
From the repo root:
```powershell
./publish.ps1
```
Builds the React app (served at root — `VITE_BASE_PATH` is empty in `web/.env.production`),
copies it into the API's `wwwroot`, and runs `dotnet publish` into `./publish`.

## 3. Deploy to IIS
- Copy the contents of `./publish` to the site folder. **Don't delete `wwwroot/uploads`**
  on the server — it holds customers' uploaded files.
- App pool: **"No Managed Code"** (ASP.NET Core runs in-process via ANCM).
- The app pool identity needs write access to `wwwroot/uploads` and `logs`.

EF migrations are **applied on startup**, so the schema updates on first request after deploy.

## Notes
- **CORS** is open so any site can POST to `/submit`. Dashboard auth is a bearer token
  (not a cookie), so this does not expose the dashboard API to cross-site requests.
- **Webhooks** cannot target localhost / private / link-local addresses, and redirects are
  not followed.
- To host under a sub-path instead of a domain root, set `VITE_BASE_PATH=/YourPath` in
  `web/.env.production`; under IIS as a virtual application the path base is picked up
  automatically, otherwise set `App:PathBase`.
