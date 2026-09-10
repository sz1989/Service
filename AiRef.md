# AI Reference

AI-related commands for this service: the LLM chat endpoint (Ollama via `Microsoft.Extensions.AI` / `OllamaSharp`), the ML.NET salary prediction endpoint, and the MCP server.

All HTTP examples assume the API is running via `dotnet run --project src/Service` (`https://localhost:7071`). Swap the host for `https://localhost` when running under Docker Compose. `-k` skips dev-cert validation; endpoints require a bearer token from `./src/Service/generate-jwt.sh`.

---

## 1. Chat endpoint (Ollama LLM)

`GET /Chat?question=...` → `ChatController` → injected `IChatClient` (an `OllamaApiClient` registered in `Program.cs`) → Ollama at `http://localhost:11434`, model `llama3.2:1b`.

### Prerequisites — Ollama running locally

```bash
# Install (macOS)
brew install ollama

# Start the Ollama server (leave running, or `brew services start ollama`)
ollama serve

# Pull the model used by Utility.Talk
ollama pull llama3.2:1b

# Sanity-check the model directly
ollama run llama3.2:1b "Why is the sky blue?"

# Confirm the API is up
curl http://localhost:11434/api/tags
```

### Call the endpoint

```bash
# -G keeps it a GET; --data-urlencode escapes spaces / ? / & / #
curl -k -G https://localhost:7071/Chat \
  -H "Authorization: Bearer $(./src/Service/generate-jwt.sh admin)" \
  --data-urlencode "question=Why is the sky blue?"

# multiple params
curl -k -G https://localhost:7071/Chat \
  -H "Authorization: Bearer $(./src/Service/generate-jwt.sh)" \
  --data-urlencode "question=Summarize this in one sentence: the mitochondria is the powerhouse of the cell"
```

Empty/whitespace `question` → `400 Bad Request` (`"question is required."`).

### Override the model / host

Set via configuration (`appsettings.json`, env vars, or user-secrets):

```json
"Ollama": {
  "Endpoint": "http://localhost:11434",
  "Model": "llama3.2:1b"
}
```

```bash
# e.g. override per-run with environment variables
Ollama__Model=llama3.1:8b dotnet run --project src/Service
```

---

## 2. Salary prediction (ML.NET)

`POST /Prediction/predict-salary` → `PredictionController` → `PredictionEnginePool<PersonData, PersonPrediction>` (model `PersonSalaryModel`).

```bash
curl -k -X POST https://localhost:7071/Prediction/predict-salary \
  -H "Authorization: Bearer $(./src/Service/generate-jwt.sh)" \
  -H "Content-Type: application/json" \
  -d '{"name": "Alice", "age": 25}'
```

Response: `{ "name": "Alice", "age": 25, "predictedSalary": <float> }`. Blank `name` → `400`.

### Model lifecycle

- Model file: `src/Service/MLModels/model.zip`. Training data: `src/Service/MLModels/people.csv`.
- On startup `AddMachineLearning` trains and saves `model.zip` **only if it does not exist** (`MLModels.ModelBuilder.TrainAndSaveModel`).
- `watchForChanges: true` — the prediction pool hot-reloads when `model.zip` changes.

```bash
# Force a retrain from people.csv on next startup
rm src/Service/MLModels/model.zip
dotnet run --project src/Service
```

---

## 3. MCP server

Stateless HTTP transport, tools from `Service.Tools.CustomerTools` (`GetWeatherAsync`).

| Item          | Value                                            |
|---------------|--------------------------------------------------|
| Endpoint      | `https://localhost:7071/mcp` (`https://localhost/mcp` in Compose) |
| Transport     | Streamable HTTP, `Stateless = true`              |
| Registered in | `AddMcp()` / `app.MapMcp("/mcp")`                |
| Tools         | `GetWeatherAsync(cityName)`                      |

### MCP Inspector

```bash
npm i @modelcontextprotocol/inspector
npx @modelcontextprotocol/inspector
```

Then in the Inspector UI: transport **Streamable HTTP**, URL `https://localhost:7071/mcp`.
The Inspector only works over HTTP(S) transport (not stdio here).

### Quick protocol check with curl

```bash
# list tools
curl -k -X POST https://localhost:7071/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'

# call the weather tool
curl -k -X POST https://localhost:7071/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"GetWeatherAsync","arguments":{"cityName":"New York"}}}'
```

---

## Packages

| Package                          | Use                                    |
|----------------------------------|----------------------------------------|
| `Microsoft.Extensions.AI`        | `IChatClient` abstraction injected into `ChatController` |
| `OllamaSharp`                    | Ollama client implementation           |
| `Microsoft.ML` / `Microsoft.Extensions.ML` | ML.NET training + `PredictionEnginePool` |
| `ModelContextProtocol.AspNetCore`| MCP server + HTTP transport             |
