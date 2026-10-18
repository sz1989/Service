import { useMutation, useQueryClient, type QueryKey } from '@tanstack/react-query'
import { request } from '../api/request'
import { useAuthedRequest } from './useAuthedRequest'

interface UseApiMutationOptions<TVariables> {
  method: 'POST' | 'PUT' | 'DELETE'
  /** Builds the URL from what the caller passes to mutate. */
  path: (variables: TVariables) => string
  /** Queries to reload after a success. A prefix matches every key that starts with it. */
  invalidateKey?: QueryKey
}

// A write to the protected API: sends the bearer token, logs out on a 401, and reloads the
// affected queries after a success. For POST and PUT the variables are the JSON body; a DELETE
// sends no body, so its variables are only used to build the path (e.g. the id).
export function useApiMutation<TVariables, TData = void>({
  method,
  path,
  invalidateKey = ['people'],
}: UseApiMutationOptions<TVariables>) {
  const queryClient = useQueryClient()
  const { authHeaders, handleUnauthorized } = useAuthedRequest()

  return useMutation({
    mutationFn: (variables: TVariables) =>
      request<TData>(path(variables), {
        method,
        headers: authHeaders,
        body: method === 'DELETE' ? undefined : variables,
      }),
    // Returned, so the mutation stays pending until the reloaded data has arrived.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: invalidateKey }),
    onError: (err) => {
      handleUnauthorized(err)
    },
  })
}
