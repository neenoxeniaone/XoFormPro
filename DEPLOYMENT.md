# Deploying XOFormPro (single app on IIS, same host)

The ASP.NET API **also serves the React app**, so you deploy **one** IIS application at
`/XoFormPro`. Same origin → no reverse proxy, no CORS config, no absolute API URL.
PostgreSQL runs on the same host.

```
https://apps.hostack.com/XoFormPro/            -> React app (index.html)
https://apps.hostack.com/XoFormPro/assets/...  -> React JS/CSS (wwwroot/assets)
https://apps.hostack.com/XoFormPro/api/...     -> dashboard API
https://apps.hostack.com/XoFormPro/submit      -> public form endpoint
https://apps.hostack.com/XoFormPro/v1/...      -> public REST API
https://apps.hostack.com/XoFormPro/form-config -> per-key config read by forms.js
https://apps.hostack.com/XoFormPro/form-token  -> signed form tokens for forms.js
```

> If anything sits in front of the app (a reverse proxy, a dev server, a CDN), **every**
> path above needs forwarding to the backend. A missing rule usually returns the SPA's
> `index.html` with a 200 rather than a 404, so the symptom is a feature silently not
> working rather than an error. `forms.js` logs a `[XoFormPro]` console warning when this
> happens.

> Note: the visible brand is **XOFormPro**, but the code project / namespace stay
> `FormSubmissionPro`. Only the deploy **folder/subpath** is `XoFormPro` (set via VITE_BASE_PATH).

## Prerequisites on the host
- **.NET 9 Hosting Bundle** installed (ASP.NET Core Module for IIS).
- **PostgreSQL** running locally, with a `formsubmissionpro` database + user.

## 1. Configure production settings (environment variables on the host)
Set these as environment variables for the app. Use `__` (double underscore) for nesting:

```
ConnectionStrings__Postgres = Host=localhost;Port=5432;Database=formsubmissionpro;Username=postgres;Password=YOUR_PASSWORD
Jwt__SigningKey             = <a long, stable random secret>
App__BaseUrl                = https://apps.hostack.com/XoFormPro
```

Notes:
- Env vars override `appsettings.json`, so no `appsettings.Production.json` is needed.
- `ASPNETCORE_ENVIRONMENT` defaults to `Production` for a published app (fine to leave as-is).
- `Email:*` (Graph creds) lives in the DB (`fsp_email_configurations`) — set it there as before.

## 2. Build + publish (one command)
From the repo root:
```powershell
./publish.ps1
```
This builds the React app (with `VITE_BASE_PATH=/XoFormPro`), copies it into the API's
`wwwroot`, and runs `dotnet publish` into `./publish`.

## 3. Deploy to IIS
- Copy the contents of `./publish` to the host folder served at `/XoFormPro`.
- In IIS, make that folder an **Application** (not just a virtual directory), on an app pool
  with **"No Managed Code"** (ASP.NET Core runs via ANCM).
- Under a virtual application, IIS sets the path base to `/XoFormPro` automatically —
  the app's routes and static files resolve correctly.

The app **applies EF migrations on startup**, so the database schema is created/updated
automatically on first run.

## Notes
- **Uploads** are written to `wwwroot/uploads` — ensure the app pool identity has write access,
  and that the deploy doesn't wipe that folder.
- **CORS** stays open so external sites can POST to `/submit`.
- For **non-IIS hosting** (Kestrel/nginx), set `App:PathBase=/XoFormPro` so routing
  and static files use the subpath.
