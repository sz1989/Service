#!/usr/bin/env bash
set -euo pipefail

# Mints an HS256 JWT matching this service's dev auth config (Program.cs),
# for local testing with curl. 
# Not for use against real environments.

JWT_KEY="${JWT_KEY:-dev-only-signing-key-do-not-use-in-production-32bytes+}"
JWT_ISSUER="${JWT_ISSUER:-Service}"
JWT_AUDIENCE="${JWT_AUDIENCE:-Service}"
JWT_SUBJECT="${JWT_SUBJECT:-testuser}"
# The role can be passed as the first argument, or set via the JWT_ROLE env var, or defaults to "user".
JWT_ROLE="${1:-${JWT_ROLE:-user}}"
JWT_TTL_SECONDS="${JWT_TTL_SECONDS:-3600}"

b64url() {
    openssl base64 -A | tr '+/' '-_' | tr -d '='
}

EXP=$(($(date +%s) + JWT_TTL_SECONDS))

HEADER='{"alg":"HS256","typ":"JWT"}'
PAYLOAD=$(printf '{"iss":"%s","aud":"%s","sub":"%s","exp":%d,"role":"%s"}' \
    "$JWT_ISSUER" "$JWT_AUDIENCE" "$JWT_SUBJECT" "$EXP" "$JWT_ROLE")

HEADER_B64=$(printf '%s' "$HEADER" | b64url)
PAYLOAD_B64=$(printf '%s' "$PAYLOAD" | b64url)
SIGNING_INPUT="$HEADER_B64.$PAYLOAD_B64"
SIGNATURE_B64=$(printf '%s' "$SIGNING_INPUT" | openssl dgst -sha256 -hmac "$JWT_KEY" -binary | b64url)

TOKEN="$SIGNING_INPUT.$SIGNATURE_B64"

echo "$TOKEN"
