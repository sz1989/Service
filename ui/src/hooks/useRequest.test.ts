import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/ApiError'
import { useRequest } from './useRequest'

function stubFetch(response: Response) {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response))
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('useRequest', () => {
  it('starts idle', () => {
    const { result } = renderHook(() => useRequest<string[]>())

    expect(result.current.data).toBeNull()
    expect(result.current.error).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('stores and returns the result of a successful request', async () => {
    stubFetch(new Response(JSON.stringify(['a', 'b']), { status: 200, headers: { 'content-type': 'application/json' } }))
    const { result } = renderHook(() => useRequest<string[]>())

    let returned: string[] | undefined
    await act(async () => {
      returned = await result.current.execute('/items')
    })

    expect(returned).toEqual(['a', 'b'])
    expect(result.current.data).toEqual(['a', 'b'])
    expect(result.current.error).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('is loading while the request is in flight', async () => {
    let finish: (response: Response) => void = () => {}
    vi.stubGlobal('fetch', vi.fn().mockReturnValue(new Promise<Response>((resolve) => { finish = resolve })))
    const { result } = renderHook(() => useRequest<string>())

    let pending: Promise<string> = Promise.resolve('')
    act(() => {
      pending = result.current.execute('/slow')
    })
    expect(result.current.isLoading).toBe(true)

    await act(async () => {
      finish(new Response('done', { status: 200, headers: { 'content-type': 'text/plain' } }))
      await pending
    })
    expect(result.current.isLoading).toBe(false)
  })

  it('sets the ApiError message and rethrows for a bad status', async () => {
    stubFetch(new Response('forbidden', { status: 403 }))
    const { result } = renderHook(() => useRequest<string>())

    let thrown: unknown
    await act(async () => {
      thrown = await result.current.execute('/V2/Person').catch((err: unknown) => err)
    })

    expect(thrown).toBeInstanceOf(ApiError)
    expect(result.current.error).toBe('Request to /V2/Person failed with status 403.')
    expect(result.current.isLoading).toBe(false)
  })

  it('reports "Network error." when fetch itself fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
    const { result } = renderHook(() => useRequest<string>())

    await act(async () => {
      await result.current.execute('/x').catch(() => undefined)
    })

    expect(result.current.error).toBe('Network error.')
  })

  it('clears a previous error when the next request starts', async () => {
    const { result } = renderHook(() => useRequest<string>())

    stubFetch(new Response('boom', { status: 500 }))
    await act(async () => {
      await result.current.execute('/x').catch(() => undefined)
    })
    expect(result.current.error).not.toBeNull()

    stubFetch(new Response('ok', { status: 200, headers: { 'content-type': 'text/plain' } }))
    await act(async () => {
      await result.current.execute('/x')
    })
    expect(result.current.error).toBeNull()
  })

  it('keeps the same execute function between renders', () => {
    const { result, rerender } = renderHook(() => useRequest<string>())
    const first = result.current.execute

    rerender()

    expect(result.current.execute).toBe(first)
  })
})
