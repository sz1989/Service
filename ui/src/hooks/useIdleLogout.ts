import { useEffect, useEffectEvent } from 'react'

// Anything the user does with the mouse, keyboard, touch screen or scroll wheel counts as activity.
const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll', 'wheel'] as const

/**
 * Calls `onIdle` once when the user has not touched the page for `idleMs`.
 *
 * The caller decides what idle means, e.g. `useIdleLogout(30 * 60_000, logout, isAuthenticated)`.
 * While `enabled` is false nothing is listened to and no timer runs. `onIdle` fires at most once
 * per time `enabled` turns on, and the clock restarts from then.
 *
 * It remembers the time of the last activity and compares against the clock, instead of trusting a
 * single long timer: browsers delay timers in background tabs and a sleeping computer stops them.
 */
export function useIdleLogout(idleMs: number, onIdle: () => void, enabled = true) {
  // Always the latest callback, without restarting the clock when the caller re-renders.
  const handleIdle = useEffectEvent(onIdle)

  useEffect(() => {
    if (!enabled) return

    let lastActivity = Date.now()
    let fired = false
    let timer: ReturnType<typeof setTimeout> | undefined

    // Idle for long enough? Fire. Otherwise look again when the remaining time has passed.
    const check = () => {
      clearTimeout(timer)
      if (fired) return

      const remaining = idleMs - (Date.now() - lastActivity)
      if (remaining <= 0) {
        fired = true
        handleIdle()
      } else {
        timer = setTimeout(check, remaining)
      }
    }

    const markActive = () => {
      lastActivity = Date.now() // cheap on purpose: mousemove fires hundreds of times a second
    }

    // Coming back to a tab that was hidden, or waking the computer: do not wait for a throttled timer.
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') check()
    }

    timer = setTimeout(check, idleMs)
    // capture: scrolling inside an element does not bubble up to the window
    for (const name of ACTIVITY_EVENTS) {
      window.addEventListener(name, markActive, { capture: true, passive: true })
    }
    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      clearTimeout(timer)
      for (const name of ACTIVITY_EVENTS) {
        window.removeEventListener(name, markActive, { capture: true })
      }
      document.removeEventListener('visibilitychange', handleVisibilityChange)
    }
  }, [idleMs, enabled])
}
