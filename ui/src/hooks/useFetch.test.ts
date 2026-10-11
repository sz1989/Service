import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { API_BASE_URL } from '../api/config'
import { useFetch } from './useFetch'

function stubFetch(response: Response) {
  const fetchMock = vi.fn().mockResolvedValue(response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('useFetch', () => {
  it('returns and stores the parsed response', async () => {
    const fetchMock = stubFetch(jsonResponse([{ id: 1 }]))
    const { result } = renderHook(() => useFetch<{ id: number }[]>())

    await act(async () => {
      await result.current.execute('/V2/Person/All', { headers: { Authorization: 'Bearer abc' } })
    })

    expect(result.current.data).toEqual([{ id: 1 }])
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/V2/Person/All`)
    expect(fetchMock.mock.calls[0][1].headers).toEqual({ Authorization: 'Bearer abc' })
  })

  it('still accepts a body the caller already serialized (login, ingest)', async () => {
    const fetchMock = stubFetch(jsonResponse({ token: 't' }))
    const { result } = renderHook(() => useFetch<{ token: string }>())
    const body = JSON.stringify({ username: 'a', password: 'b' })

    await act(async () => {
      await result.current.execute('/Auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body,
      })
    })

    expect(fetchMock.mock.calls[0][1].body).toBe(body)
  })
})
