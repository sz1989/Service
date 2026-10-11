import { useCallback, useState } from 'react'
import { API_BASE_URL } from '../api/config'
import { ApiError } from './useFetch'

interface UseDeleteResult {
  error: string | null
  isLoading: boolean
  execute: (path: string, options?: RequestInit) => Promise<void>
}

export function useDelete(): UseDeleteResult {
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const execute = useCallback(async (path: string, options?: RequestInit) => {
    setIsLoading(true)
    setError(null)

    try {
      const response = await fetch(`${API_BASE_URL}${path}`, { ...options, method: 'DELETE' })

      if (!response.ok) {
        throw new ApiError(response.status, `Request to ${path} failed with status ${response.status}.`)
      }
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Network error.'
      setError(message)
      throw err
    } finally {
      setIsLoading(false)
    }
  }, [])

  return { error, isLoading, execute }
}
