---
name: diagnose-local-dev
description: Use when the local dev stack won't come up or a request fails unexpectedly — 401s, cert warnings, "connection refused" to Postgres/Redis, or the API container stuck unhealthy. Triggers on "service won't start", "getting a 401", "cert error", "connection refused", "container unhealthy".
---

# Diagnose local dev issues

Check in this order — each step rules out a layer before moving to the next.

## 1. Is the stack actually up?

```bash
docker compose ps
```

- `db` and `redis` must show `healthy` before `api` will even start — `api` has
  `depends_on: condition: service_healthy` on both in `docker-compose.yml`.
- `db` healthcheck: `pg_isready`. `redis` healthcheck: `redis-cli ping`. If either is
  stuck starting, check `docker logs postgres` / `docker logs service-redis` before
  looking at the API at all.

## 2. Cert warnings / HTTPS failing

- API expects `src/Service/certs/aspnetcore.pfx`, password from `CERT_PASSWORD` in
  `.env` (must match `src/Service/generate-cert.sh`'s default `P@ssw0rd!` unless you
  changed both).
- UI dev server expects `ui/certs/localhost.pem` / `localhost.key`; without them Vite
  silently falls back to `http://localhost:3000` instead of HTTPS.
- Fix: re-run `cd src/Service && ./generate-cert.sh` and/or `cd ui && ./generate-cert.sh`.
  Both export the same trusted `dotnet dev-certs` cert, so don't hand-roll a
  self-signed one — that's why the certs are gitignored and regenerated locally.

## 3. 401 / 403 from an endpoint

- Every controller requires a bearer token except `[AllowAnonymous]` routes
  (`Authentication/ApiKeyAuthenticationHandler.cs`). Mint one:

  ```bash
  ./src/Service/generate-jwt.sh admin
  ```

- 403 (not 401) usually means the token's role doesn't match the controller's
  `[Authorize(Roles = "...")]` — check the role on the token vs. what the action needs.

## 4. "Connection refused" to Postgres/Redis from the API

- If running the API with `dotnet run` (not Dockerized), confirm
  `ConnectionStrings__DefaultConnection` / `ConnectionStrings__Redis` in
  `appsettings.Development.json` point at `localhost`, not the Docker service names
  (`db`/`redis`) — those hostnames only resolve inside the `personnet` Docker network.
- Confirm the infra containers are actually running:
  `docker compose up -d db redis seq`.

## 5. Chat / RAG endpoint failing

- These need Ollama running locally with the right model pulled — not started by
  default. Use `./start.sh --ollama`, or check `doc/AiRef.md`.

## Don't

- Don't commit regenerated certs or `.env` — both are gitignored for a reason.
- Don't "fix" a 401 by adding `[AllowAnonymous]` — find out why the token is missing
  or wrong instead.
