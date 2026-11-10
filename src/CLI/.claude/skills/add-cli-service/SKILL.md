---
name: add-cli-service
description: Use when adding a new service to the CLI (src/CLI) that fetches data — scaffolds the interface, implementation, exception, DI registration, and tests following the CLI's conventions. Triggers on "add a CLI service", "new CLI command that reads data", "add a service for X to the CLI".
---

# Add a CLI service

The CLI keeps parsing separate from logic: `Commands` (System.CommandLine) → `Services`
(business logic, data access). No `System.CommandLine` types in a service.
See `src/CLI/AGENTS.md` for the full rules.

## Steps

1. **Interface**: `src/CLI/Services/I<Feature>Service.cs`, `internal`. Every async method
   takes a `CancellationToken`. Document in XML docs which exception it throws
   (`GenerateDocumentationFile` is on). Mirror `IGreetingService.cs`.

2. **Exception**: `src/CLI/Services/<Feature>ServiceException.cs`, a one-line
   `internal sealed class X(string message) : Exception(message);`
   The command catches this and maps it to a non-zero exit code.

3. **Implementation**: `<Feature>Service.cs`, `internal sealed`, primary constructor,
   e.g. `(NpgsqlDataSource dataSource, ILogger<<Feature>Service> logger)`.
   - Call `ct.ThrowIfCancellationRequested()` first.
   - Catch the specific driver exception (e.g. `NpgsqlException`) and rethrow as the
     service exception with the original as inner exception. Never `catch (Exception)`.
   - Let `OperationCanceledException` propagate (the command maps it to exit 130).
   - Log with message templates (`"Read {Count} rows"`), never string interpolation.

4. **DI**: register in `src/CLI/Program.cs` inside `ConfigureServices`. Services are
   `AddTransient<IFooService, FooService>()`; shared infrastructure such as
   `NpgsqlDataSource` stays a singleton. Anything needing config or a connection must be
   created lazily with a factory (`AddSingleton(sp => ...)`) so `--help` works with
   nothing configured. Read config from `context.Configuration`; never hardcode secrets.

5. **Command**: add a `Commands/<Feature>Command.cs` taking the service in its
   constructor (use a primary constructor when the body needs no setup logic; commands
   that add options/validators in the body use an explicit one, like `GreetCommand`).
   Register it in `AddCommands()` (`Commands/CommandServiceCollectionExtensions.cs`) as
   `services.AddTransient<Command, <Feature>Command>()`. `RootCommandFactory` resolves
   every registered `Command`, so do not edit the factory.
   - If the service needs config or a database, take `Func<I<Feature>Service>` instead
     and register `services.AddTransient<Func<I<Feature>Service>>(sp =>
     sp.GetRequiredService<I<Feature>Service>)` in `AddCommands()`, so the service is
     only resolved when the command runs and `--help` still works. Mirror `PeopleCommand`.
   - Write output via `parseResult.InvocationConfiguration.Output`, errors via `.Error`.
     Status messages go through `ILogger<T>` (stderr JSON), not the output writer.
   - Return 0 on success, 1 on the service exception, 130 on cancellation.

6. **Tests** in `tests/CLI.Tests`, mirroring `GreetCommandHandlerTests.cs`:
   - Command tests with `Substitute.For<I<Feature>Service>()`: success, service
     exception, cancellation, empty result.
   - Extend `ServiceCollectionStub`: add an optional `I<Feature>Service?` parameter and
     register it with `.AddTransient(_ => service ?? Substitute.For<I<Feature>Service>())`.
     The stub builds a real `ServiceCollection` and calls `AddCommands()`, so the new
     command is resolved exactly as in production.
   - Use plain xUnit assertions with the expected value first. No FluentAssertions.

   ```bash
   dotnet test --no-restore --verbosity normal
   ```

7. **Usings**: if a `using` ends up in more than one `.cs` file of a project, move it to
   that project's `Using.cs` as `global using` (alphabetical, `System.*` not first) and
   delete the per-file copies. See the root `AGENTS.md`.

8. **Verify**: `dotnet build src/CLI` must produce 0 warnings (warnings are errors),
   and `dotnet run --project src/CLI -- --help` must work with no env vars set.

## Don't

- Don't put `System.CommandLine` types in a service.
- Don't instantiate commands by hand or add them to `RootCommandFactory`; register them
  in `AddCommands()`.
- Don't register eagerly something that throws when config is missing.
- Don't add a package version in `CLI.csproj`; add it to `Directory.Packages.props`.
- Don't use `.Result` / `.Wait()` or empty `catch` blocks.
