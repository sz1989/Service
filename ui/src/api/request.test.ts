import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from './ApiError'
import { API_BASE_URL } from './config'
import { request } from './request'

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

describe('request', () => {
  it('calls the API base url plus the path and returns parsed JSON', async () => {
    const fetchMock = stubFetch(jsonResponse([{ id: 1 }]))

    const result = await request<{ id: number }[]>('/V2/Person/All')

    expect(result).toEqual([{ id: 1 }])
    expect(fetchMock).toHaveBeenCalledWith(`${API_BASE_URL}/V2/Person/All`, expect.any(Object))
  })

  it('returns the body as text when the response is not JSON', async () => {
    stubFetch(new Response('hello', { status: 200, headers: { 'content-type': 'text/plain' } }))

    expect(await request<string>('/hello')).toBe('hello')
  })

  it('returns undefined for 204 No Content', async () => {
    stubFetch(new Response(null, { status: 204 }))

    expect(await request('/V2/Person/1', { method: 'DELETE' })).toBeUndefined()
  })

  it('sends a body as JSON with a Content-Type header and keeps the caller headers', async () => {
    const fetchMock = stubFetch(jsonResponse({ id: 5 }, 201))

    await request('/V2/Person', {
      method: 'POST',
      body: { name: 'Alex' },
      headers: { Authorization: 'Bearer abc' },
    })

    const [, init] = fetchMock.mock.calls[0]
    expect(init.method).toBe('POST')
    expect(init.body).toBe(JSON.stringify({ name: 'Alex' }))
    expect(init.headers).toEqual({ 'Content-Type': 'application/json', Authorization: 'Bearer abc' })
  })

  it('sends a string body as it is, without a second JSON encoding or a Content-Type of its own', async () => {
    const fetchMock = stubFetch(jsonResponse({ token: 't' }))
    const body = JSON.stringify({ username: 'a', password: 'b' })

    await request('/Auth/login', { method: 'POST', body, headers: { 'Content-Type': 'application/json' } })

    const [, init] = fetchMock.mock.calls[0]
    expect(init.body).toBe(body)
    expect(init.headers).toEqual({ 'Content-Type': 'application/json' })
  })

  it('does not add a Content-Type for a string body', async () => {
    const fetchMock = stubFetch(new Response('ok', { status: 200 }))

    await request('/raw', { method: 'POST', body: 'plain text' })

    expect(fetchMock.mock.calls[0][1].headers).toEqual({})
  })

  it('returns undefined for an empty JSON response', async () => {
    stubFetch(new Response('', { status: 200, headers: { 'content-type': 'application/json' } }))

    expect(await request('/empty')).toBeUndefined()
  })

  it('does not set Content-Type or a body when there is no body', async () => {
    const fetchMock = stubFetch(jsonResponse([]))

    await request('/V2/Person/All', { headers: { Authorization: 'Bearer abc' } })

    const [, init] = fetchMock.mock.calls[0]
    expect(init.body).toBeUndefined()
    expect(init.headers).toEqual({ Authorization: 'Bearer abc' })
  })

  it('throws an ApiError carrying the status for a non-2xx response', async () => {
    stubFetch(new Response('nope', { status: 403 }))

    const error = await request('/V2/Person', { method: 'POST', body: {} }).catch((err: unknown) => err)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 403, message: 'Request to /V2/Person failed with status 403.' })
  })

  it('rejects with the original error when the network fails', async () => {
    const networkError = new TypeError('Failed to fetch')
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(networkError))

    await expect(request('/anything')).rejects.toBe(networkError)
  })
})
