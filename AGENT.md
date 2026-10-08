# AGENT.md

Project structure reference for this repo. See `README.md` for setup/run instructions and `doc/ServiceRef.md` / `doc/UIRef.md` / `doc/AiRef.md` for endpoint and feature docs.

## Commands

### Setup (one-time)

```bash
cp .env.example .env                           # then edit values as needed
cd src/Service && ./generate-cert.sh && cd -   # backend dev HTTPS cert -> src/Service/certs
cd ui && ./generate-cert.sh && cd -            # UI dev HTTPS cert -> ui/certs
cd ui && npm install && cd -
``` 

### Run

```bash
./start.sh                 # docker deps (db/redis/seq) + API + UI dev server
./start.sh --ollama        # same, plus local Ollama (needed for chat/RAG endpoints)

# or individually:
docker compose up -d --build        # everything in Docker
docker compose up -d db redis seq   # just infra, then run API locally:
dotnet run --project src/Service    # -> https://localhost:7071
dotnet run --project src/Gateway    # -> https://localhost:7081 (proxies to Service)
cd ui && npm run dev                # -> https://localhost:3000 (UI only)
```

Mint a dev JWT for manual API calls:

```bash
./src/Service/generate-jwt.sh          # role: user
./src/Service/generate-jwt.sh admin    # role: admin
curl -k -H "Authorization: Bearer $(./src/Service/generate-jwt.sh admin)" https://localhost/Person/1
```

### Build / Test / Lint

```bash
# Backend
dotnet build ./Service.slnx --configuration Release
dotnet test ./Service.slnx --collect:"XPlat Code Coverage"

# UI (run from ui/)
npm run build       # tsc -b && vite build
npm test            # vitest run (single run, what CI uses)
npm run test:watch  # vitest watch mode
npm run lint        # oxlint
```

CI mirrors these: `.github/workflows/dotnet-build.yml` (restore/build/test + coverage report, triggered on non-`ui/` changes) and `.github/workflows/react-build.yml` (`npm ci`, `npm test`, `npm run build`, triggered on `ui/` changes).

## Conventions

- **Nullable + implicit usings enabled** repo-wide (`Directory.Build.props`, target `net10.0`). Central NuGet package versions live in `Directory.Packages.props` — add new packages there, not per-`.csproj` versions.
- **BAL feature folders**: each feature under `src/Service/BAL/<Feature>/` has an `IXxxService` interface + `XxxService` impl (e.g. `IChatService`/`ChatService`). Controllers depend on the interface, not the concrete class.
- **Primary constructors + constructor injection** for services (e.g. `public class ChatService(IChatClient chatClient) : IChatService`), not field assignment in a body.
- **DI registration lives in `Extensions/ServiceCollectionExtensions.cs`** (and `WebApplicationExtensions.cs` for middleware/pipeline setup) — not inline in `Program.cs`. Prefer the narrowest lifetime that works (`AddSingleton` for connection multiplexers/queues, `AddScoped`/`AddTransient` for request-scoped services).
- **Layering**: `Controllers` → `BAL/<Feature>` → `Data` (repositories / `AppDbContext`). Don't call `Data` directly from a controller, and don't put business logic in `Data`.
- **API versioning**: breaking controller changes go in a new `Controllers/vN/` folder (see `v1/PersonController.cs` vs `v2/PersonController.cs`) rather than mutating the existing version.
- **UI tests** live next to the component as `*.test.tsx` (e.g. `PersonActions.test.tsx`), using Vitest + React Testing Library; new components should follow the same colocation pattern.
- Keep `doc/ServiceRef.md` / `doc/UIRef.md` / `doc/AiRef.md` in sync when adding endpoints or UI routes — they're the canonical reference, this file is just structure/commands/conventions.

## Overview

ASP.NET Core 10 Web API (person records, JWT auth, Redis, Postgres/EF Core, LLM chat + RAG via Ollama, ML.NET prediction, MCP server) with a separate React + TypeScript UI. Solution is defined in `Service.slnx`.

## Top-level layout

```
.
├── Service.slnx              # solution file (Gateway, Model, Service, Service.Tests)
├── Directory.Build.props     # shared MSBuild settings
├── Directory.Packages.props  # central NuGet package version management
├── docker-compose.yml        # API + Postgres + Redis + pgAdmin + Seq
├── dockerfile                # Service container build
├── start.sh                  # helper launch script
├── db/                       # init/seed/query SQL (Postgres + pgvector)
├── doc/                      # ServiceRef.md, UIRef.md, AiRef.md reference docs
├── src/
│   ├── Gateway/              # Gateway.csproj — YARP reverse proxy / API gateway in front of Service
│   ├── Model/                # Model.csproj — shared POCOs (Person, Train)
│   └── Service/               # Service.csproj — the API project
├── tests/
│   └── Service.Tests/        # xUnit test project
└── ui/                        # React + TypeScript + Vite frontend
```

## `src/Service` (API project)

```
src/Service/
├── Program.cs                 # app composition root
├── Using.cs, Utility.cs
├── Authentication/            # ApiKeyAuthenticationHandler, AuthOptions
├── BAL/                        # business logic, one folder per feature, interface + impl
│   ├── Auth/
│   ├── Chat/                   # IChatClient-backed chat service
│   ├── Embeddings/              # embeds docs for RAG
│   ├── People/
│   ├── Prediction/              # ML.NET salary prediction
│   └── Rag/                     # retrieval-augmented generation
├── Controllers/                # API endpoints
│   ├── v1/, v2/                 # versioned PersonController (Asp.Versioning)
│   └── AuthController, ChatController, InventoryController,
│       PredictionController, ResilienceController, StationController,
│       WeatherForecastController
├── Data/                        # AppDbContext, GenericRepository, PersonsRepository
├── ErrorHandling/                # GlobalExceptionHandler
├── Extensions/                   # ServiceCollectionExtensions, WebApplicationExtensions
│                                 #   (DI/service registration lives here, not in Program.cs)
├── Hubs/                         # ChatHub (SignalR)
├── MLModels/                     # ModelBuilder.cs + model.zip (ML.NET salary model)
├── Models/                       # DTOs: ChatMessages, Inventory, PersonData, etc.
├── Others/                       # Noted.cs, Other.cs (scratch/example code)
├── Services/                     # background services, queues, Redis pub/sub
│   ├── AppBackgroundService, ChatBackgroundService
│   ├── BackgroundTaskQueue / IBackgroundTaskQueue
│   ├── ChatRequestQueue, ChatRequestRegistry (+ interfaces)
│   ├── RedisPublisher / IRedisPublisher, RedisChannels
│   └── PersonNotificationSubscriberService
├── Tools/                        # CustomerTools.cs (MCP server tools)
├── appsettings.json, appsettings.Development.json
├── Properties/launchSettings.json
├── generate-cert.sh, generate-jwt.sh
└── certs/                         # dev HTTPS cert (git-ignored contents)
```

Layering convention: `Controllers` → `BAL/<Feature>` (interface + impl) → `Data` (repositories/`AppDbContext`). Cross-cutting concerns (auth, error handling, background work, Redis) sit alongside in their own top-level folders rather than inside `BAL`.

## `src/Gateway` (API gateway)

```
src/Gateway/
├── Program.cs                 # composition root: CORS, JWT auth, rate limiting, YARP, /health
├── Extensions/                # ServiceCollectionExtensions, WebApplicationExtensions
├── appsettings.json           # "ReverseProxy" routes/clusters (destination -> Service)
├── appsettings.Development.json
└── Properties/launchSettings.json   # https://localhost:7081
```

The gateway validates JWTs at the edge (same `Jwt` issuer/audience/key as `Service`; `Jwt:Key` must match) and rejects missing/invalid tokens with 401. `publicRoute` (`/Auth`, `/health`, `/openapi`, `/scalar`) is anonymous; every other path needs a valid JWT or an `X-Api-Key` header (the key itself is only checked by `Service`). Auth headers are forwarded, so `Service` re-validates and owns role checks. Also handles CORS + edge rate limiting. Add routes/clusters in `appsettings.json` (no code change). Container build: `dockerfile.gateway`. The UI's `VITE_API_BASE_URL` points at the gateway.

## `ui/` (React frontend)

```
ui/
├── index.html, vite.config.ts, tsconfig*.json
├── certs/                     # dev HTTPS cert (git-ignored contents)
├── public/                    # favicon.svg, icons.svg
└── src/
    ├── main.tsx, App.tsx, index.css
    ├── api/                   # config.ts, types.ts
    ├── auth/                  # AuthContext.tsx
    ├── components/            # ChatPanel, IngestPanel, PersonList, PersonActions,
    │                          #   ProtectedRoute, RagSearchPanel, ErrorBoundary (+ *.test.tsx)
    ├── hooks/                 # useChatHub.ts, useFetch.ts
    ├── pages/                 # DashboardPage.tsx, LoginPage.tsx
    └── test/                  # setup.ts (Vitest)
```

## Other notes

- Root `package.json` only pulls in `@modelcontextprotocol/inspector`; it is unrelated to `ui/package.json`.
- CI: `.github/workflows/dotnet-build.yml` and `react-build.yml`.
