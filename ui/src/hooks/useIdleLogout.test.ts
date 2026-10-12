import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useIdleLogout } from './useIdleLogout'

const IDLE_MS = 60_000

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.restoreAllMocks()
  vi.useRealTimers()
})

function advance(ms: number) {
  act(() => {
    vi.advanceTimersByTime(ms)
  })
}

function touch(name = 'keydown') {
  window.dispatchEvent(new Event(name))
}

describe('useIdleLogout', () => {
  it('calls onIdle once the user has been idle for the whole period', () => {
    const onIdle = vi.fn()
    renderHook(() => useIdleLogout(IDLE_MS, onIdle))

    advance(IDLE_MS - 1)
    expect(onIdle).not.toHaveBeenCalled()

    advance(1)
    expect(onIdle).toHaveBeenCalledTimes(1)
  })

  it('starts the clock over whenever the user does something', () => {
    const onIdle = vi.fn()
    renderHook(() => useIdleLogout(IDLE_MS, onIdle))

    advance(40_000)
    touch()
    advance(40_000) // 80 s since the start, but only 40 s since the last activity
    expect(onIdle).not.toHaveBeenCalled()

    advance(20_000)
    expect(onIdle).toHaveBeenCalledTimes(1)
  })

  it.each(['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll', 'wheel'])(
    'counts %s as activity',
    (name) => {
      const onIdle = vi.fn()
      renderHook(() => useIdleLogout(IDLE_MS, onIdle))

      advance(50_000)
      touch(name)
      advance(50_000)

      expect(onIdle).not.toHaveBeenCalled()
    },
  )

  it('counts activity inside an element, including scrolling that does not bubble', () => {
    const onIdle = vi.fn()
    renderHook(() => useIdleLogout(IDLE_MS, onIdle))
    const box = document.createElement('div')
    document.body.append(box)

    advance(50_000)
    box.dispatchEvent(new Event('scroll')) // scroll events do not bubble
    advance(50_000)
    expect(onIdle).not.toHaveBeenCalled()

    box.remove()
  })

  it('calls onIdle only once, however long the user stays away', () => {
    const onIdle = vi.fn()
    renderHook(() => useIdleLogout(IDLE_MS, onIdle))

    advance(IDLE_MS * 5)

    expect(onIdle).toHaveBeenCalledTimes(1)
  })

  it('does nothing while disabled, and starts the clock when it is enabled', () => {
    const onIdle = vi.fn()
    const { rerender } = renderHook(({ enabled }) => useIdleLogout(IDLE_MS, onIdle, { enabled }), {
      initialProps: { enabled: false },
    })

    advance(IDLE_MS * 2)
    expect(onIdle).not.toHaveBeenCalled()

    rerender({ enabled: true })
    advance(IDLE_MS - 1)
    expect(onIdle).not.toHaveBeenCalled()
    advance(1)
    expect(onIdle).toHaveBeenCalledTimes(1)
  })

  it('stops counting when it is disabled again, and waits a full period after the next enable', () => {
    const onIdle = vi.fn()
    const { rerender } = renderHook(({ enabled }) => useIdleLogout(IDLE_MS, onIdle, { enabled }), {
      initialProps: { enabled: true },
    })

    advance(50_000)
    rerender({ enabled: false })
    advance(IDLE_MS)
    expect(onIdle).not.toHaveBeenCalled()

    rerender({ enabled: true })
    advance(IDLE_MS - 1)
    expect(onIdle).not.toHaveBeenCalled()
    advance(1)
    expect(onIdle).toHaveBeenCalledTimes(1)
  })

  it('stops everything when the component unmounts', () => {
    const onIdle = vi.fn()
    const { unmount } = renderHook(() => useIdleLogout(IDLE_MS, onIdle))

    unmount()
    advance(IDLE_MS * 2)

    expect(onIdle).not.toHaveBeenCalled()
    expect(vi.getTimerCount()).toBe(0)
  })

  it('removes its listeners on unmount', () => {
    const add = vi.spyOn(window, 'addEventListener')
    const remove = vi.spyOn(window, 'removeEventListener')
    const { unmount } = renderHook(() => useIdleLogout(IDLE_MS, vi.fn()))

    unmount()

    const removed = remove.mock.calls.map(([name]) => name)
    for (const [name] of add.mock.calls.filter(([n]) => n !== 'unhandledrejection')) {
      expect(removed).toContain(name)
    }
  })

  it('calls the latest onIdle without restarting the clock when the callback changes', () => {
    const first = vi.fn()
    const second = vi.fn()
    const { rerender } = renderHook(({ cb }) => useIdleLogout(IDLE_MS, cb), {
      initialProps: { cb: first },
    })

    advance(40_000)
    rerender({ cb: second })
    advance(20_000) // 60 s since the start, not since the re-render

    expect(first).not.toHaveBeenCalled()
    expect(second).toHaveBeenCalledTimes(1)
  })

  describe('when the tab was hidden or the computer slept', () => {
    function setVisibility(state: 'visible' | 'hidden') {
      vi.spyOn(document, 'visibilityState', 'get').mockReturnValue(state)
      document.dispatchEvent(new Event('visibilitychange'))
    }

    it('checks right away when the tab becomes visible, without waiting for a delayed timer', () => {
      const onIdle = vi.fn()
      renderHook(() => useIdleLogout(IDLE_MS, onIdle))

      // The clock jumps ahead (sleep) but the browser has not run the timer yet.
      vi.setSystemTime(Date.now() + IDLE_MS + 1)
      expect(onIdle).not.toHaveBeenCalled()

      act(() => setVisibility('visible'))

      expect(onIdle).toHaveBeenCalledTimes(1)
    })

    it('does not fire when the tab becomes visible before the period is over', () => {
      const onIdle = vi.fn()
      renderHook(() => useIdleLogout(IDLE_MS, onIdle))

      advance(10_000)
      act(() => setVisibility('visible'))
      advance(IDLE_MS - 10_000 - 1)

      expect(onIdle).not.toHaveBeenCalled()
      advance(1)
      expect(onIdle).toHaveBeenCalledTimes(1)
    })

    it('does not check while the tab is hidden', () => {
      const onIdle = vi.fn()
      renderHook(() => useIdleLogout(IDLE_MS, onIdle))

      vi.setSystemTime(Date.now() + IDLE_MS + 1)
      act(() => setVisibility('hidden'))

      expect(onIdle).not.toHaveBeenCalled()
    })
  })

  describe('warning before the logout', () => {
    const WARNING_MS = 10_000

    function setup(onIdle = vi.fn()) {
      const hook = renderHook(() => useIdleLogout(IDLE_MS, onIdle, { warningMs: WARNING_MS }))
      return { ...hook, onIdle }
    }

    it('does not warn when no warning period is configured', () => {
      const { result } = renderHook(() => useIdleLogout(IDLE_MS, vi.fn()))

      advance(IDLE_MS - 1)

      expect(result.current.isWarning).toBe(false)
    })

    it('starts the warning when the warning period begins and counts the seconds down', () => {
      const { result } = setup()

      advance(IDLE_MS - WARNING_MS - 1)
      expect(result.current.isWarning).toBe(false)

      advance(1)
      expect(result.current.isWarning).toBe(true)
      expect(result.current.secondsLeft).toBe(10)

      advance(1000)
      expect(result.current.secondsLeft).toBe(9)
      advance(4000)
      expect(result.current.secondsLeft).toBe(5)
    })

    it('calls onIdle at the end of the warning period and clears the warning', () => {
      const { result, onIdle } = setup()

      advance(IDLE_MS - 1)
      expect(onIdle).not.toHaveBeenCalled()
      expect(result.current.secondsLeft).toBe(1)

      advance(1)
      expect(onIdle).toHaveBeenCalledTimes(1)
      expect(result.current.isWarning).toBe(false)
    })

    it('does not warn when the user was active before the warning period', () => {
      const { result } = setup()

      advance(IDLE_MS - WARNING_MS - 1000)
      touch()
      advance(WARNING_MS) // it would be warning by now without the activity

      expect(result.current.isWarning).toBe(false)
    })

    it('ignores ordinary activity once the warning has started', () => {
      const { result, onIdle } = setup()
      advance(IDLE_MS - WARNING_MS)
      expect(result.current.isWarning).toBe(true)

      touch('mousemove') // e.g. the mouse moving towards the dialog
      advance(WARNING_MS)

      expect(onIdle).toHaveBeenCalledTimes(1)
    })

    it('keepAlive ends the warning and starts the whole period over', () => {
      const { result, onIdle } = setup()
      advance(IDLE_MS - WARNING_MS)
      expect(result.current.isWarning).toBe(true)

      act(() => result.current.keepAlive())
      expect(result.current.isWarning).toBe(false)

      advance(IDLE_MS - WARNING_MS - 1)
      expect(result.current.isWarning).toBe(false)
      advance(1)
      expect(result.current.isWarning).toBe(true) // warned again after another full quiet period
      advance(WARNING_MS)
      expect(onIdle).toHaveBeenCalledTimes(1)
    })

    it('warns right away when the tab becomes visible during the warning period', () => {
      const { result } = setup()

      vi.setSystemTime(Date.now() + IDLE_MS - 5000) // sleep: the timer has not run
      vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible')
      act(() => {
        document.dispatchEvent(new Event('visibilitychange'))
      })

      expect(result.current.isWarning).toBe(true)
      expect(result.current.secondsLeft).toBe(5)
    })

    it('logs out instead of warning when the tab becomes visible after the whole period', () => {
      const { result, onIdle } = setup()

      vi.setSystemTime(Date.now() + IDLE_MS + 1)
      vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible')
      act(() => {
        document.dispatchEvent(new Event('visibilitychange'))
      })

      expect(onIdle).toHaveBeenCalledTimes(1)
      expect(result.current.isWarning).toBe(false)
    })

    it('drops the warning when it is disabled', () => {
      const onIdle = vi.fn()
      const { result, rerender } = renderHook(
        ({ enabled }) => useIdleLogout(IDLE_MS, onIdle, { enabled, warningMs: WARNING_MS }),
        { initialProps: { enabled: true } },
      )
      advance(IDLE_MS - WARNING_MS)
      expect(result.current.isWarning).toBe(true)

      rerender({ enabled: false })

      expect(result.current.isWarning).toBe(false)
      advance(IDLE_MS)
      expect(onIdle).not.toHaveBeenCalled()
    })

    it('warns at once when the warning period is as long as the idle period', () => {
      const { result } = renderHook(() => useIdleLogout(5000, vi.fn(), { warningMs: 5000 }))

      advance(0)

      expect(result.current.isWarning).toBe(true)
      expect(result.current.secondsLeft).toBe(5)
    })
  })
})
