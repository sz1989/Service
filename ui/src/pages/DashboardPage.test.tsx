import { act, fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { IDLE_LOGOUT_MS, IDLE_WARNING_MS } from '../auth/idleConfig'
import { DashboardPage } from './DashboardPage'

const logout = vi.fn()

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({ token: 't', logout }),
}))

// The panels do their own requests; they are not what is tested here.
vi.mock('../components/ChatPanel', () => ({ ChatPanel: () => <div>chat panel</div> }))
vi.mock('../components/PersonList', () => ({ PersonList: () => <div>person list</div> }))
vi.mock('../components/RagSearchPanel', () => ({ RagSearchPanel: () => <div>rag panel</div> }))

function LoginProbe() {
  const reason = (useLocation().state as { reason?: string } | null)?.reason
  return <div>login page, reason: {reason ?? 'none'}</div>
}

function renderDashboard() {
  render(
    <MemoryRouter initialEntries={['/dashboard']}>
      <Routes>
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/login" element={<LoginProbe />} />
      </Routes>
    </MemoryRouter>,
  )
}

function advance(ms: number) {
  act(() => {
    vi.advanceTimersByTime(ms)
  })
}

beforeEach(() => {
  vi.useFakeTimers()
  logout.mockClear()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('DashboardPage: inactivity logout', () => {
  it('shows no warning while the user is within the idle period', () => {
    renderDashboard()

    advance(IDLE_LOGOUT_MS - IDLE_WARNING_MS - 1)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(logout).not.toHaveBeenCalled()
  })

  it('warns, with the seconds left, shortly before the logout', () => {
    renderDashboard()

    advance(IDLE_LOGOUT_MS - IDLE_WARNING_MS)

    expect(screen.getByRole('dialog', { name: 'Are you still there?' })).toBeInTheDocument()
    expect(screen.getByRole('timer')).toHaveTextContent(`${IDLE_WARNING_MS / 1000} seconds`)
    expect(logout).not.toHaveBeenCalled()

    advance(1000)
    expect(screen.getByRole('timer')).toHaveTextContent(`${IDLE_WARNING_MS / 1000 - 1} seconds`)
  })

  it('logs out and goes to the login page with the inactivity reason when nobody answers', () => {
    renderDashboard()

    advance(IDLE_LOGOUT_MS)

    expect(logout).toHaveBeenCalledTimes(1)
    expect(screen.getByText('login page, reason: idle')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('Stay signed in closes the warning and starts the whole idle period again', () => {
    renderDashboard()
    advance(IDLE_LOGOUT_MS - IDLE_WARNING_MS)

    fireEvent.click(screen.getByRole('button', { name: 'Stay signed in' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

    advance(IDLE_LOGOUT_MS - 1) // a full period later it would have logged out without the answer
    expect(logout).not.toHaveBeenCalled()
    expect(screen.getByText('chat panel')).toBeInTheDocument()
  })

  it('Sign out now logs out at once, without the inactivity reason', () => {
    renderDashboard()
    advance(IDLE_LOGOUT_MS - IDLE_WARNING_MS)

    fireEvent.click(screen.getByRole('button', { name: 'Sign out now' }))

    expect(logout).toHaveBeenCalledTimes(1)
    expect(screen.getByText('login page, reason: none')).toBeInTheDocument()
  })

  it('moving the mouse before the warning postpones it', () => {
    renderDashboard()
    advance(IDLE_LOGOUT_MS - IDLE_WARNING_MS - 1000)

    fireEvent.mouseMove(window)
    advance(IDLE_WARNING_MS) // the warning would be showing by now without the activity

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
