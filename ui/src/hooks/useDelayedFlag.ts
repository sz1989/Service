import { useEffect, useState } from 'react'

// True only once `active` has stayed true for `delayMs`. Use it to show a busy indicator for slow
// work without flashing it for work that finishes quickly.
export function useDelayedFlag(active: boolean, delayMs: number): boolean {
  const [elapsed, setElapsed] = useState(false)

  useEffect(() => {
    if (!active) return

    const timer = setTimeout(() => setElapsed(true), delayMs)
    return () => {
      clearTimeout(timer)
      setElapsed(false) // the next time `active` turns on, the wait starts over
    }
  }, [active, delayMs])

  return active && elapsed
}
