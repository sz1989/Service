#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "Docker services (db, redis, seq)..."
docker compose up -d db redis seq

echo "dotnet run --project src/Service"
dotnet run --project src/Service &
BACKEND_PID=$!

cleanup() {
  echo
  echo "Stopping backend (pid $BACKEND_PID)..."
  kill "$BACKEND_PID" 2>/dev/null || true
  wait "$BACKEND_PID" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

echo "Preparing UI..."
cd ui
if [ ! -d node_modules ]; then
  echo "==> node_modules not found, running npm install..."
  npm install
fi

echo "npm run dev"
npm run dev
