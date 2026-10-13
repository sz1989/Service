import { useCallback, useEffect, useEffectEvent, useState } from 'react'

// Anything the user does with the mouse, keyboard, touch screen or scroll wheel counts as activity.
const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll', 'wheel'] as const

interface IdleLogoutOptions {
  /** While false nothing is listened to and no timer runs. Default true. */
  enabled?: boolean
  /** Warn this long before `onIdle`. Default 0: no warning. */
  warningMs?: number
}

interface IdleLogoutState {
  /** The warning period has started: show "still there?". */
  isWarning: boolean
  /** Whole seconds left before `onIdle`, counted down while `isWarning`. */
  secondsLeft: number
  /** The user answered the warning: start the whole idle period again. */
  keepAlive: () => void
}

/**
 * Calls `onIdle` once when the user has not touched the page for `idleMs`, e.g.
 * `useIdleLogout(30 * 60_000, logout, { warningMs: 60_000 })`.
 *
 * With `warningMs`, `isWarning` turns true that long before `onIdle` and `secondsLeft` counts down.
 * From then on ordinary activity is ignored on purpose (reaching for the dialog's button is
 * activity too): only `keepAlive()` starts the idle period over.
 *
 * It remembers the time of the last activity and compares against the clock, instead of trusting a
 * single long timer: browsers delay timers in background tabs and a sleeping computer stops them.
 * `onIdle` fires at most once per time `enabled` turns on, and the clock restarts from then.
 */
export function useIdleLogout(
  idleMs: number,
  onIdle: () => void,
  { enabled = true, warningMs = 0 }: IdleLogoutOptions = {},
): IdleLogoutState {
  // Always the latest callback, without restarting the clock when the caller re-renders.
  const handleIdle = useEffectEvent(onIdle)
  const [isWarning, setIsWarning] = useState(false)
  const [secondsLeft, setSecondsLeft] = useState(0)
  const [session, setSession] = useState(0) // changing it restarts the effect, and with it the clock

  const keepAlive = useCallback(() => {
    setIsWarning(false)
    setSession((current) => current + 1)
  }, [])

  useEffect(() => {
    if (!enabled) return

    let lastActivity = Date.now()
    let warned = false
    let fired = false
    let timer: ReturnType<typeof setTimeout> | undefined

    // Where are we? Idle: fire. In the warning period: show it and look again in a second.
    // Otherwise sleep until the warning (or the idle limit) is due.
    const check = () => {
      clearTimeout(timer)
      if (fired) return

      const untilIdle = idleMs - (Date.now() - lastActivity)
      if (untilIdle <= 0) {
        fired = true
        setIsWarning(false)
        handleIdle()
      } else if (warningMs > 0 && untilIdle <= warningMs) {
        warned = true
        setIsWarning(true)
        setSecondsLeft(Math.ceil(untilIdle / 1000))
        timer = setTimeout(check, Math.min(untilIdle, 1000))
      } else {
        timer = setTimeout(check, warningMs > 0 ? untilIdle - warningMs : untilIdle)
      }
    }

    const markActive = () => {
      if (!warned) lastActivity = Date.now() // cheap on purpose: mousemove fires constantly
    }

    // Coming back to a tab that was hidden, or waking the computer: do not wait for a throttled timer.
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') check()
    }

    timer = setTimeout(check, Math.max(warningMs > 0 ? idleMs - warningMs : idleMs, 0))
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
      setIsWarning(false) // disabled, restarted or unmounted: no warning outlives its clock
    }
  }, [idleMs, warningMs, enabled, session])

  return { isWarning, secondsLeft, keepAlive }
}
