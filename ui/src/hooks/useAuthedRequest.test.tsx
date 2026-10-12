import { renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/ApiError'
import { useAuthedRequest } from './useAuthedRequest'

const logout = vi.fn()
const navigate = vi.fn()
let token: string | null = 'abc'

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({ token, logout }),
}))

vi.mock('react-router-dom', () => ({
  useNavigate: () => navigate,
}))

beforeEach(() => {
  logout.mockClear()
  navigate.mockClear()
  token = 'abc'
})

describe('useAuthedRequest', () => {
  it('builds the bearer header from the token', () => {
    const { result } = renderHook(() => useAuthedRequest())

    expect(result.current.authHeaders).toEqual({ Authorization: 'Bearer abc' })
  })

  it('keeps the same headers and handler between renders while the token is unchanged', () => {
    const { result, rerender } = renderHook(() => useAuthedRequest())
    const { authHeaders, handleUnauthorized } = result.current

    rerender()

    expect(result.current.authHeaders).toBe(authHeaders)
    expect(result.current.handleUnauthorized).toBe(handleUnauthorized)
  })

  it('builds new headers when the token changes', () => {
    const { result, rerender } = renderHook(() => useAuthedRequest())
    const before = result.current.authHeaders

    token = 'xyz'
    rerender()

    expect(result.current.authHeaders).not.toBe(before)
    expect(result.current.authHeaders).toEqual({ Authorization: 'Bearer xyz' })
  })

  it('logs out, goes to the login page and returns true for a 401', () => {
    const { result } = renderHook(() => useAuthedRequest())

    const handled = result.current.handleUnauthorized(new ApiError(401, 'Unauthorized'))

    expect(handled).toBe(true)
    expect(logout).toHaveBeenCalledTimes(1)
    expect(navigate).toHaveBeenCalledWith('/login', { replace: true })
  })

  it.each([
    ['another status', new ApiError(403, 'Forbidden')],
    ['a network error', new TypeError('Failed to fetch')],
    ['something that is not an error', 'oops'],
  ])('does nothing and returns false for %s', (_name, err) => {
    const { result } = renderHook(() => useAuthedRequest())

    const handled = result.current.handleUnauthorized(err)

    expect(handled).toBe(false)
    expect(logout).not.toHaveBeenCalled()
    expect(navigate).not.toHaveBeenCalled()
  })
})
