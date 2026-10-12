import { useCallback, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../api/ApiError'
import { useAuth } from '../auth/AuthContext'

// What every call to the protected API needs: the bearer token header, and what to do when the
// server answers 401 because the session is gone.
export function useAuthedRequest() {
  const { token, logout } = useAuth()
  const navigate = useNavigate()

  // Changes only when the token does, so it is safe to use as an effect dependency.
  const authHeaders = useMemo(() => ({ Authorization: `Bearer ${token}` }), [token])

  // On a 401: log out and go to the login page. Returns true when it handled the error, so the
  // caller knows not to report it again.
  const handleUnauthorized = useCallback(
    (err: unknown): boolean => {
      if (err instanceof ApiError && err.status === 401) {
        logout()
        navigate('/login', { replace: true })
        return true
      }
      return false
    },
    [logout, navigate],
  )

  return { authHeaders, handleUnauthorized }
}
