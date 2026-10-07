---
name: add-service-feature
description: Use when adding a new backend feature/endpoint to this .NET service — scaffolds the BAL interface+impl, DI registration, controller, and test following this repo's existing conventions. Triggers on "add an endpoint", "new feature", "add a service for X".
---

# Add a service feature

Follow this repo's layering: `Controllers` → `BAL/<Feature>` → `Data`. Don't skip BAL
and call `Data`/`AppDbContext` straight from a controller.

## Steps

1. **BAL folder**: create `src/Service/BAL/<Feature>/I<Feature>Service.cs` and
   `<Feature>Service.cs`. Interface first, then the impl using a primary constructor
   for injected dependencies, e.g.:

   ```csharp
   public class <Feature>Service(ISomeDependency dep) : I<Feature>Service
   {
       public async Task<Result> DoThingAsync(...) { ... }
   }
   ```

2. **Register in DI**: add the registration to
   `src/Service/Extensions/ServiceCollectionExtensions.cs` — do NOT register services
   inline in `Program.cs`. Use the narrowest lifetime that works (`AddSingleton` for
   connection-holding/queue types, `AddScoped`/`AddTransient` otherwise).

3. **Controller**: add an action to an existing controller, or create
   `src/Service/Controllers/<Feature>Controller.cs`. Inject `I<Feature>Service`, never
   the concrete class. If this is a breaking change to an existing versioned endpoint,
   add it under a new `Controllers/vN/` folder instead of mutating the current version
   (see `v1/PersonController.cs` vs `v2/PersonController.cs`).

4. **Auth**: default to requiring a bearer token; only mark `[AllowAnonymous]` when the
   endpoint genuinely needs to be public (see `Authentication/ApiKeyAuthenticationHandler.cs`
   for the API-key path).

5. **Tests**: add `tests/Service.Tests/BAL/<Feature>ServiceTests.cs` using
   Moq/NSubstitute for dependencies — mirror an existing test in that folder for style.
   Run:

   ```bash
   dotnet test ./Service.slnx --collect:"XPlat Code Coverage"
   ```

6. **Docs**: if the endpoint is user-facing, add a row to `doc/ServiceRef.md`'s
   endpoint table.

## Don't

- Don't put business logic in `Data/` repositories — they're persistence-only.
- Don't new-up services inside controllers; always go through DI.
- Don't add a NuGet package version directly in a `.csproj` — add it to
  `Directory.Packages.props` (central package management).
