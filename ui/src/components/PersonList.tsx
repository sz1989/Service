import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Person } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { ApiError, useFetch } from '../hooks/useFetch'

export function PersonList() {
  const { token, logout } = useAuth()
  const navigate = useNavigate()
  const { data: persons, error, isLoading, execute } = useFetch<Person[]>()

  useEffect(() => {
    let cancelled = false

    execute('/V2/Person/All', {
      headers: { Authorization: `Bearer ${token}` },
    }).catch((err) => {
      if (cancelled) return

      if (err instanceof ApiError && err.status === 401) {
        logout()
        navigate('/login', { replace: true })
      }
    })

    return () => {
      cancelled = true
    }
  }, [token, execute, logout, navigate])

  if (isLoading) {
    return <p>Loading…</p>
  }

  if (error) {
    return <p className="error">{error}</p>
  }

  return (
    <table>
      <thead>
        <tr>
          <th>Id</th>
          <th>Name</th>
          <th>Date of birth</th>
          <th>Manager Id</th>
          <th>Salary</th>
        </tr>
      </thead>
      <tbody>
        {(persons ?? []).map((person) => (
          <tr key={person.id}>
            <td>{person.id}</td>
            <td>{person.name}</td>
            <td>{person.dateOfBirth}</td>
            <td>{person.managerId ?? '—'}</td>
            <td>{person.salary}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
