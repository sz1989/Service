#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

OLLAMA_MODEL="llama3.2:1b"
OLLAMA_EMBEDDING_MODEL="nomic-embed-text"
WITH_OLLAMA=false

for arg in "$@"; do
  case "$arg" in
    --ollama)
      WITH_OLLAMA=true
      ;;
    *)
      echo "Unknown argument: $arg" >&2
      echo "Usage: $0 [--ollama]" >&2
      exit 1
      ;;
  esac
done

echo "Docker services (db, redis, seq)..."
docker compose up -d db redis seq

OLLAMA_PID=""
if [ "$WITH_OLLAMA" = true ]; then
  if ! command -v ollama >/dev/null 2>&1; then
    echo "ollama not found on PATH. Install it from https://ollama.com/download" >&2
    exit 1
  fi

  if curl -s -o /dev/null http://localhost:11434; then
    echo "Ollama server already running."
  else
    echo "Starting Ollama server..."
    ollama serve &
    OLLAMA_PID=$!

    for _ in $(seq 1 30); do
      curl -s -o /dev/null http://localhost:11434 && break
      sleep 1
    done
  fi

  echo "Ensuring model $OLLAMA_MODEL is pulled..."
  ollama pull "$OLLAMA_MODEL"

  echo "Ensuring embedding model $OLLAMA_EMBEDDING_MODEL is pulled..."
  ollama pull "$OLLAMA_EMBEDDING_MODEL"
fi

echo "dotnet run --project src/Service"
dotnet run --project src/Service &
BACKEND_PID=$!

cleanup() {
  echo
  echo "Stopping backend (pid $BACKEND_PID)..."
  kill "$BACKEND_PID" 2>/dev/null || true
  wait "$BACKEND_PID" 2>/dev/null || true

  if [ -n "$OLLAMA_PID" ]; then
    echo "Stopping Ollama server (pid $OLLAMA_PID)..."
    kill "$OLLAMA_PID" 2>/dev/null || true
    wait "$OLLAMA_PID" 2>/dev/null || true
  fi
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
