[![Build](https://github.com/sz1989/Service/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/sz1989/Service/actions/workflows/dotnet-build.yml)

# About

An ASP.NET Core 10 Web API for managing person records, secured with JWT bearer auth and role-based authorization. Person data is persisted in PostgreSQL (via EF Core) and cached in Redis, with Redis pub/sub used to broadcast update notifications to other services. The service also exposes an ML.NET-powered salary prediction endpoint, a Model Context Protocol (MCP) server, a background task queue for async work, and Polly-based resilience policies. It runs as a set of Docker Compose services (API, Postgres, Redis, pgAdmin, Seq) for local development.

# Tech Stack

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=.net&logoColor=white)](https://dotnet.microsoft.com/) [![C#](https://img.shields.io/badge/C%23-latest-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/) [![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-6DB33F?logo=asp.net&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet) [![BackgroundService](https://img.shields.io/badge/BackgroundService-Hosted%20Service-512BD4)](https://learn.microsoft.com/dotnet/core/extensions/workers)  
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-316192?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Redis](https://img.shields.io/badge/Redis-Latest-DC382D?logo=redis&logoColor=white)](https://redis.io/) [![Docker](https://img.shields.io/badge/Docker-Compose%20%2F%20Docker-2496ED?logo=docker&logoColor=white)](https://www.docker.com/) [![ML.NET](https://img.shields.io/badge/ML.NET-5.0-008080)](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet)  
[![Serilog](https://img.shields.io/badge/Serilog-Logging-4E0A80)](https://serilog.net/) [![OpenAPI](https://img.shields.io/badge/OpenAPI-Swagger-85EA2D?logo=swagger&logoColor=white)](https://swagger.io/) [![Model%20Context%20Protocol](https://img.shields.io/badge/MCP-Model%20Context%20Protocol-00ADEF)](https://modelcontextprotocol.org/) [![xUnit](https://img.shields.io/badge/xUnit-Tests-CC2927?logo=xunit&logoColor=white)](https://xunit.net/) [![Polly](https://img.shields.io/badge/Polly-Resilience-7B68EE)](https://www.pollydocs.org/)

# Reference

## Generate the certificate

```bash
cd /Users/davidkao/Projects/Service
./generate-cert.sh

docker compose down && docker compose up -d --build

docker logs service-api
```