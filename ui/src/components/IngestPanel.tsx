import { useState, type SubmitEvent } from 'react'
import { useAuth } from '../auth/AuthContext'
import { useFetch } from '../hooks/useFetch'

const DEFAULT_TEXT = [
  'Minimal APIs simplify endpoint development in ASP.NET Core.',
  'Entity Framework Core streamlines database operations.',
  'Background services process long-running tasks efficiently.',
  'Dependency injection improves application maintainability.',
].join('\n')

export function IngestPanel() {
  const { token } = useAuth()
  const [text, setText] = useState(DEFAULT_TEXT)
  const { error, isLoading, execute } = useFetch<void>()
  const [ingestedCount, setIngestedCount] = useState<number | null>(null)

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()

    const texts = text
      .split('\n')
      .map((line) => line.trim())
      .filter((line) => line.length > 0)

    if (texts.length === 0) return

    await execute('/Chat/ingest', {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(texts),
    })
      .then(() => {
        setIngestedCount(texts.length)
        setText('')
      })
      .catch(() => {
        // error state is already surfaced via useFetch
      })
  }

  return (
    <section className="dashboard-card ingest-panel">
      <h2>Ingest Documents</h2>
      <form onSubmit={handleSubmit}>
        <textarea
          value={text}
          onChange={(event) => {
            setText(event.target.value)
            setIngestedCount(null)
          }}
          placeholder={'One document per line…\ne.g.\nMinimal APIs simplify endpoint development in ASP.NET Core.\nEntity Framework Core streamlines database operations.'}
          aria-label="Documents to ingest, one per line"
          rows={6}
          required
        />
        <button type="submit" disabled={isLoading || !text.trim()}>
          {isLoading ? 'Ingesting…' : 'Ingest'}
        </button>
      </form>

      {error && <p className="error">{error}</p>}
      {ingestedCount !== null && !error && (
        <p className="ingest-success">Ingested {ingestedCount} document(s).</p>
      )}
    </section>
  )
}
