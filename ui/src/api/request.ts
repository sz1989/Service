import { ApiError } from './ApiError'
import { API_BASE_URL } from './config'

export interface RequestOptions extends Omit<RequestInit, 'body' | 'headers'> {
  /**
   * Objects and arrays are sent as JSON, with a Content-Type: application/json header.
   * A string, FormData, Blob, URLSearchParams or ArrayBuffer is sent as it is, so callers that
   * already serialized their body (and set their own Content-Type) are not encoded twice.
   */
  body?: unknown
  headers?: Record<string, string>
}

function isRawBody(body: unknown): body is BodyInit {
  return (
    typeof body === 'string' ||
    body instanceof FormData ||
    body instanceof Blob ||
    body instanceof URLSearchParams ||
    body instanceof ArrayBuffer
  )
}

/**
 * Calls the API and returns the parsed response: JSON when the server sends JSON, otherwise text,
 * or undefined when there is no content (204, or an empty JSON response). Throws an ApiError
 * (carrying the HTTP status) for non-2xx responses. A network failure rejects with the original
 * fetch error.
 */
export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, headers, ...init } = options
  const hasJsonBody = body !== undefined && !isRawBody(body)

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      ...(hasJsonBody && { 'Content-Type': 'application/json' }),
      ...headers,
    },
    body: hasJsonBody ? JSON.stringify(body) : (body as BodyInit | undefined),
  })

  if (!response.ok) {
    throw new ApiError(response.status, `Request to ${path} failed with status ${response.status}.`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  const isJson = (response.headers.get('content-type') ?? '').includes('application/json')
  if (isJson) {
    return (text === '' ? undefined : JSON.parse(text)) as T
  }
  return text as T
}
