import { existsSync, readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

const certPath = resolve(import.meta.dirname, 'certs/localhost.pem')
const keyPath = resolve(import.meta.dirname, 'certs/localhost.key')
const hasCert = existsSync(certPath) && existsSync(keyPath)

if (!hasCert) {
  console.warn('No dev cert found in ./certs — run ./generate-cert.sh for HTTPS. Falling back to HTTP.')
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    strictPort: true,
    https: hasCert ? { cert: readFileSync(certPath), key: readFileSync(keyPath) } : undefined,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    globals: true,
  },
})
