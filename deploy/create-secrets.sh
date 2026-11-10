#!/usr/bin/env bash
# Creates the `xoformpro-secrets` Kubernetes secret on the server. Run once, before the
# first deploy. Values never touch git or the repo:
#   - Jwt__SigningKey  from api/FormSubmissionPro.Api/appsettings.Production.json (git-ignored),
#                      so existing dashboard logins stay valid after the move
#   - POSTGRES_PASSWORD freshly generated for the new in-cluster database
# Refuses to overwrite an existing secret (that would lock the app out of its database).
# Usage (Git Bash, from the repo root):  bash deploy/create-secrets.sh
set -euo pipefail

KEY="${KEY:-/e/Projects/XenieoneServerKey/LightsailDefaultKey-eu-west-2.pem}"
HOST="${HOST:-ubuntu@13.135.138.127}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROD_JSON="$ROOT/api/FormSubmissionPro.Api/appsettings.Production.json"
SSH=(ssh -i "$KEY" -o BatchMode=yes "$HOST")

if "${SSH[@]}" 'kubectl get secret xoformpro-secrets -n xoformpro >/dev/null 2>&1'; then
  echo "xoformpro-secrets already exists - leaving it alone."
  exit 0
fi

JWT=$(node -e 'console.log(require(process.argv[1]).Jwt.SigningKey)' "$(cygpath -w "$PROD_JSON" 2>/dev/null || echo "$PROD_JSON")")
[ -n "$JWT" ] || { echo "No Jwt.SigningKey in $PROD_JSON"; exit 1; }
PGPW=$(node -e 'console.log(require("crypto").randomBytes(24).toString("base64url"))')
CONN="Host=postgres;Port=5432;Database=xoformpro;Username=xoformpro;Password=$PGPW"

# Values go over stdin as an env file, never on a command line.
printf 'POSTGRES_PASSWORD=%s\nConnectionStrings__Postgres=%s\nJwt__SigningKey=%s\n' "$PGPW" "$CONN" "$JWT" \
  | "${SSH[@]}" 'set -e
      umask 077
      f=$(mktemp); cat > "$f"
      kubectl create namespace xoformpro --dry-run=client -o yaml | kubectl apply -f - >/dev/null
      kubectl create secret generic xoformpro-secrets -n xoformpro --from-env-file="$f"
      rm -f "$f"'
echo "Created xoformpro-secrets."
