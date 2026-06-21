#!/usr/bin/env bash
set -euo pipefail

CERT_DIR="$(pwd)/certs"
CERT_NAME="aspnetcore.pfx"
CERT_PASSWORD="P@ssw0rd!"

mkdir -p "$CERT_DIR"

# Clean old certificates, create and trust a new local development certificate,
# then export it to a password-protected PFX file for Docker.

dotnet dev-certs https --clean

dotnet dev-certs https --trust

dotnet dev-certs https --export-path "$CERT_DIR/$CERT_NAME" --password "$CERT_PASSWORD"

cat <<EOF
Generated certificate:
  Path: $CERT_DIR/$CERT_NAME
  Password: $CERT_PASSWORD

Mount the cert folder into Docker and configure:
  ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/$CERT_NAME
  ASPNETCORE_Kestrel__Certificates__Default__Password=$CERT_PASSWORD
EOF
