import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { API_BASE_URL } from '../api/config'
import { RagSearchPanel } from './RagSearchPanel'

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

async function search(user: ReturnType<typeof userEvent.setup>, query: string) {
  await user.type(screen.getByLabelText('Document search query'), query)
  await user.click(screen.getByRole('button', { name: 'Search' }))
}

beforeEach(() => {
  logout.mockClear()
  navigate.mockClear()
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('RagSearchPanel', () => {
  it('searches with the encoded query and the bearer token, and lists the matches', async () => {
    const user = userEvent.setup()
    const fetchMock = stubFetch(
      new Response(JSON.stringify([{ id: 'a', text: 'Minimal APIs', distance: 0.12345 }]), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    )
    render(<RagSearchPanel />)

    await search(user, 'minimal apis')

    expect(await screen.findByText('Minimal APIs')).toBeInTheDocument()
    expect(screen.getByText('0.1235')).toBeInTheDocument()
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/Chat/search?query=minimal%20apis&topK=5`)
    expect(init.headers).toEqual({ Authorization: 'Bearer test-token' })
  })

  it('logs out and goes to the login page on a 401', async () => {
    const user = userEvent.setup()
    stubFetch(new Response('', { status: 401 }))
    render(<RagSearchPanel />)

    await search(user, 'anything')

    await waitFor(() => expect(logout).toHaveBeenCalledTimes(1))
    expect(navigate).toHaveBeenCalledWith('/login', { replace: true })
  })

  it('shows the error and does not log out on another failure', async () => {
    const user = userEvent.setup()
    stubFetch(new Response('', { status: 500 }))
    render(<RagSearchPanel />)

    await search(user, 'anything')

    expect(await screen.findByText('Request to /Chat/search?query=anything&topK=5 failed with status 500.')).toBeInTheDocument()
    expect(logout).not.toHaveBeenCalled()
  })
})
