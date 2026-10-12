import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { API_BASE_URL } from '../api/config'
import { IngestPanel } from './IngestPanel'

const logout = vi.fn()
const navigate = vi.fn()

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({ token: 'test-token', logout }),
}))

vi.mock('react-router-dom', () => ({
  useNavigate: () => navigate,
}))

function stubFetch(response: Response) {
  const fetchMock = vi.fn().mockResolvedValue(response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

beforeEach(() => {
  logout.mockClear()
  navigate.mockClear()
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('IngestPanel', () => {
  it('POSTs the lines as a JSON array with the bearer token', async () => {
    const user = userEvent.setup()
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    render(<IngestPanel />)

    const textarea = screen.getByLabelText('Documents to ingest, one per line')
    await user.clear(textarea)
    await user.type(textarea, ' first {Enter}second')
    await user.click(screen.getByRole('button', { name: 'Ingest' }))

    expect(await screen.findByText('Ingested 2 document(s).')).toBeInTheDocument()
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/Chat/ingest`)
    expect(init.method).toBe('POST')
    expect(init.body).toBe(JSON.stringify(['first', 'second']))
    expect(init.headers).toEqual({ Authorization: 'Bearer test-token', 'Content-Type': 'application/json' })
  })

  it('logs out and goes to the login page on a 401', async () => {
    const user = userEvent.setup()
    stubFetch(new Response('', { status: 401 }))
    render(<IngestPanel />)

    await user.click(screen.getByRole('button', { name: 'Ingest' }))

    await waitFor(() => expect(logout).toHaveBeenCalledTimes(1))
    expect(navigate).toHaveBeenCalledWith('/login', { replace: true })
  })

  it('shows the error and does not log out on another failure', async () => {
    const user = userEvent.setup()
    stubFetch(new Response('', { status: 500 }))
    render(<IngestPanel />)

    await user.click(screen.getByRole('button', { name: 'Ingest' }))

    expect(await screen.findByText('Request to /Chat/ingest failed with status 500.')).toBeInTheDocument()
    expect(logout).not.toHaveBeenCalled()
  })
})
