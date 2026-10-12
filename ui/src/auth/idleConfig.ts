// The server's login token lasts 1 hour in total (AuthService), so an idle limit above that is moot.
export const IDLE_LOGOUT_MS = 15 * 60_000

// How long before the logout the "still there?" dialog appears.
export const IDLE_WARNING_MS = 60_000
