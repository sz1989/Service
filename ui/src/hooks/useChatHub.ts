import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { useCallback, useRef } from 'react'
import { HUB_URL } from '../api/config'

export function useChatHub(token: string | null) {
  const connectionRef = useRef<HubConnection | null>(null)
  const startPromiseRef = useRef<Promise<HubConnection> | null>(null)
  const tokenRef = useRef(token)
  tokenRef.current = token

  const getConnection = useCallback((): Promise<HubConnection> => {
    if (startPromiseRef.current) {
      return startPromiseRef.current
    }

    if (!connectionRef.current) {
      connectionRef.current = new HubConnectionBuilder()
        .withUrl(HUB_URL, { accessTokenFactory: () => tokenRef.current ?? '' })
        .build()
    }

    const connection = connectionRef.current
    const startPromise =
      connection.state === HubConnectionState.Disconnected
        ? connection.start().then(() => connection)
        : Promise.resolve(connection)

    startPromiseRef.current = startPromise.finally(() => {
      startPromiseRef.current = null
    })

    return startPromiseRef.current
  }, [])

  return { getConnection }
}
