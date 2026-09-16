import { useState, type SubmitEvent } from 'react'
import { useAuth } from '../auth/AuthContext'
import { useFetch } from '../hooks/useFetch'

export function ChatPanel() {
  const { token } = useAuth()
  const [question, setQuestion] = useState('')
  const { data: answer, error, isLoading, execute } = useFetch<string>()

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!question.trim()) return

    await execute(`/Chat?question=${encodeURIComponent(question)}`, {
      headers: { Authorization: `Bearer ${token}` },
    }).catch(() => {
      // error state is already surfaced via useFetch
    })
  }

  return (
    <section className="dashboard-card chat-panel">
      <form className="chat-form" onSubmit={handleSubmit}>
        <input
          type="text"
          value={question}
          onChange={(event) => setQuestion(event.target.value)}
          placeholder="Ask the assistant a question…"
          aria-label="Question for Ollama"
          required
        />
        <button type="submit" disabled={isLoading || !question.trim()}>
          {isLoading ? 'Asking…' : 'Ask Ollama'}
        </button>
      </form>

      {error && <p className="error">{error}</p>}

      <div className="chat-response" aria-live="polite">
        {answer ?? 'Ask a question to see the response here.'}
      </div>
    </section>
  )
}
