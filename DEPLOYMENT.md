# Deploying XOFormPro

Production runs on the **k3s server `13.135.138.127`** (AWS Lightsail, eu-west-2), in its own
namespace `xoformpro`, next to CompliVault (`cvs`) and the XeniaOne website (`website`).
Same pattern as the website: stock container images, app files uploaded to `/srv`, HTTPS
from the shared ingress-nginx + cert-manager (`letsencrypt-prod`). No image registry.

```
xoformpro.xeniaone.com ──► ingress-nginx (TLS) ──► Service app ──► Deployment app
                                                    (mcr.microsoft.com/dotnet/aspnet:9.0,
                                                     files from /srv/xoformpro/app)
                                                             │
                                                             ▼
                                                    StatefulSet postgres (postgres:17, 5Gi PVC)
```

All files are in [`deploy/`](deploy/). SSH key: `E:\Projects\XenieoneServerKey\LightsailDefaultKey-eu-west-2.pem`
(override with `KEY=...`; host with `HOST=...`). Run the scripts from **Git Bash** at the repo root.

| Script | What it does |
|--------|--------------|
| `bash deploy/deploy.sh` | Builds React + API, uploads to `/srv/xoformpro/app`, applies `k8s-xoformpro.yaml`, rolling-restarts the app. **The normal deploy command.** |
| `bash deploy/create-secrets.sh` | One-time: creates the `xoformpro-secrets` secret (DB password, connection string, JWT key). Never overwrites an existing one. |
| `bash deploy/db-tunnel.sh` | Opens a tunnel to the production DB on `localhost:15433` for pgAdmin / DBeaver / psql; prints the connection details. Ctrl+C closes it. |
| `bash deploy/migrate-db.sh` | **Done 2026-11-11 — do not use.** Copied the old IIS database into the cluster, **replacing** it. Now refuses to run unless `CONFIRM_OVERWRITE_LIVE=yes`. |

## What lives where on the server

| Path / object | Contents |
|---------------|----------|
| `/srv/xoformpro/app` | Published app (replaced on every deploy) |
| `/srv/xoformpro/uploads` | Visitor file uploads — **keep** |
| `/srv/xoformpro/keys` | ASP.NET Data Protection keys (decrypt stored secrets) — **keep** |
| `/srv/xoformpro/backups` | Nightly `pg_dump` (02:30 UTC, 14 days kept) + migration dumps |
| PVC `pgdata-postgres-0` | PostgreSQL data (`local-path`) |
| Secret `xoformpro-secrets` | `POSTGRES_PASSWORD`, `ConnectionStrings__Postgres`, `Jwt__SigningKey` |

Secrets are never in git. Changing `Jwt__SigningKey` signs everyone out.

## Day-to-day

```bash
bash deploy/deploy.sh                                   # deploy current code
ssh -i <key> ubuntu@13.135.138.127
kubectl -n xoformpro get pods                           # status
kubectl -n xoformpro logs deploy/app --tail=100 -f      # app logs
kubectl -n xoformpro exec -it postgres-0 -- psql -U xoformpro -d xoformpro   # SQL
kubectl -n xoformpro create job --from=cronjob/postgres-backup backup-now    # backup now
```

Restore a backup (replaces the database — stop the app first):
```bash
kubectl -n xoformpro scale deploy/app --replicas=0
kubectl -n xoformpro exec postgres-0 -- psql -U xoformpro -d postgres -c "DROP DATABASE xoformpro WITH (FORCE)" -c "CREATE DATABASE xoformpro OWNER xoformpro"
sudo zcat /srv/xoformpro/backups/xoformpro-<stamp>.sql.gz | kubectl -n xoformpro exec -i postgres-0 -- psql -U xoformpro -d xoformpro
kubectl -n xoformpro scale deploy/app --replicas=1
```

EF migrations are applied automatically when the app starts.

Email transport (Microsoft Graph or Amazon SES) is configured in the database: row `Id = 1`
of `fsp_email_configurations`.

Optional: `Retention__FreeDays` / `Retention__ProDays` env vars enable deletion of old
submissions (both default `0` = keep forever).

## Routing

Every backend path is served by the app: `/api`, `/submit`, `/v1`, `/form-config`,
`/form-token`, `/uploads`, `/health`, plus the SPA and `/forms.js`. The ingress sends
everything to the app, so a new backend path needs no ingress change. For **local dev**
it still needs a proxy rule in `web/vite.config.ts` — a missing rule returns `index.html`
with a 200 instead of a 404. `forms.js` logs a `[XoFormPro]` console warning when that happens.

## Notes
- `App__TrustForwardedHeaders=true` makes the app use the client IP from the ingress
  (`X-Forwarded-For`); without it every visitor shares one rate-limit bucket.
- Uploads are capped at 25 MB per request by the ingress (`proxy-body-size`).
- PostgreSQL only accepts connections from pods in the `xoformpro` namespace (NetworkPolicy).
- The visible brand is **XOFormPro**; code project / namespace stay `FormSubmissionPro`.

## Legacy: IIS

The previous host was IIS on `164.52.217.36` (`./publish.ps1` → copy `./publish` to the site,
secrets from a git-ignored `appsettings.Production.json`). `web.config`, `web/public/web.config`
and `publish.ps1` are kept for that setup only.
