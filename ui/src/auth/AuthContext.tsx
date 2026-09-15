import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import type { LoginResponse } from '../api/types'
import { useFetch } from '../hooks/useFetch'

const TOKEN_STORAGE_KEY = 'authToken'

interface AuthContextValue {
  token: string | null
  isAuthenticated: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() =>
    localStorage.getItem(TOKEN_STORAGE_KEY),
  )
  const { execute } = useFetch<LoginResponse>()

  const login = useCallback(
    async (username: string, password: string) => {
      const { token } = await execute('/Auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      })
      localStorage.setItem(TOKEN_STORAGE_KEY, token)
      setToken(token)
    },
    [execute],
  )

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_STORAGE_KEY)
    setToken(null)
  }, [])

  const value = useMemo(
    () => ({ token, isAuthenticated: token !== null, login, logout }),
    [token, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
