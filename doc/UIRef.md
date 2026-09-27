# UI Reference

Commands and reference for the React UI (`ui/`): Vite + React 19 + TypeScript, React Router for
routing, JWT auth against the API, and Vitest + React Testing Library for unit tests.

All commands below are run from the `ui/` directory unless noted otherwise.

---

## 1. Setup

```bash
npm install
```

## 2. Dev server

```bash
npm run dev   # https://localhost:3000 if certs exist, otherwise http://localhost:3000
```

`vite.config.ts` runs on port `3000` (`strictPort: true`) and serves over HTTPS when
`ui/certs/localhost.pem` / `localhost.key` exist.

### Generate the dev HTTPS certificate

```bash
./generate-cert.sh
```

Exports the already-trusted `dotnet dev-certs` certificate to `ui/certs/` so the Vite dev
server uses the same trusted cert as the backend (see `src/Service/generate-cert.sh`) — no
untrusted-certificate warning in the browser.

## 3. Build

```bash
npm run build   # tsc -b && vite build
```

Type-checks the project (`tsc -b`), then produces a production bundle in `ui/dist`.

```bash
npm run preview   # serve the built dist/ locally
```

## 4. Lint

```bash
npm run lint   # oxlint
```

## 5. Test

```bash
npm test          # vitest run — single run, used in CI
npm run test:watch  # vitest — watch mode
```

- Framework: [Vitest](https://vitest.dev/) (config in `vite.config.ts`, `test` block) with
  `environment: 'jsdom'` and `globals: true`.
- Setup file: `src/test/setup.ts` (imports `@testing-library/jest-dom/vitest` matchers).
- Component tests use `@testing-library/react` (`render`, `screen`) and
  `@testing-library/user-event` for interactions — see `src/components/PersonActions.test.tsx`
  for the pattern.
- Test files live alongside the component they cover, named `*.test.tsx`.

CI runs `npm test` before `npm run build` — see
[`.github/workflows/react-build.yml`](../.github/workflows/react-build.yml).

---

## Project structure

| Path                        | Purpose                                              |
|------------------------------|-------------------------------------------------------|
| `src/App.tsx`                | Route table (`react-router-dom`)                      |
| `src/main.tsx`                | App entry point                                       |
| `src/pages/`                  | `LoginPage`, `DashboardPage`                          |
| `src/components/`             | `PersonList`, `PersonActions`, `ChatPanel`, `IngestPanel`, `RagSearchPanel`, `ErrorBoundary`, `ProtectedRoute` |
| `src/auth/AuthContext.tsx`    | JWT auth context — `login`/`logout`, token in `localStorage` (`authToken`) |
| `src/hooks/useFetch.ts`       | Fetch wrapper hook used by auth + panels               |
| `src/api/types.ts`            | Shared API response types (`Person`, `LoginResponse`, `DocumentMatch`) |
| `src/test/setup.ts`           | Vitest setup (jest-dom matchers)                       |

## Routes

| Path         | Component        | Notes                                   |
|--------------|------------------|------------------------------------------|
| `/login`     | `LoginPage`      | Posts to `/Auth/login`, stores JWT       |
| `/dashboard` | `DashboardPage`  | Wrapped in `ProtectedRoute`              |
| `/`, `*`     | —                | Redirect to `/dashboard`                 |

## Auth

`AuthContext` calls `POST /Auth/login` with `{ username, password }`, stores the returned
JWT in `localStorage` under `authToken`, and exposes `isAuthenticated` / `logout`.
`ProtectedRoute` redirects to `/login` when there's no token.

---

## Packages

| Package                              | Use                                        |
|----------------------------------------|---------------------------------------------|
| `react`, `react-dom`                    | UI runtime                                  |
| `react-router-dom`                      | Client-side routing                         |
| `vite`, `@vitejs/plugin-react`          | Dev server / build                          |
| `typescript`                            | Type checking (`tsc -b`)                    |
| `oxlint`                                 | Linting                                     |
| `vitest`, `jsdom`                        | Unit test runner + DOM environment          |
| `@testing-library/react`                 | Render/query React components in tests      |
| `@testing-library/user-event`            | Simulated user interactions in tests        |
| `@testing-library/jest-dom`              | Extra DOM matchers (`toBeDisabled()`, etc.) |

## Related

- [ServiceRef.md](ServiceRef.md) — Docker / backend reference commands.
- [AiRef.md](AiRef.md) — AI-related backend commands (Ollama chat, ML.NET, MCP).
