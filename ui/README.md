# React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some Oxlint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the Oxlint configuration

If you are developing a production application, we recommend enabling type-aware lint rules by installing `oxlint-tsgolint` and editing `.oxlintrc.json`:

```json
{
  "$schema": "./node_modules/oxlint/configuration_schema.json",
  "plugins": ["react", "typescript", "oxc"],
  "options": {
    "typeAware": true
  },
  "rules": {
    "react/rules-of-hooks": "error",
    "react/only-export-components": ["warn", { "allowConstantExport": true }]
  }
}
```

See the [Oxlint rules documentation](https://oxc.rs/docs/guide/usage/linter/rules) for the full list of rules and categories.

## Testing

`npm run test` runs `tsc -b` first (via the `pretest` script in `package.json`), then `vitest run`. If the type-check fails, the tests don't run.

`tsc -b` runs TypeScript in build mode. Plain `tsc` compiles one project, but `-b` follows project references (`tsconfig.app.json` and `tsconfig.node.json`) and builds each one in dependency order. Both projects set `noEmit`, so it only type-checks. Results are cached in `node_modules/.tmp/*.tsbuildinfo`, so repeat runs are fast.

`npm run test:watch` does not run the type-check, because npm only runs `pre<name>` for the exact script name.
