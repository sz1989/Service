import { render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ErrorBoundary } from './ErrorBoundary'

function Bomb(): never {
  throw new Error('boom')
}

function MaybeBomb({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) {
    throw new Error('boom')
  }

  return <p>Recovered</p>
}

describe('ErrorBoundary', () => {
  beforeEach(() => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('does not recover after the failing child stops throwing', () => {
    const { rerender } = render(
      <ErrorBoundary>
        <MaybeBomb shouldThrow={true} />
      </ErrorBoundary>,
    )

    expect(screen.getByText('Something went wrong. Please refresh the page.')).toBeInTheDocument()

    rerender(
      <ErrorBoundary>
        <MaybeBomb shouldThrow={false} />
      </ErrorBoundary>,
    )

    expect(screen.getByText('Something went wrong. Please refresh the page.')).toBeInTheDocument()
    expect(screen.queryByText('Recovered')).not.toBeInTheDocument()
  })

  it('recovers when remounted via a changed key', () => {
    const { rerender } = render(
      <ErrorBoundary key="attempt-1">
        <MaybeBomb shouldThrow={true} />
      </ErrorBoundary>,
    )

    expect(screen.getByText('Something went wrong. Please refresh the page.')).toBeInTheDocument()

    rerender(
      <ErrorBoundary key="attempt-2">
        <MaybeBomb shouldThrow={false} />
      </ErrorBoundary>,
    )

    expect(screen.getByText('Recovered')).toBeInTheDocument()
    expect(screen.queryByText('Something went wrong. Please refresh the page.')).not.toBeInTheDocument()
  })

  it('renders children when no error is thrown', () => {
    render(
      <ErrorBoundary>
        <p>All good</p>
      </ErrorBoundary>,
    )

    expect(screen.getByText('All good')).toBeInTheDocument()
  })

  it('renders the default message when a child throws and no fallback is given', () => {
    render(
      <ErrorBoundary>
        <Bomb />
      </ErrorBoundary>,
    )

    expect(screen.getByText('Something went wrong. Please refresh the page.')).toBeInTheDocument()
  })

  it('renders the fallback prop when a child throws', () => {
    render(
      <ErrorBoundary fallback={<p>Custom fallback</p>}>
        <Bomb />
      </ErrorBoundary>,
    )

    expect(screen.getByText('Custom fallback')).toBeInTheDocument()
    expect(screen.queryByText('Something went wrong. Please refresh the page.')).not.toBeInTheDocument()
  })

  it('logs the caught error via console.error', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    render(
      <ErrorBoundary>
        <Bomb />
      </ErrorBoundary>,
    )

    expect(consoleError).toHaveBeenCalledWith(
      'ErrorBoundary caught an error:',
      expect.any(Error),
      expect.anything(),
    )
  })
})
