#!/usr/bin/env bash
# Copies the CURRENT production database (old IIS server) into the in-cluster PostgreSQL,
# REPLACING whatever the new database holds. Safe to re-run: do a rehearsal now, then run
# it again at cutover so the new server gets the latest submissions.
#
# The old database only accepts connections from this PC, not from the k3s server, so the
# copy is tunnelled: ssh -R makes the server's 127.0.0.1:15432 reach the old database
# through this machine. A one-off host-network pod runs pg_dump against that tunnel.
#
# Source = ConnectionStrings.Postgres in api/FormSubmissionPro.Api/appsettings.Production.json
# (git-ignored). A copy of each source dump is kept in /srv/xoformpro/backups on the server.
#
# Usage (Git Bash, from the repo root):  bash deploy/migrate-db.sh
set -euo pipefail

KEY="${KEY:-/e/Projects/XenieoneServerKey/LightsailDefaultKey-eu-west-2.pem}"
HOST="${HOST:-ubuntu@13.135.138.127}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROD_JSON="$ROOT/api/FormSubmissionPro.Api/appsettings.Production.json"
TUNNEL_PORT=15432

# Parse "Host=..;Port=..;Database=..;Username=..;Password=.." into shell vars.
eval "$(node -e '
  const cs = require(process.argv[1]).ConnectionStrings.Postgres;
  const kv = Object.fromEntries(cs.split(";").filter(Boolean).map(p => { const i = p.indexOf("="); return [p.slice(0,i).trim().toLowerCase(), p.slice(i+1)]; }));
  const q = s => "'"'"'" + String(s).replace(/'"'"'/g, "'"'"'\\'"'"''"'"'") + "'"'"'";
  console.log(`SRC_HOST=${q(kv.host)} SRC_PORT=${q(kv.port||5432)} SRC_DB=${q(kv.database)} SRC_USER=${q(kv.username)} SRC_PW=${q(kv.password)}`);
' "$(cygpath -w "$PROD_JSON" 2>/dev/null || echo "$PROD_JSON")")"

STAMP=$(date -u +%Y%m%d-%H%M%S)
DUMP="/srv/xoformpro/backups/migrate-source-$STAMP.sql"
echo "==> Copying $SRC_DB@$SRC_HOST -> xoformpro (in cluster). Dump kept at $DUMP"

# Upload the remote half first, then run it with only the source password on stdin
# (never on a command line).
REMOTE_SCRIPT=/tmp/xoformpro-migrate-$STAMP.sh
ssh -i "$KEY" -o BatchMode=yes "$HOST" "umask 077; cat > $REMOTE_SCRIPT" <<'REMOTE'
set -euo pipefail
read -r SRC_PW
NS=xoformpro
POD=pgmigrate-$(date +%s)

echo "--> Stopping the app so nothing writes during the restore"
kubectl -n $NS scale deploy/app --replicas=0 >/dev/null 2>&1 || true
kubectl -n $NS wait --for=delete pod -l app=xoformpro --timeout=120s >/dev/null 2>&1 || true

cleanup() {
  kubectl -n $NS delete secret "$POD" --ignore-not-found >/dev/null
  kubectl -n $NS delete pod "$POD" --ignore-not-found --wait=false >/dev/null
  echo "--> Starting the app"
  kubectl -n $NS scale deploy/app --replicas=1 >/dev/null 2>&1 || true
}
trap cleanup EXIT

kubectl -n $NS create secret generic "$POD" --from-literal=PGPASSWORD="$SRC_PW" >/dev/null

echo "--> Dumping source via tunnel"
sudo mkdir -p /srv/xoformpro/backups
kubectl -n $NS apply -f - >/dev/null <<YAML
apiVersion: v1
kind: Pod
metadata: { name: $POD, namespace: $NS }
spec:
  hostNetwork: true
  dnsPolicy: ClusterFirstWithHostNet
  restartPolicy: Never
  containers:
    - name: dump
      image: postgres:17.6-alpine
      envFrom: [ { secretRef: { name: $POD } } ]
      command: ["sh","-c","pg_dump -h 127.0.0.1 -p $PORT -U $SRC_USER -d $SRC_DB --no-owner --no-privileges -f /backups/$(basename $DUMP).tmp && mv /backups/$(basename $DUMP).tmp /backups/$(basename $DUMP)"]
      volumeMounts: [ { name: b, mountPath: /backups } ]
  volumes: [ { name: b, hostPath: { path: /srv/xoformpro/backups, type: Directory } } ]
YAML
kubectl -n $NS wait --for=jsonpath='{.status.phase}'=Succeeded pod/$POD --timeout=300s >/dev/null || {
  echo "!! dump failed:"; kubectl -n $NS logs $POD || true; exit 1; }
sudo test -s "$DUMP" || { echo "!! dump file is empty"; exit 1; }
echo "    $(sudo du -h "$DUMP" | cut -f1) dumped"

echo "--> Replacing the in-cluster database"
kubectl -n $NS exec postgres-0 -- psql -q -U xoformpro -d postgres -v ON_ERROR_STOP=1 \
  -c "DROP DATABASE IF EXISTS xoformpro WITH (FORCE)" -c "CREATE DATABASE xoformpro OWNER xoformpro" >/dev/null
sudo cat "$DUMP" | kubectl -n $NS exec -i postgres-0 -- psql -q -U xoformpro -d xoformpro -v ON_ERROR_STOP=1 >/dev/null

echo "--> Row counts in the new database:"
kubectl -n $NS exec postgres-0 -- psql -U xoformpro -d xoformpro -At -c "
  select 'organizations', count(*) from fsp_organizations union all
  select 'users', count(*) from fsp_users union all
  select 'forms', count(*) from fsp_forms union all
  select 'submissions', count(*) from fsp_submissions union all
  select 'migrations', count(*) from \"__EFMigrationsHistory\"" | sed 's/|/: /; s/^/    /'
REMOTE

printf '%s\n' "$SRC_PW" | ssh -i "$KEY" -o BatchMode=yes -o ExitOnForwardFailure=yes \
  -R "127.0.0.1:$TUNNEL_PORT:$SRC_HOST:$SRC_PORT" "$HOST" \
  "SRC_DB='$SRC_DB' SRC_USER='$SRC_USER' DUMP='$DUMP' PORT=$TUNNEL_PORT bash $REMOTE_SCRIPT; rc=\$?; rm -f $REMOTE_SCRIPT; exit \$rc"
echo "==> Done"
