import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/ApiError'
import { API_BASE_URL } from '../api/config'
import { useUpdate } from './useUpdate'

interface Item {
  id: number
  name: string
}

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

describe('useUpdate', () => {
  it('starts idle', () => {
    const { result } = renderHook(() => useUpdate<Item>())

    expect(result.current.data).toBeNull()
    expect(result.current.error).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('PUTs the body as JSON with the caller headers', async () => {
    const fetchMock = stubFetch(jsonResponse({ id: 3, name: 'Alex' }))
    const { result } = renderHook(() => useUpdate<Item>())

    await act(async () => {
      await result.current.execute('/V2/Person', { id: 3, name: 'Alex' }, { headers: { Authorization: 'Bearer abc' } })
    })

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/V2/Person`)
    expect(init.method).toBe('PUT')
    expect(init.body).toBe(JSON.stringify({ id: 3, name: 'Alex' }))
    expect(init.headers).toEqual({ 'Content-Type': 'application/json', Authorization: 'Bearer abc' })
  })

  it('resolves with the updated item the server returns and stores it in data', async () => {
    stubFetch(jsonResponse({ id: 3, name: 'Alex (saved)' }))
    const { result } = renderHook(() => useUpdate<Item>())

    let returned: Item | undefined
    await act(async () => {
      returned = await result.current.execute('/V2/Person', { id: 3, name: 'Alex' })
    })

    expect(returned).toEqual({ id: 3, name: 'Alex (saved)' })
    expect(result.current.data).toEqual({ id: 3, name: 'Alex (saved)' })
    expect(result.current.error).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('always uses PUT, even if the caller passes another method', async () => {
    const fetchMock = stubFetch(jsonResponse({ id: 3, name: 'Alex' }))
    const { result } = renderHook(() => useUpdate<Item>())

    await act(async () => {
      await result.current.execute('/V2/Person', { id: 3, name: 'Alex' }, { method: 'POST' })
    })

    expect(fetchMock.mock.calls[0][1].method).toBe('PUT')
  })

  it('sets the error and rethrows an ApiError carrying the status when the item is not found (404)', async () => {
    stubFetch(new Response('', { status: 404 }))
    const { result } = renderHook(() => useUpdate<Item>())

    let thrown: unknown
    await act(async () => {
      thrown = await result.current.execute('/V2/Person', { id: 99, name: 'Ghost' }).catch((err: unknown) => err)
    })

    expect(thrown).toBeInstanceOf(ApiError)
    expect(thrown).toMatchObject({ status: 404 })
    expect(result.current.error).toBe('Request to /V2/Person failed with status 404.')
    expect(result.current.data).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('reports "Network error." when fetch itself fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
    const { result } = renderHook(() => useUpdate<Item>())

    await act(async () => {
      await result.current.execute('/V2/Person', { id: 3, name: 'Alex' }).catch(() => undefined)
    })

    expect(result.current.error).toBe('Network error.')
  })

  it('keeps the same execute function between renders', () => {
    const { result, rerender } = renderHook(() => useUpdate<Item>())
    const first = result.current.execute

    rerender()

    expect(result.current.execute).toBe(first)
  })
})
