import { useEffect, useRef, useState, type SubmitEvent } from 'react'
import type { ChatAnswerMessage, ChatAskAccepted, ChatCancelledMessage, ChatErrorMessage } from '../api/types'
import { API_BASE_URL } from '../api/config'
import { useAuth } from '../auth/AuthContext'
import { useChatHub } from '../hooks/useChatHub'

export function ChatPanel() {
  const { token } = useAuth()
  const { getConnection } = useChatHub(token)
  const [question, setQuestion] = useState('')
  const [answer, setAnswer] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const pendingRequestId = useRef<string | null>(null)

  // Connect once and listen for the push the background worker sends once
  // ChatService.AskAsync finishes, instead of blocking the request that asked.
  useEffect(() => {
    let cancelled = false

    getConnection().then((connection) => {
      if (cancelled) return

      connection.on('chatAnswer', (message: ChatAnswerMessage) => {
        if (message.requestId !== pendingRequestId.current) return
        setAnswer(message.answer ?? '')
        setIsLoading(false)
      })

      connection.on('chatError', (message: ChatErrorMessage) => {
        if (message.requestId !== pendingRequestId.current) return
        setError(message.message)
        setIsLoading(false)
      })

      connection.on('chatCancelled', (message: ChatCancelledMessage) => {
        if (message.requestId !== pendingRequestId.current) return
        setIsLoading(false)
      })
    })

    return () => {
      cancelled = true
    }
  }, [getConnection])

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!question.trim() || isLoading) return

    setError(null)
    setIsLoading(true)

    try {
      const connection = await getConnection()

      const response = await fetch(`${API_BASE_URL}/Chat/ask-async`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ question, connectionId: connection.connectionId }),
      })

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}.`)
      }

      const { requestId } = (await response.json()) as ChatAskAccepted
      pendingRequestId.current = requestId
      // isLoading stays true until the "chatAnswer"/"chatError"/"chatCancelled" push arrives.
    } catch {
      setError('Network error.')
      setIsLoading(false)
    }
  }

  async function handleCancel() {
    const requestId = pendingRequestId.current
    if (!requestId) return

    try {
      await fetch(`${API_BASE_URL}/Chat/ask-async/${requestId}/cancel`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
      })
      // isLoading stays true until the "chatCancelled" push confirms it.
    } catch {
      // best-effort; the request will still finish and push a result if this failed to reach the server
    }
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
        {isLoading && (
          <button type="button" onClick={handleCancel}>
            Cancel
          </button>
        )}
      </form>

      {error && <p className="error">{error}</p>}

      <div className="chat-response" aria-live="polite">
        {answer ?? 'Ask a question to see the response here.'}
      </div>
    </section>
  )
}
