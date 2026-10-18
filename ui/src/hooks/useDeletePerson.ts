import { useMutation, useQueryClient } from '@tanstack/react-query'
import { PERSON_URL } from '../api/config'
import { request } from '../api/request'
import { useAuthedRequest } from './useAuthedRequest'

// Deletes one person by id, then reloads the people list.
export function useDeletePerson() {
  const queryClient = useQueryClient()
  const { authHeaders, handleUnauthorized } = useAuthedRequest()

  return useMutation({
    mutationFn: (id: number) =>
      request<void>(`${PERSON_URL}/${id}`, { method: 'DELETE', headers: authHeaders }),
    // Returned, so the mutation stays pending until the list has been reloaded.
    // The key is a prefix of ['people', token], so this reloads the list for the current login.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['people'] }),
    onError: (err) => {
      handleUnauthorized(err)
    },
  })
}
