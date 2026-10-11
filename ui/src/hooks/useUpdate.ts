import { useCallback } from 'react'
import type { RequestOptions } from '../api/request'
import { useRequest } from './useRequest'

interface UseUpdateResult<T>{
    data: T | null
    error: string | null
    isLoading: boolean
    execute: (path: string, body: T, options?: RequestOptions) => Promise<T>
}

export function useUpdate<T>(): UseUpdateResult<T> {
    const { data, error, isLoading, execute } = useRequest<T>()

    const update = useCallback(
        async (path: string, body:T, options?: RequestOptions) => {
            return await execute(path, { ...options, method: 'PUT', body })
        }, 
        [execute]
    )

    return {data, error, isLoading, execute: update }
}