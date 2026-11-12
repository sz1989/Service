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
dotnet run --project src/CLI -- people --csv ./people.csv
dotnet run --project src/CLI -- people --csv        # writes people.csv next to the executable
dotnet run --project src/CLI -- people --markdown ./people.md
dotnet run --project src/CLI -- people --markdown   # writes people.md next to the executable
```

| Option            | Alias | Required | Default | Description                                                                              |
|-------------------|-------|----------|---------|------------------------------------------------------------------------------------------|
| `--limit <n>`     | `-l`  | No       | `20`    | Maximum number of people to list. Minimum 1.                                             |
| `--csv <file>`    | `-c`  | No       | —       | Write the list to this CSV file (overwritten if it exists) instead of printing it. The parent directory must exist. Pass `--csv` with no file to write `people.csv` in the executable's folder. |
| `--markdown <file>` | `-m` | No      | —       | Write the list to this Markdown file (overwritten if it exists) instead of printing it. The parent directory must exist. Pass `--markdown` with no file to write `people.md` in the executable's folder. Can be combined with `--csv`. |

Output is tab-separated: `Id`, `Name`, `DateOfBirth` (`yyyy-MM-dd`), `Salary` (two decimals).

With `--csv`, the file has the header `Id,Name,Date of birth,Annual salary` (from the `[Description]` attributes on `PersonSummary`, falling back to the property name) followed by one row per person (`DateOfBirth` as `MM/dd/yyyy`, UTF-8, CRLF line endings, invariant formatting, fields quoted per RFC 4180). An empty result writes the header only.

With `--markdown`, the file is a GitHub-flavored Markdown table with the same header names, then one row per person (`DateOfBirth` as `yyyy-MM-dd`, salary with two decimals and right-aligned, `Id` right-aligned, UTF-8, LF line endings). `|` and `\` in names are backslash-escaped and line breaks become `<br>`. An empty result writes the header only.

Exit codes: `0` success, `1` service or configuration error, `130` cancelled.

The database connection is read from `ConnectionStrings:Db` in `appsettings.json` (next to the executable).
