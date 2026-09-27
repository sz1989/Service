#!/usr/bin/env bash
set -euo pipefail

CERT_DIR="$(pwd)/certs"

mkdir -p "$CERT_DIR"

# Export the already-trusted ASP.NET Core dev cert (see src/Service/generate-cert.sh)
# to PEM so Vite's dev server can reuse it. Since it's the same trusted dev cert,
# the browser won't show an untrusted-certificate warning.

dotnet dev-certs https --trust

dotnet dev-certs https --export-path "$CERT_DIR/localhost.pem" --format Pem --no-password

cat <<EOF
Generated certificate:
  Cert: $CERT_DIR/localhost.pem
  Key:  $CERT_DIR/localhost.key
EOF
