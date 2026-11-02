# AGENTS.md — `src/Service` (API project)

Guidance for working inside the API project. Repo-wide setup, commands and CI live in the root `AGENTS.md`; endpoint docs are in `doc/ServiceRef.md` / `doc/AiRef.md`.

## Run

```bash
dotnet run --project src/Service    # -> https://localhost:7071
./src/Service/generate-jwt.sh admin # mint a dev JWT (role: user by default)
```

## Layout

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

Layering: `Controllers` → `BAL/<Feature>` (interface + impl) → `Data` (repositories/`AppDbContext`). Cross-cutting concerns (auth, error handling, background work, Redis) sit alongside in their own top-level folders rather than inside `BAL`.

## Conventions

- **BAL feature folders**: each feature under `BAL/<Feature>/` has an `IXxxService` interface + `XxxService` impl (e.g. `IChatService`/`ChatService`). Controllers depend on the interface, not the concrete class.
- **Primary constructors + constructor injection** for services (e.g. `public class ChatService(IChatClient chatClient) : IChatService`), not field assignment in a body.
- **DI registration lives in `Extensions/ServiceCollectionExtensions.cs`** (and `WebApplicationExtensions.cs` for middleware/pipeline setup) — not inline in `Program.cs`. Prefer the narrowest lifetime that works (`AddSingleton` for connection multiplexers/queues, `AddScoped`/`AddTransient` for request-scoped services).
- **Layering**: don't call `Data` directly from a controller, and don't put business logic in `Data`.
- **API versioning**: breaking controller changes go in a new `Controllers/vN/` folder (see `v1/PersonController.cs` vs `v2/PersonController.cs`) rather than mutating the existing version.
- **Tests** live in `tests/Service.Tests/`, mirroring the folders here (`BAL/`, `Controllers/`).
- Keep `doc/ServiceRef.md` / `doc/AiRef.md` in sync when adding endpoints.
