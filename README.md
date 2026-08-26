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
```

## Docker Run

```bash
docker build -t service:latest .

docker run -d --name my-service -p 7071:8081 -p 7070:8080 -v "$(pwd)/src/Service/certs:/app/certs" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/aspnetcore.pfx -e ASPNETCORE_Kestrel__Certificates__Default__Password='P@ssw0rd!' service:latest

docker rm -f container_name
```
-f (force) run docker run at root 

dotnet publish --os linux --configuration Release -t:PublishContainer

docker run -d -p 7070:8080 service

## Docker CLI:
```bash
docker kill $(docker ps -q)
```
Lists your running containers.-q: The "quiet" flag. It strips away the extra columns (like names, images, and status) and outputs only the container IDs.$(...): A command substitution. It runs the inside command first, grabs the list of IDs, and passes them as arguments to the outside command.docker kill: The action command. It forces the containers to stop immediately.
```bash
docker ps -q | xargs -r docker stop
```
docker stop (Graceful): Sends a SIGTERM signal. It asks the container nicely to save its state, finish current tasks, and close down safely. It waits 10 seconds before forcing it shut.docker kill (Forced): Sends a SIGKILL signal. It bypasses the container's internal process and cuts the power instantly.

## Docker Compose CLI:
```bash
docker compose up -d  # -v command removes named volumes # -d it instructs Docker to start your containers in the background and leave them running.
docker compose down 

docker compose down && docker compose up -d --build

docker logs service-api
```
docker compose down -v. deletes all named and anonymous volumes attached to the services defined in your Docker Compose fileclear

### How to Double-Check Your Setup

If you want to verify that Docker Compose is pulling the password correctly before launching, you can render your evaluated compose file in your terminal:

curl -k -H is used to send a web request to a server while ignoring insecure SSL certificate warnings and adding custom headers

```bash
docker compose config

curl -k -H "Authorization: Bearer $(./src/Service/generate-jwt.sh)" https://localhost:7071/weatherforecast -v   
curl -k -H "Authorization: Bearer $(./src/Service/generate-jwt.sh)" https://localhost:7071/Person/2 -v  
curl -k -H "Authorization: Bearer $(./src/Service/generate-jwt.sh)" https://localhost:7071/Person/All -v  # generating errors
curl -X POST https://localhost:7071/Person/1/refresh -k -H "Authorization: Bearer $(./src/Service/generate-jwt.sh)" # test background service

# docker
curl -k -H "Authorization: Bearer $(./src/Service/generate-jwt.sh)" https://localhost/weatherforecast -v
curl -k -H "Authorization: Bearer $(./src/Service/generate-jwt.sh)" https://localhost/Person/2 -v  

curl -X POST https://localhost:7071/Prediction/predict-salary \
  -H "Content-Type: application/json" \
  -d '{"name": "Alice", "age": 25}'

pgadmin: http://localhost:8080/login?next=/

seq: http://localhost:5341
```

## Test MCP Inspector
```
npm i @modelcontextprotocol/inspector
npx @modelcontextprotocol/inspector
```
for Http, inspector only works on Http

### Copolit Reference:
🛠️ Core Slash Commands (/)These act as shortcuts so you don't have to write out long prompts. Just type  in the chat input to see them. 

• /explain - Explains the selected code block, file, or a general programming concept. Great for deciphering legacy code. 
• /fix - Analyzes the selected code or the error you are currently highlighting and proposes a fix. 
• /tests - Generates unit tests for the selected methods or functions. 
• /setupTests - Provides recommendations and steps to set up a testing framework for your current project. 
• /doc - Automatically generates documentation comments (like XML summaries in C#) for your code. 
• /new - Scaffolds a new project, workspace, or file based on a natural language description. 
• /plan - Creates a detailed, step-by-step implementation plan for a complex coding task before you actually write the code. 
• /clear - Clears the current chat context and starts a fresh session. 
• /search - Translates your natural language question into a robust codebase search query. 
• /startDebugging - Helps you generate a  configuration and start a debugging session. [2]  

* @workspace - Has deep knowledge of your entire open codebase. Use this when asking questions that require looking across multiple files (e.g., "@workspace Where are we configuring the database connection?").
* @terminal - Has context about your integrated terminal. You can ask it how to run a specific command, or ask it to figure out why a terminal build command just failed.
* @github - Has knowledge of your repository, issues, pull requests, and GitHub actions.
* @vscode - Knows about the IDE itself. You can ask it how to change a setting, find a shortcut, or customize your editor.
🖇️ Context Variables (#)
You can use variables to explicitly attach specific pieces of context to your prompt.
* #file - Forces Copilot to read a specific file (e.g., "Explain how #file:Program.cs connects to #file:appsettings.json").
* #selection - Explicitly scopes your prompt to just the code you currently have highlighted.
* #terminalLastCommand - Grabs the last command and output from your terminal (perfect for debugging failures).
Pro-Tip for your .NET work: If you ever get an MSBuild or container publish error in your terminal, just pop open Copilot Chat and type:
"@terminal /explain why my dotnet publish command failed" Copilot will read the exact error output and tell you how to fix it!

### Postgre:
Once you are on the dashboard, you need to register your database server:Click 

Add New Server on the quick links dashboard (or right-click Servers in the left sidebar and choose Register > Server...)
.In the General tab:Name: Type a friendly name for your connection (e.g., Local Dev DB).Click on the Connection tab and fill out the fields exactly like this:Host name/address: db 
(Crucial: You must use the Docker service name db, not localhost, because pgAdmin and the database are running inside the same Docker network) [^1, 2]Port: 5432 [^1]Maintenance database: Enter the value of your ${DB_NAME} variable from your .env file.Username: Enter the value of your ${DB_USER} variable from your .env file.Password: Enter the value of your ${DB_PW} variable from your .env file.Save password?: Check this box so you do not have to type it every time.

```SQL
SELECT version();
```