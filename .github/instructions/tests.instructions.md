---
applyTo: "tests/**"
---

# Test Instructions (xUnit v3)

Applies to the test projects under `tests/` (e.g. `tests/CLI.Tests`), in addition to the repo-wide instructions.

## Framework
- Use xUnit v3 APIs and packages on the Microsoft Testing Platform runner, not the older v2 style.
- Run tests with `dotnet test --no-restore --verbosity normal`.

## Assertions
1. Use standard xUnit v3 assertions only (`Assert.Equal`, `Assert.Null`, `Assert.ThrowsAsync`). FluentAssertions and other third-party fluent libraries are forbidden.
2. Always put the expected value first: `Assert.Equal(expected, actual)`.
3. No `if` statements or loops inside assertions.

## Structure & Naming
- One test class per class under test.
- Name tests `Method_Scenario_ExpectedResult`.
- Test command logic directly rather than spawning the process.

## Parallelism & State
- xUnit v3 runs test classes in parallel by default.
- Isolate integration tests that share state (databases, files, system clock, shared host state) with the `[Collection]` attribute so they run sequentially, and reset that state after every run.

## Lifecycle & Cancellation
- Use `ValueTask` models and `IAsyncLifetime`.
- Use `TestContext.Current.CancellationToken` to keep integration tests responsive to cancellation.

## Coverage
- Cover edge cases and boundary inputs (null, empty, negative), not just the happy path.

## Test Checklist
- [ ] Edge and boundary cases are covered.
- [ ] No third-party fluent testing frameworks are introduced.
- [ ] Assertions place the expected value first and contain no `if` or loops.
- [ ] Stateful integration tests are isolated in sequential collections.
