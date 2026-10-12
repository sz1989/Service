import type { RequestOptions } from '../api/request'
import { useRequest } from './useRequest'

interface UseFetchResult<T> {
  data: T | null
  error: string | null
  isLoading: boolean
  execute: (path: string, options?: RequestOptions) => Promise<T>
}

// Any request whose response is read: GET lists, login, search, ingest...
export function useFetch<T>(): UseFetchResult<T> {
  return useRequest<T>()
}
