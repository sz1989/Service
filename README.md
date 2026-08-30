# Service

[![Build](https://github.com/sz1989/Service/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/sz1989/Service/actions/workflows/dotnet-build.yml)

## About

An ASP.NET Core 10 Web API for managing person records, secured with JWT bearer auth and role-based authorization. Person data is persisted in PostgreSQL (via EF Core) and cached in Redis, with Redis pub/sub used to broadcast update notifications to other services. The service also exposes an ML.NET-powered salary prediction endpoint, a Model Context Protocol (MCP) server, a background task queue for async work, and Polly-based resilience policies. It runs as a set of Docker Compose services (API, Postgres, Redis, pgAdmin, Seq) for local development.

## Tech Stack

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=.net&logoColor=white)](https://dotnet.microsoft.com/) [![C#](https://img.shields.io/badge/C%23-latest-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/) [![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-6DB33F?logo=asp.net&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet) [![BackgroundService](https://img.shields.io/badge/BackgroundService-Hosted%20Service-512BD4)](https://learn.microsoft.com/dotnet/core/extensions/workers)  
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4)](https://learn.microsoft.com/ef/core/) [![Npgsql](https://img.shields.io/badge/Npgsql-PostgreSQL%20Provider-316192?logo=postgresql&logoColor=white)](https://www.npgsql.org/efcore/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-316192?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![pgAdmin](https://img.shields.io/badge/pgAdmin-4-326690?logo=postgresql&logoColor=white)](https://www.pgadmin.org/)  
[![Redis](https://img.shields.io/badge/Redis-Latest-DC382D?logo=redis&logoColor=white)](https://redis.io/) [![StackExchange.Redis](https://img.shields.io/badge/StackExchange.Redis-Pub%2FSub-DC382D?logo=redis&logoColor=white)](https://stackexchange.github.io/StackExchange.Redis/) [![Rate Limiting](https://img.shields.io/badge/Rate%20Limiting-Redis%20Distributed-DC382D)](https://github.com/cristipufu/aspnetcore-redis-rate-limiting) [![Docker](https://img.shields.io/badge/Docker-Compose%20%2F%20Docker-2496ED?logo=docker&logoColor=white)](https://www.docker.com/) [![ML.NET](https://img.shields.io/badge/ML.NET-5.0-008080)](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet)  
[![JWT](https://img.shields.io/badge/JWT-Bearer%20Auth-000000?logo=jsonwebtokens&logoColor=white)](https://jwt.io/) [![RBAC](https://img.shields.io/badge/Authorization-Role--Based%20(RBAC)-6DB33F)](https://learn.microsoft.com/aspnet/core/security/authorization/roles) [![Health Checks](https://img.shields.io/badge/Health%20Checks-ASP.NET%20Core-6DB33F)](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)  
[![Serilog](https://img.shields.io/badge/Serilog-Logging-4E0A80)](https://serilog.net/) [![Seq](https://img.shields.io/badge/Seq-Structured%20Logs-005A9C)](https://datalust.co/seq) [![OpenAPI](https://img.shields.io/badge/OpenAPI-Swagger-85EA2D?logo=swagger&logoColor=white)](https://swagger.io/) [![Scalar](https://img.shields.io/badge/Scalar-API%20Reference-1A1A1A)](https://scalar.com/) [![Model%20Context%20Protocol](https://img.shields.io/badge/MCP-Model%20Context%20Protocol-00ADEF)](https://modelcontextprotocol.org/) [![Polly](https://img.shields.io/badge/Polly-Resilience-7B68EE)](https://www.pollydocs.org/)  
[![xUnit](https://img.shields.io/badge/xUnit-Tests-CC2927?logo=xunit&logoColor=white)](https://xunit.net/) [![Coverlet](https://img.shields.io/badge/Coverlet-Code%20Coverage-CC2927)](https://github.com/coverlet-coverage/coverlet) [![GitHub Actions](https://img.shields.io/badge/GitHub%20Actions-CI-2088FF?logo=githubactions&logoColor=white)](https://github.com/features/actions)

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) with Docker Compose
- `bash` and `openssl` (for the helper scripts)

### 1. Configure environment

```bash
cp .env.example .env   # then edit values as needed
```

`.env` is git-ignored and consumed by `docker-compose.yml`. Keep `CERT_PASSWORD` in
sync with the password in `src/Service/generate-cert.sh` (default `P@ssw0rd!`).

### 2. Generate the dev HTTPS certificate

The script writes to `./certs`, which Compose mounts into the API container, so it
must be run from `src/Service`:

```bash
cd src/Service && ./generate-cert.sh && cd -
```

### 3. Run

```bash
docker compose up -d --build
```

Or run the API directly against Dockerized dependencies:

```bash
docker compose up -d db redis
dotnet run --project src/Service   # https://localhost:7071
```

## Service URLs

| Service          | URL                         | Notes                                  |
|------------------|-----------------------------|----------------------------------------|
| API (HTTPS)      | https://localhost           | `443:8081` in Compose; `7071` via `dotnet run` |
| API (HTTP)       | http://localhost            | `80:8080` in Compose; `7070` via `dotnet run`  |
| Scalar API ref   | https://localhost/scalar/v1 | Development environment only            |
| Health           | https://localhost/health, `/health/details` |                        |
| MCP endpoint     | https://localhost/mcp       | HTTP transport                          |
| pgAdmin          | http://localhost:8080       | Login from `PGADMIN_*` in `.env`        |
| Seq (logs)       | http://localhost:5341       |                                        |
| Postgres         | localhost:5432              |                                        |
| Redis            | localhost:6379              |                                        |

## Authentication

All controllers require a bearer token except where marked `[AllowAnonymous]`.
Mint a dev HS256 token (optionally with a role) using the helper script:

```bash
./src/Service/generate-jwt.sh          # role: user
./src/Service/generate-jwt.sh admin    # role: admin

curl -k -H "Authorization: Bearer $(./src/Service/generate-jwt.sh admin)" https://localhost/Person/1
```

## Endpoints

| Route                          | Auth            | Description                                   |
|--------------------------------|-----------------|----------------------------------------------|
| `GET /Person/{id}`             | `admin`, `user` | Person by id, Redis cache-aside              |
| `GET /Person/All`             | `admin`         | Demonstrates the global exception handler    |
| `POST /Person/{id}/refresh`   | anonymous       | Queues background refresh + Redis pub/sub    |
| `POST /Prediction/predict-salary` | any token   | ML.NET salary prediction                     |
| `GET /Inventory/All`          | any token       | Inventory items from Postgres                |
| `GET /Resilience`, `/Resilience/circuit-breaker`, `/Resilience/timeout` | any token | Polly retry / circuit-breaker / timeout demos |
| `GET /WeatherForecast`        | any token       | Sample data                                  |
| `GET /health`, `/health/details` | anonymous    | Liveness + Redis health check                |
| `POST /mcp`                    | anonymous       | MCP server (`GetWeather` tool)               |

## Testing

```bash
dotnet test ./Service.slnx --collect:"XPlat Code Coverage"
```

## Reference

```bash
docker compose down && docker compose up -d --build
docker logs service-api
docker compose up -d redis   # run redis and any services it depends on
```

See [Ref.md](Ref.md) for additional reference commands (Docker build/run, publish, pgAdmin setup, Copilot).
