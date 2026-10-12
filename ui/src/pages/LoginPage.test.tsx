import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { LoginPage } from './LoginPage'

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({ isAuthenticated: false, login: vi.fn() }),
}))

function renderLogin(state?: unknown) {
  render(
    <MemoryRouter initialEntries={[{ pathname: '/login', state }]}>
      <LoginPage />
    </MemoryRouter>,
  )
}

describe('LoginPage', () => {
  it('explains an inactivity logout', () => {
    renderLogin({ reason: 'idle' })

    expect(screen.getByRole('status')).toHaveTextContent(
      'You were signed out because of inactivity. Please sign in again.',
    )
  })

  it('shows no message on a normal visit', () => {
    renderLogin()

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
  })

  it('shows no message for an unrelated reason', () => {
    renderLogin({ reason: 'something-else' })

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })
})
