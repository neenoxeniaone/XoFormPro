#!/usr/bin/env bash
# Opens a tunnel from this PC to the production XOFormPro database on the k3s server,
# so pgAdmin / DBeaver / psql can connect to localhost. Nothing is exposed publicly:
# kubectl port-forward listens only on the server's 127.0.0.1, and SSH carries it here.
#
# Usage (Git Bash, from the repo root):  bash deploy/db-tunnel.sh      (Ctrl+C to close)
#   LOCAL_PORT=5544 bash deploy/db-tunnel.sh                           (other local port)
set -euo pipefail

KEY="${KEY:-/e/Projects/XenieoneServerKey/LightsailDefaultKey-eu-west-2.pem}"
HOST="${HOST:-ubuntu@13.135.138.127}"
LOCAL_PORT="${LOCAL_PORT:-15433}"
REMOTE_PORT=15433   # on the server's loopback only

PW=$(ssh -i "$KEY" -o BatchMode=yes "$HOST" \
  'kubectl -n xoformpro get secret xoformpro-secrets -o jsonpath="{.data.POSTGRES_PASSWORD}" | base64 -d')

cat <<EOF
Production database tunnel  (Ctrl+C to close)
  Host:     localhost
  Port:     $LOCAL_PORT
  Database: xoformpro
  User:     xoformpro
  Password: $PW

  psql "postgresql://xoformpro:$PW@localhost:$LOCAL_PORT/xoformpro"

This is the LIVE database - be careful with UPDATE / DELETE.
EOF

exec ssh -i "$KEY" -o BatchMode=yes -o ExitOnForwardFailure=yes -o ServerAliveInterval=30 \
  -L "127.0.0.1:$LOCAL_PORT:127.0.0.1:$REMOTE_PORT" "$HOST" \
  "exec kubectl -n xoformpro port-forward --address 127.0.0.1 svc/postgres $REMOTE_PORT:5432"
