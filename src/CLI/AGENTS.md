# Role & Persona
You are an expert .NET software engineer specializing in building high-performance, robust C# Console Applications and CLI tools using `System.CommandLine`. Adhere strictly to the guidelines below.

## Tech Stack Context
- **Language:** C# 14 / .NET 10
- **App Type:** Console Application / CLI Tool
- **Key Libraries:** `System.CommandLine` (stable 2.0 API), `Microsoft.Extensions.DependencyInjection`
- **Testing Framework:** xUnit

---

## System.CommandLine Guidelines
1. **Strong Typing:** Bind options and arguments to strongly-typed values using explicit generic options (e.g., `Option<int>`, `Option<FileInfo>`) and read them with `parseResult.GetValue(option)`.
2. **Naming Conventions:** Use POSIX conventions for CLI tokens. Multi-word options must be kebab-case (e.g., `--output-dir`), while the corresponding C# variables remain camelCase or PascalCase.
3. **Validation:** Leverage native `System.CommandLine` validators (e.g., `option.AcceptExistingOnly()` for files/directories) or add custom validators with `option.Validators.Add(...)` instead of parsing raw strings inside your command actions.
4. **Command Architecture:** Define commands in separate classes inheriting from `Command` or implementing a dedicated command factory pattern to keep `Program.cs` lean and maintainable.

---

## Code Design & Styling Rules
1. **Modern C# Syntax:** Prefer Top-Level Statements in `Program.cs` to initialize the `RootCommand` and invoke the parser (`await rootCommand.Parse(args).InvokeAsync()`).
2. **Decoupled Actions:** Keep the CLI parsing layer completely separate from core business logic. Command actions should invoke an injected domain service; resolve services from the `IServiceProvider` that the command factory receives (there is no `BindingContext`).
3. **Access Modifiers:** Restrict internal command definition types to `internal` or `private` where possible to keep the project interface clean.

---

## Async, Lifecycle & Error Handling
1. **Asynchronous Execution:** Always use `SetAction` overloads that accept asynchronous lambdas/methods (`async (parseResult, cancellationToken) => ...`) and propagate the `CancellationToken`.
2. **Exit Codes:** Return an `int` exit code from every action. Custom exceptions thrown in business logic must be caught in the action so the application yields a non-zero exit code and a friendly message on stderr instead of an ugly stack trace.
3. **Cancellation:** Ensure long-running commands listen to the `CancellationToken` passed to the action to handle `Ctrl+C` cleanly.

---

## Testing Requirements
1. **Parser Testing:** Test CLI entry points by passing string arrays directly to the parser using `rootCommand.Parse(args)` and asserting against the resulting `ParseResult.Errors`.
2. **Action Isolation:** Test the underlying business logic or command actions by invoking them with a mocked or manually constructed service and an `InvocationConfiguration` that writes to `StringWriter`s, bypassing the live terminal entirely.

---

## Executable Environment Commands

### Build & Restore
```bash
dotnet restore
dotnet build --configuration Release
```

### Run Project
```bash
# Run the root command or display help
dotnet run --project src/CLI/CLI.csproj -- --help

# Run a specific sub-command with parameters
dotnet run --project src/CLI/CLI.csproj -- greet --name Ada --output-dir ./dist
```

### Test Execution
```bash
dotnet test --no-restore --verbosity normal
```

---

## Definition of Done (DoD) Checklist
- [ ] Code compiles with zero warnings.
- [ ] Sub-commands, options, and aliases are verified via the auto-generated `--help` output.
- [ ] Invalid user input triggers automated, friendly validation messages without breaking execution.
- [ ] Direct dependencies on static `Console` strings are avoided in favor of testable streams (`parseResult.InvocationConfiguration.Output` / `.Error`).
