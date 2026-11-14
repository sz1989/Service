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
dotnet run --project src/CLI -- people --output-format csv        # writes People.csv next to the executable
dotnet run --project src/CLI -- people --output-format markdown   # writes People.md next to the executable
dotnet run --project src/CLI -- people --output-format pdf        # writes People.pdf next to the executable
dotnet run --project src/CLI -- people --output-format csv --output-file-name ./out/staff.csv
```

| Option                      | Alias                    | Required | Default | Description |
|-----------------------------|--------------------------|----------|---------|-------------|
| `--limit <n>`               | `-l`                     | No       | `20`    | Maximum number of people to list. Minimum 1. |
| `--output-format <format>`  | `-f`, `--outputFormat`   | No       | —       | Write the list to a file in this format instead of printing it: `csv`, `markdown` or `pdf` (case-insensitive). |
| `--output-file-name <file>` | `-o`, `--outputFileName` | No       | `People.<ext>` in the executable's folder | File to write, optionally with a path (relative paths resolve against the current directory); overwritten if it exists. The parent directory must exist. Requires `--output-format`. The default name follows the format: `People.csv`, `People.md` or `People.pdf`. |

Output is tab-separated: `Id`, `Name`, `DateOfBirth` (`yyyy-MM-dd`), `Salary` (two decimals).

With `--output-format csv`, the file has the header `Id,Name,Date of birth,Annual salary` (from the `[Description]` attributes on `PersonSummary`, falling back to the property name) followed by one row per person (`DateOfBirth` as `MM/dd/yyyy`, UTF-8, CRLF line endings, invariant formatting, fields quoted per RFC 4180). An empty result writes the header only.

With `--output-format markdown`, the file is a GitHub-flavored Markdown table with the same header names, then one row per person (`DateOfBirth` as `yyyy-MM-dd`, salary with two decimals and right-aligned, `Id` right-aligned, UTF-8, LF line endings). `|` and `\` in names are backslash-escaped and line breaks become `<br>`. An empty result writes the header only.

With `--output-format pdf`, the file is a paged A4 document titled "People" with the same columns as the Markdown table (header row repeated on every page, page numbers in the footer), rendered with [QuestPDF](https://www.questpdf.com/) under its Community license (free below US$1M annual revenue; review the license for your use).

Exit codes: `0` success, `1` service or configuration error, `130` cancelled.

The database connection is read from `ConnectionStrings:Db` in `appsettings.json` (next to the executable).
