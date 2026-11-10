# CLI (src/CLI)

Conventions for the `System.CommandLine` console app and its tests in `tests/CLI.Tests`.
Applies in addition to the root `AGENTS.md`.

## Tech Stack Context
- **Language:** C# 14 / .NET 10
- **App Type:** Console Application / Hosted Service
- **Logging:** Serilog with `Serilog.Formatting.Json.JsonFormatter` (or `Serilog.Expressions` / `Serilog.Formatting.Compact`)
- **Key Libraries:** `System.CommandLine`, `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection`
- **Testing Framework:** xUnit v3 (Targeting the Microsoft Testing Platform runner)

---

## Architecture & Dependency Injection
1. **Host Orchestration:** Build application lifecycles natively using `Host.CreateDefaultBuilder` or `Host.CreateApplicationBuilder`. Manage configurations, logging, and services inside the host infrastructure.
2. **Command Registration:** Decouple CLI parsing from core actions. Commands must be defined via `System.CommandLine` (`Command`, `Option`, `Argument`) and actions (`command.SetAction(async (parseResult, ct) => ...)`) must use dependency injection through an injected service layer. System.CommandLine 2.0 has no `BindingContext`; register each command in `AddCommands()` (`Commands/CommandServiceCollectionExtensions.cs`) as a `Command` so the container builds it with its constructor dependencies; `RootCommandFactory` resolves every registered `Command` from the host's `IServiceProvider` and adds it to the root. Adding a command means writing the class and registering it there.
3. **Optional Single-File Compatibility:** To support optional Single-File compilation (`-p:PublishSingleFile=true`), do not rely on dynamic path discovery techniques like `Assembly.GetExecutingAssembly().Location`. Use `AppContext.BaseDirectory` instead. Avoid heavy runtime reflection or dynamic code generation that fails single-file packaging.

---

## Structured Logging Framework (Serilog)
1. **Host Integration:** Fully replace the native .NET logging providers by chaining `.UseSerilog((context, services, configuration) => ...)` onto the host builder initialization block.
2. **JSON Console Output:** Enforce machine-readable structured streams for container and telemetry collection architectures by outputting console targets explicitly via a JSON formatter (e.g., `.WriteTo.Console(new JsonFormatter())` or `.WriteTo.Console(new CompactJsonFormatter())`).
3. **Semantic Logging:** Never inject raw string formatting or string interpolation into log streams. Always provide structured parameter names as placeholders to preserve message query indexing (e.g., `_logger.LogInformation("Processing batch {BatchId} for user {UserId}", batchId, userId);`).

---

## System.CommandLine Guidelines
1. **POSIX Conventions:** Keep tokens standard. Options use kebab-case (`--output-path`), while backing C# properties and variable definitions map to camelCase or PascalCase.
2. **Strongly Typed Bindings:** Bind input arguments directly using generic structures (`Option<FileInfo>`, `Option<int>`).
3. **Native Rules & Input Validation:** Use built-in token rules (e.g., `option.AcceptExistingOnly()`) or hook into `option.Validators.Add(...)` instead of running string parses inside command handlers.

---

## Async, Lifecycle & Error Handling
1. **Asynchronous Execution:** Bind command actions via async method delegates. Ensure all operations pass a valid `CancellationToken`.
2. **Graceful Cancellation:** Long-running loop routines or workers must dynamically listen to the `CancellationToken` provided by the execution context to capture `Ctrl+C` or `SIGTERM` without corrupting state.
3. **Exit Codes & Exception Safety:** Catch internal business exceptions explicitly, mapping failures to precise, non-zero app exit codes. Prevent raw stack traces from bleeding into user terminals. Use `Console.Error` exclusively for telemetry, failure logs, and exceptions.

---

## xUnit v3 Testing & Parallelization Rules
1. **Assertion Framework:** Use standard xUnit v3 assertions only (`Assert.Equal`, `Assert.Null`, `Assert.ThrowsAsync`). FluentAssertions is strictly forbidden.
2. **Assert Argument Order:** Always follow the xUnit standard convention: `Assert.Equal(expected, actual)`, placing the expected outcome value first.
3. **Parallel Execution Architecture:** All xUnit v3 tests execute in parallel by default across classes. For integration tests sharing stateful dependencies (e.g., databases, specific file access, shared host state), explicitly isolate them using the `[Collection]` attribute to group them onto non-parallelized worker threads.
4. **Lifecycle & Cancellation:** Leverage xUnit v3 `ValueTask` models and `IAsyncLifetime`. Utilize `TestContext.Current.CancellationToken` directly to manage cancellation responsiveness within integration tests.

---

## Executable Environment Commands

### Build & Restore
```bash
dotnet restore
dotnet build --configuration Release
```

### Publish Single-File Executable (Optional)
```bash
# Publish as a self-contained, standalone single-file binary for a target runtime
dotnet publish src/CLI/CLI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Run Project
```bash
# Display built-in help documentation
dotnet run --project src/CLI/CLI.csproj -- --help

# Run specific commands with flags
dotnet run --project src/CLI/CLI.csproj -- greet --name Ada --output-dir ./dist
```

### Test Execution
```bash
# Execute tests through the Microsoft Testing Platform (MTP) wrapper
dotnet test --no-restore --verbosity normal
```

---

## Definition of Done (DoD) Checklist

### Code Quality & Formatting
- [ ] Code compiles with zero warnings (Warnings as Errors is enforced).
- [ ] Code follows the repo's `.editorconfig` rules (e.g., file-scoped namespaces, naming, indentation).
- [ ] No dead code: unused `using` directives, commented-out code blocks, and unreferenced locals are removed.

### Architecture & Pattern Guardrails
- [ ] Types and members use the most restrictive access modifier possible (`private` or `internal` before `public`).
- [ ] CLI parsing stays separate from business logic; no `System.CommandLine` types leak into domain services.
- [ ] Interfaces exist only for multiple implementations or test mocking; no speculative one-to-one interface bloat.

### Asynchronous & Performance Safety
- [ ] Every async method accepts a `CancellationToken` and forwards it to downstream async calls.
- [ ] No async-over-sync: `.Result`, `.Wait()` and `.GetAwaiter().GetResult()` are not used.
- [ ] Serilog is fully hooked into the host context, and logging parameters preserve structured metadata properties.
- [ ] Log statements use message templates (`"User {UserId} logged in"`), never string interpolation.

### Error Handling
- [ ] No catch-all `catch (Exception)` except at the application's global root, where it logs and maps to an exit code.
- [ ] No empty `catch` blocks; every caught exception is handled, logged, or rethrown.
- [ ] Public method inputs are validated at the entry point with guard expressions (e.g., `ArgumentNullException.ThrowIfNull`).
- [ ] Command arguments parse gracefully with structured, friendly usage printouts on failure.

### Testing & Verification
- [ ] Tests cover edge cases and boundary inputs (null, empty, negative), not just the happy path.
- [ ] No references to third-party fluent testing frameworks are introduced.
- [ ] Assertions contain no `if`/loops and place the expected value first (`Assert.Equal(expected, actual)`).
- [ ] Stateful integration tests (database, files, system clock, shared host state) are isolated in sequential Test Collections and reset after every run.

### Deployment & Environment Compliance
- [ ] No secrets or environment-specific configuration are hardcoded; they come from environment variables or a secure configuration provider.
- [ ] Assembly locations use single-file friendly paths (`AppContext.BaseDirectory`).
- [ ] No dynamic runtime reflection or code generation that would break Single-File (or AOT) publishing.
