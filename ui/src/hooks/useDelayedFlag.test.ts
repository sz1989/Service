import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useDelayedFlag } from './useDelayedFlag'

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

function advance(ms: number) {
  act(() => {
    vi.advanceTimersByTime(ms)
  })
}

describe('useDelayedFlag', () => {
  it('is false while inactive', () => {
    const { result } = renderHook(() => useDelayedFlag(false, 300))

    advance(1000)

    expect(result.current).toBe(false)
  })

  it('turns true only after the delay has passed', () => {
    const { result } = renderHook(() => useDelayedFlag(true, 300))

    expect(result.current).toBe(false)
    advance(299)
    expect(result.current).toBe(false)
    advance(1)
    expect(result.current).toBe(true)
  })

  it('never turns true when the work finishes before the delay', () => {
    const { result, rerender } = renderHook(({ active }) => useDelayedFlag(active, 300), {
      initialProps: { active: true },
    })

    advance(200)
    rerender({ active: false })
    advance(1000)

    expect(result.current).toBe(false)
  })

  it('turns false as soon as the work finishes', () => {
    const { result, rerender } = renderHook(({ active }) => useDelayedFlag(active, 300), {
      initialProps: { active: true },
    })
    advance(300)
    expect(result.current).toBe(true)

    rerender({ active: false })

    expect(result.current).toBe(false)
  })

  it('waits the full delay again the next time it becomes active', () => {
    const { result, rerender } = renderHook(({ active }) => useDelayedFlag(active, 300), {
      initialProps: { active: true },
    })
    advance(300)
    rerender({ active: false })

    rerender({ active: true })
    expect(result.current).toBe(false)
    advance(299)
    expect(result.current).toBe(false)
    advance(1)
    expect(result.current).toBe(true)
  })
})
