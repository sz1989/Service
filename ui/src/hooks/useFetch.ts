import { useCallback, useState } from 'react'

const API_BASE_URL = 'https://localhost:7071'

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

interface UseFetchResult<T> {
  data: T | null
  error: string | null
  isLoading: boolean
  execute: (path: string, options?: RequestInit) => Promise<T>
}

export function useFetch<T>(): UseFetchResult<T> {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const execute = useCallback(async (path: string, options?: RequestInit) => {
    setIsLoading(true)
    setError(null)

    try {
      const response = await fetch(`${API_BASE_URL}${path}`, options)

      if (!response.ok) {
        throw new ApiError(response.status, `Request to ${path} failed with status ${response.status}.`)
      }

      const result = (await response.json()) as T
      setData(result)
      return result
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Network error.'
      setError(message)
      throw err
    } finally {
      setIsLoading(false)
    }
  }, [])

  return { data, error, isLoading, execute }
}
