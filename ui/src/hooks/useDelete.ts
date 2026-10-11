import { useCallback } from 'react'
import type { RequestOptions } from '../api/request'
import { useRequest } from './useRequest'

interface UseDeleteResult {
  error: string | null
  isLoading: boolean
  execute: (path: string, options?: RequestOptions) => Promise<void>
}

export function useDelete(): UseDeleteResult {
  const { error, isLoading, execute } = useRequest<void>()

  const remove = useCallback(
    async (path: string, options?: RequestOptions) => {
      // method comes last so a caller cannot turn the delete into another verb
      await execute(path, { ...options, method: 'DELETE' })
    },
    [execute],
  )

  return { error, isLoading, execute: remove }
}
