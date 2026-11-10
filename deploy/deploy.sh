#!/usr/bin/env bash
# Build XOFormPro (React + API), upload it to the k3s server and (re)apply its resources.
# Usage (Git Bash, from the repo root):  bash deploy/deploy.sh
# First time only, before this:          bash deploy/create-secrets.sh
set -euo pipefail

KEY="${KEY:-/e/Projects/XenieoneServerKey/LightsailDefaultKey-eu-west-2.pem}"
HOST="${HOST:-ubuntu@13.135.138.127}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
API="$ROOT/api/FormSubmissionPro.Api"
OUT="$ROOT/deploy/.out"
SSH=(ssh -i "$KEY" -o BatchMode=yes "$HOST")

echo "==> Building frontend"
(cd "$ROOT/web" && npm ci --silent && npm run build)

echo "==> Copying SPA into API wwwroot"
rm -rf "$API/wwwroot/assets"
find "$API/wwwroot" -maxdepth 1 -type f ! -name '.gitkeep' -delete
cp -r "$ROOT/web/dist/." "$API/wwwroot/"

echo "==> Publishing API"
rm -rf "$OUT"
dotnet publish "$API/FormSubmissionPro.Api.csproj" -c Release -o "$OUT" --nologo -v quiet
mkdir -p "$OUT/wwwroot/uploads"   # mount point for the uploads volume

echo "==> Uploading app (secrets and IIS-only files excluded)"
tar -czf - -C "$OUT" \
  --exclude='./appsettings.Production.json' --exclude='./appsettings.Development.json' \
  --exclude='./web.config' --exclude='./wwwroot/web.config' \
  --exclude='./wwwroot/uploads/*' --exclude='*.pdb' \
  . | "${SSH[@]}" 'set -e
    sudo mkdir -p /srv/xoformpro/app.new /srv/xoformpro/app /srv/xoformpro/uploads /srv/xoformpro/keys /srv/xoformpro/backups
    sudo tar -xzf - -C /srv/xoformpro/app.new --no-same-owner
    sudo chmod -R a+rX /srv/xoformpro/app.new
    sudo rsync -a --delete /srv/xoformpro/app.new/ /srv/xoformpro/app/
    sudo rm -rf /srv/xoformpro/app.new
    sudo chown 1654:1654 /srv/xoformpro/uploads /srv/xoformpro/keys
    sudo chmod 700 /srv/xoformpro/keys /srv/xoformpro/backups'

echo "==> Applying Kubernetes resources"
"${SSH[@]}" 'kubectl apply -f -' < "$ROOT/deploy/k8s-xoformpro.yaml"
"${SSH[@]}" 'set -e
  kubectl -n xoformpro rollout status statefulset/postgres --timeout=180s
  kubectl -n xoformpro rollout restart deploy/app >/dev/null
  kubectl -n xoformpro rollout status deploy/app --timeout=240s'
echo "==> Done"
