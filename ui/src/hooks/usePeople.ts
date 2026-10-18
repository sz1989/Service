import { useQuery } from '@tanstack/react-query'
import { ApiError } from '../api/ApiError'
import { PERSON_URL } from '../api/config'
import { request } from '../api/request'
import type { Person } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { useAuthedRequest } from './useAuthedRequest'

// Loads the people list. The token is part of the key so a new login never sees the previous
// user's cached list.
export function usePeople() {
  const { token } = useAuth()
  const { authHeaders, handleUnauthorized } = useAuthedRequest()

  return useQuery({
    queryKey: ['people', token],
    queryFn: async () => {
      try {
        return await request<Person[]>(`${PERSON_URL}/All`, { headers: authHeaders })
      } catch (err) {
        handleUnauthorized(err)
        throw err // rethrown so the query still ends in its error state
      }
    },
    // A 4xx will not fix itself, so only retry network and server errors.
    retry: (failureCount, err) =>
      !(err instanceof ApiError && err.status < 500) && failureCount < 2,
  })
}
