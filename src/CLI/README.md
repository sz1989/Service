# CLI

Command-line tool for the Service solution.

```bash
dotnet run --project src/CLI -- <command> [options]
```

Use `--help` on the root or any command for built-in usage, and `--version` for the version.

## Commands

### `greet`

Greet someone by name.

```bash
dotnet run --project src/CLI -- greet --name Alice
dotnet run --project src/CLI -- greet -n Alice -o ./out
```

| Option                | Alias | Required | Description                                                          |
|-----------------------|-------|----------|----------------------------------------------------------------------|
| `--name <name>`       | `-n`  | Yes      | Name of the person to greet. Must not be blank.                      |
| `--output-dir <dir>`  | `-o`  | No       | Existing directory to write `greeting.txt` into. Omit to print only. |

Exit codes: `0` success, `1` greeting error, `130` cancelled.

### `people`

List people from the database.

```bash
dotnet run --project src/CLI -- people
dotnet run --project src/CLI -- people --limit 5
```

| Option            | Alias | Required | Default | Description                                  |
|-------------------|-------|----------|---------|----------------------------------------------|
| `--limit <n>`     | `-l`  | No       | `20`    | Maximum number of people to list. Minimum 1. |

Output is tab-separated: `Id`, `Name`, `DateOfBirth` (`yyyy-MM-dd`), `Salary` (two decimals). Prints `No people found.` when the result is empty.

Exit codes: `0` success, `1` service or configuration error, `130` cancelled.

The database connection is read from `ConnectionStrings:Db` in `appsettings.json` (next to the executable).
