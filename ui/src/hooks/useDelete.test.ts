import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/ApiError'
import { API_BASE_URL } from '../api/config'
import { useDelete } from './useDelete'

function stubFetch(response: Response) {
  const fetchMock = vi.fn().mockResolvedValue(response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('useDelete', () => {
  it('sends a DELETE with the caller headers and resolves with nothing', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = renderHook(() => useDelete())

    let returned: unknown = 'not set'
    await act(async () => {
      returned = await result.current.execute('/V2/Person/5', { headers: { Authorization: 'Bearer abc' } })
    })

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/V2/Person/5`)
    expect(init.method).toBe('DELETE')
    expect(init.headers).toEqual({ Authorization: 'Bearer abc' })
    expect(returned).toBeUndefined()
    expect(result.current.error).toBeNull()
  })

  it('always uses DELETE, even if the caller passes another method', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = renderHook(() => useDelete())

    await act(async () => {
      await result.current.execute('/V2/Person/5', { method: 'POST' })
    })

    expect(fetchMock.mock.calls[0][1].method).toBe('DELETE')
  })

  it('sets the error and rethrows an ApiError carrying the status (404)', async () => {
    stubFetch(new Response('', { status: 404 }))
    const { result } = renderHook(() => useDelete())

    let thrown: unknown
    await act(async () => {
      thrown = await result.current.execute('/V2/Person/99').catch((err: unknown) => err)
    })

    expect(thrown).toBeInstanceOf(ApiError)
    expect(thrown).toMatchObject({ status: 404 })
    expect(result.current.error).toBe('Request to /V2/Person/99 failed with status 404.')
    expect(result.current.isLoading).toBe(false)
  })

  it('keeps the same execute function between renders', () => {
    const { result, rerender } = renderHook(() => useDelete())
    const first = result.current.execute

    rerender()

    expect(result.current.execute).toBe(first)
  })
})
