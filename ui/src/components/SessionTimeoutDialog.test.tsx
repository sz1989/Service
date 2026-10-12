import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { SessionTimeoutDialog } from './SessionTimeoutDialog'

function setup(secondsLeft = 60) {
  const onStay = vi.fn()
  const onSignOut = vi.fn()
  render(<SessionTimeoutDialog secondsLeft={secondsLeft} onStay={onStay} onSignOut={onSignOut} />)
  return { onStay, onSignOut }
}

describe('SessionTimeoutDialog', () => {
  it('asks if the user is still there and shows the seconds left', () => {
    setup(42)

    expect(screen.getByRole('dialog', { name: 'Are you still there?' })).toBeInTheDocument()
    expect(screen.getByRole('timer')).toHaveTextContent('You will be signed out in 42 seconds because of inactivity.')
  })

  it('says "1 second" in the singular', () => {
    setup(1)

    expect(screen.getByRole('timer')).toHaveTextContent('in 1 second because')
  })

  it('puts the focus on the Stay signed in button', () => {
    setup()

    expect(screen.getByRole('button', { name: 'Stay signed in' })).toHaveFocus()
  })

  it('calls onStay for Stay signed in and for the × button', async () => {
    const user = userEvent.setup()
    const { onStay, onSignOut } = setup()

    await user.click(screen.getByRole('button', { name: 'Stay signed in' }))
    await user.click(screen.getByRole('button', { name: 'Close' }))

    expect(onStay).toHaveBeenCalledTimes(2)
    expect(onSignOut).not.toHaveBeenCalled()
  })

  it('treats Esc as Stay signed in, and keeps the native close from happening', () => {
    const { onStay, onSignOut } = setup()

    // A browser fires "cancel" on the dialog for Esc; jsdom does not turn the key into that event.
    const notPrevented = fireEvent(screen.getByRole('dialog'), new Event('cancel', { cancelable: true }))

    expect(notPrevented).toBe(false) // preventDefault was called, so the parent decides whether it closes
    expect(onStay).toHaveBeenCalledTimes(1)
    expect(onSignOut).not.toHaveBeenCalled()
  })

  it('calls onSignOut for Sign out now', async () => {
    const user = userEvent.setup()
    const { onStay, onSignOut } = setup()

    await user.click(screen.getByRole('button', { name: 'Sign out now' }))

    expect(onSignOut).toHaveBeenCalledTimes(1)
    expect(onStay).not.toHaveBeenCalled()
  })
})
