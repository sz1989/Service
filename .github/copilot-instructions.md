# Copilot Instructions

Conventions for the `System.CommandLine` console app in `src/CLI`. Test-specific rules live in `.github/instructions/tests.instructions.md` and apply to files under `tests/`.

## Tech Stack
- **Language / runtime:** C# 14, .NET 10, SDK-style projects, nullable reference types enabled.
- **App type:** Console application / hosted service.
- **CLI parsing:** `System.CommandLine` 2.0 (stable, not a beta or RC). Use the version 2 API style, not the older beta-era handler and binder patterns. Don't hand-roll parsing.
- **Hosting / DI:** `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection`.
- **Logging:** Serilog with a JSON console formatter (`Serilog.Formatting.Json.JsonFormatter` or `Serilog.Formatting.Compact`).
- **Testing:** xUnit v3 on the Microsoft Testing Platform runner (see the tests instructions file).
- Don't add NuGet packages without asking.

---

## Build, Run, Test

```bash
dotnet restore
dotnet build --configuration Release

# Help and example run
dotnet run --project src/CLI/CLI.csproj -- --help
dotnet run --project src/CLI/CLI.csproj -- greet --name Ada --output-dir ./dist

# Tests (Microsoft Testing Platform)
dotnet test --no-restore --verbosity normal

# Optional: self-contained single-file binary
dotnet publish src/CLI/CLI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

- Run `dotnet format` before finishing any change.

---

## Architecture & Dependency Injection
1. **Host orchestration:** Build the application lifecycle with `Host.CreateDefaultBuilder` or `Host.CreateApplicationBuilder`. Manage configuration, logging and services inside the host.
2. **Thin entry point:** Keep `Program.cs` thin; push logic into separate classes.
3. **Command registration:** Decouple CLI parsing from core actions.
   - Define commands with `System.CommandLine` (`Command`, `Option`, `Argument`). Each command lives in its own class.
   - Set actions with `command.SetAction(async (parseResult, ct) => ...)` and use dependency injection through an injected service layer.
   - System.CommandLine 2.0 has no `BindingContext`. Register each command as a `Command` in `AddCommands()` (`Commands/CommandServiceCollectionExtensions.cs`) so the container builds it with its constructor dependencies. `RootCommandFactory` resolves every registered `Command` from the host's `IServiceProvider` and adds it to the root.
   - Adding a command means writing the class and registering it there.
4. **Single-file compatibility:** Don't use `Assembly.GetExecutingAssembly().Location`; use `AppContext.BaseDirectory`. Avoid heavy runtime reflection or dynamic code generation that breaks single-file (or AOT) publishing.

---

## System.CommandLine Guidelines
1. **POSIX conventions:** Options use kebab-case (`--output-path`); backing C# members use camelCase or PascalCase.
2. **Strongly typed bindings:** Bind inputs with generic types (`Option<FileInfo>`, `Option<int>`).
3. **Native validation:** Use built-in rules (`option.AcceptExistingOnly()`) or `option.Validators.Add(...)` instead of parsing strings inside handlers.
4. **Standard behavior:** Support `--help` and `--version`. Write normal output to stdout and errors to stderr. Never prompt for input unless the command is explicitly interactive.
5. **Friendly failures:** Argument errors print structured, friendly usage output.

---

## Structured Logging (Serilog)
1. **Host integration:** Fully replace the native .NET logging providers by chaining `.UseSerilog((context, services, configuration) => ...)` onto the host builder.
2. **JSON console output:** Emit machine-readable JSON to the console (e.g. `.WriteTo.Console(new JsonFormatter())` or `.WriteTo.Console(new CompactJsonFormatter())`).
3. **Semantic logging:** Never use string interpolation or raw formatting in log calls. Use message templates with named placeholders, e.g. `_logger.LogInformation("Processing batch {BatchId} for user {UserId}", batchId, userId);`.
4. Use `ILogger` for diagnostics, not `Console.WriteLine`.

---

## Async, Lifecycle & Error Handling
1. **Async execution:** Bind command actions with async delegates. Every async method accepts a `CancellationToken` and forwards it to downstream async calls. Async methods end in `Async`.
2. **No async-over-sync:** Never use `.Result`, `.Wait()` or `.GetAwaiter().GetResult()`.
3. **Graceful cancellation:** Long-running loops and workers must observe the `CancellationToken` so `Ctrl+C` and `SIGTERM` don't corrupt state.
4. **Exit codes:** Return 0 on success. Catch business exceptions explicitly and map them to precise non-zero exit codes. Never let raw stack traces reach the user's terminal.
5. **Error output:** Use `Console.Error` exclusively for failure messages and exceptions.
6. **Exception hygiene:**
   - No catch-all `catch (Exception)` except at the application's global root, where it logs and maps to an exit code.
   - No empty `catch` blocks; every caught exception is handled, logged, or rethrown.
   - Don't swallow exceptions; surface a clear message and a non-zero exit code.
7. **Guard clauses:** Validate public method inputs at the entry point (e.g. `ArgumentNullException.ThrowIfNull`).

---

## Coding Conventions
- PascalCase for types and methods, camelCase for locals and parameters, `_camelCase` for private fields.
- Follow the repo's `.editorconfig` (file-scoped namespaces, naming, indentation).
- Prefer records for immutable data.
- Use the most restrictive access modifier possible (`private` or `internal` before `public`).
- Keep CLI parsing separate from business logic; no `System.CommandLine` types leak into domain services.
- Create interfaces only for multiple implementations or test mocking; no speculative one-to-one interfaces.
- No secrets or environment-specific configuration in code; use environment variables or a secure configuration provider.

---

## Definition of Done

### Code Quality
- [ ] Compiles with zero warnings (warnings as errors is enforced).
- [ ] Follows `.editorconfig`.
- [ ] No dead code: unused `using` directives, commented-out blocks and unreferenced locals are removed.

### Architecture
- [ ] Types and members use the most restrictive access modifier.
- [ ] CLI parsing is separate from business logic.
- [ ] No speculative interfaces.
- [ ] New commands are registered in `AddCommands()`.

### Async & Logging
- [ ] Every async method accepts and forwards a `CancellationToken`.
- [ ] No async-over-sync.
- [ ] Serilog is hooked into the host and log statements use message templates, never string interpolation.

### Error Handling
- [ ] No catch-all or empty `catch` blocks (except the global root handler).
- [ ] Public inputs are validated with guard expressions.
- [ ] Failures map to non-zero exit codes with no raw stack traces.

### Deployment
- [ ] No hardcoded secrets or environment-specific configuration.
- [ ] Single-file-friendly paths (`AppContext.BaseDirectory`).
- [ ] No reflection or code generation that breaks single-file or AOT publishing.
