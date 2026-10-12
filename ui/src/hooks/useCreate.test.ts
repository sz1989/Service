import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/ApiError'
import { API_BASE_URL } from '../api/config'
import { useCreate } from './useCreate'

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

describe('useCreate', () => {
  it('POSTs the body as JSON with the caller headers and resolves true', async () => {
    const fetchMock = stubFetch(jsonResponse({ id: 7 }, 201))
    const { result } = renderHook(() => useCreate<{ name: string }>())

    let returned: boolean | undefined
    await act(async () => {
      returned = await result.current.execute('/V2/Person', { name: 'Alex' }, { headers: { Authorization: 'Bearer abc' } })
    })

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/V2/Person`)
    expect(init.method).toBe('POST')
    expect(init.body).toBe(JSON.stringify({ name: 'Alex' }))
    expect(init.headers).toEqual({ 'Content-Type': 'application/json', Authorization: 'Bearer abc' })
    expect(returned).toBe(true)
    expect(result.current.data).toBe(true)
    expect(result.current.error).toBeNull()
  })

  it('starts with data false', () => {
    const { result } = renderHook(() => useCreate<{ name: string }>())

    expect(result.current.data).toBe(false)
  })

  it('sets the error and rethrows when the server rejects the create', async () => {
    stubFetch(new Response('', { status: 403 }))
    const { result } = renderHook(() => useCreate<{ name: string }>())

    let thrown: unknown
    await act(async () => {
      thrown = await result.current.execute('/V2/Person', { name: 'Alex' }).catch((err: unknown) => err)
    })

    expect(thrown).toBeInstanceOf(ApiError)
    expect(thrown).toMatchObject({ status: 403 })
    expect(result.current.error).toBe('Request to /V2/Person failed with status 403.')
    expect(result.current.data).toBe(false)
  })

  it('keeps the same execute function between renders', () => {
    const { result, rerender } = renderHook(() => useCreate<{ name: string }>())
    const first = result.current.execute

    rerender()

    expect(result.current.execute).toBe(first)
  })
})
