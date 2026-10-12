import { useCallback } from 'react'
import type { RequestOptions } from '../api/request'
import { useRequest } from './useRequest'

interface UseCreateResult<T> {
  data: boolean
  error: string | null
  isLoading: boolean
  execute: (path: string, body: T, options?: RequestOptions) => Promise<boolean>
}

// POSTs `body` as JSON. `data` turns true after the first successful create.
export function useCreate<T>(): UseCreateResult<T> {
  const { data, error, isLoading, execute } = useRequest<unknown>()

  const create = useCallback(
    async (path: string, body: T, options?: RequestOptions) => {
      await execute(path, { ...options, method: 'POST', body })
      return true
    },
    [execute],
  )

  return { data: data !== null, error, isLoading, execute: create }
}
