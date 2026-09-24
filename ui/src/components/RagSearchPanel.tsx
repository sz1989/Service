import { useState, type SubmitEvent } from 'react'
import type { DocumentMatch } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { useFetch } from '../hooks/useFetch'
import { IngestPanel } from './IngestPanel'

export function RagSearchPanel() {
  const { token } = useAuth()
  const [query, setQuery] = useState('')
  const [showSeedPanel, setShowSeedPanel] = useState(false)
  const { data: matches, error, isLoading, execute } = useFetch<DocumentMatch[]>()

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!query.trim()) return

    await execute(`/Chat/search?query=${encodeURIComponent(query)}&topK=5`, {
      headers: { Authorization: `Bearer ${token}` },
    }).catch(() => {
      // error state is already surfaced via useFetch
    })
  }

  return (
    <section className="dashboard-card search-panel">
      <h2>Document Search</h2>

      <button
        type="button"
        className="button-secondary"
        onClick={() => setShowSeedPanel((current) => !current)}
      >
        {showSeedPanel ? 'Hide seed panel' : 'Seed embedding data'}
      </button>

      {showSeedPanel && <IngestPanel />}

      <form className="chat-form" onSubmit={handleSubmit}>
        <input
          type="text"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search ingested documents…"
          aria-label="Document search query"
          required
        />
        <button type="submit" disabled={isLoading || !query.trim()}>
          {isLoading ? 'Searching…' : 'Search'}
        </button>
      </form>

      {error && <p className="error">{error}</p>}

      <table>
        <thead>
          <tr>
            <th>Text</th>
            <th>Distance</th>
          </tr>
        </thead>
        <tbody>
          {(matches ?? []).map((match) => (
            <tr key={match.id}>
              <td>{match.text}</td>
              <td>{match.distance.toFixed(4)}</td>
            </tr>
          ))}
          {matches !== null && matches.length === 0 && (
            <tr>
              <td colSpan={2}>No matches found.</td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  )
}
