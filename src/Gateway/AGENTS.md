# AGENTS.md — `src/Gateway` (API gateway)

Guidance for working inside the gateway project. Repo-wide setup, commands and CI live in the root `AGENTS.md`.

## Run

```bash
dotnet run --project src/Gateway    # -> https://localhost:7081 (proxies to Service on 7071)
```

The UI's `VITE_API_BASE_URL` points at the gateway, so it must be running for the UI to reach the API.

## Layout

```
src/Gateway/
├── Program.cs                 # composition root: CORS, JWT auth, rate limiting, YARP, /health
├── Extensions/                # ServiceCollectionExtensions, WebApplicationExtensions
├── appsettings.json           # "ReverseProxy" routes/clusters (destination -> Service)
├── appsettings.Development.json
└── Properties/launchSettings.json   # https://localhost:7081
```

## Behavior

- Validates JWTs at the edge using the same `Jwt` issuer/audience/key as `Service` (`Jwt:Key` must match) and rejects missing/invalid tokens with 401.
- `publicRoute` (`/Auth`, `/health`, `/openapi`, `/scalar`) is anonymous; every other path needs a valid JWT or an `X-Api-Key` header (the key itself is only checked by `Service`).
- Auth headers are forwarded, so `Service` re-validates and owns role checks.
- Also handles CORS and edge rate limiting.

## Conventions

- **Add routes/clusters in `appsettings.json`** under `ReverseProxy` — no code change needed.
- Service registration and middleware order live in `Extensions/`, not inline in `Program.cs`.
- Container build: `dockerfile.gateway`.
- **Tests** live in `tests/Gateway.Tests/` (`InternalsVisibleTo` is already set in `Gateway.csproj`).
