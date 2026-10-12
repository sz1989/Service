import { useCallback, useState } from 'react'
import { ApiError } from '../api/ApiError'
import { request, type RequestOptions } from '../api/request'

interface UseRequestResult<T> {
  data: T | null
  error: string | null
  isLoading: boolean
  execute: (path: string, options?: RequestOptions) => Promise<T>
}

// Wraps request() with loading, error and data state. Each call of the hook has its own state,
// and `execute` keeps the same identity between renders.
export function useRequest<T>(): UseRequestResult<T> {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const execute = useCallback(async (path: string, options?: RequestOptions) => {
    setIsLoading(true)
    setError(null)

    try {
      const result = await request<T>(path, options)
      setData(result)
      return result
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Network error.')
      throw err
    } finally {
      setIsLoading(false)
    }
  }, [])

  return { data, error, isLoading, execute }
}
