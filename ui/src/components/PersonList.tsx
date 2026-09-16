import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Person } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { ApiError, useFetch } from '../hooks/useFetch'

export function PersonList() {
  const pageSize = 4
  const { token, logout } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const { data: persons, error, isLoading, execute } = useFetch<Person[]>()

  const totalPages = Math.max(1, Math.ceil((persons?.length ?? 0) / pageSize))
  const currentPage = Math.min(page, totalPages)
  const visiblePersons = (persons ?? []).slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize,
  )

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

  useEffect(() => {
    setPage(1)
  }, [persons])

  if (isLoading) {
    return <p>Loading…</p>
  }

  if (error) {
    return <p className="error">{error}</p>
  }

  return (
    <>
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
        {visiblePersons.map((person) => (
          <tr key={person.id}>
            <td>{person.id}</td>
            <td>{person.name}</td>
            <td>{person.dateOfBirth}</td>
            <td>{person.managerId ?? '—'}</td>
            <td>{person.salary}</td>
          </tr>
        ))}
        {visiblePersons.length === 0 && (
          <tr>
            <td colSpan={5}>No persons found.</td>
          </tr>
        )}
      </tbody>
    </table>
    <nav className="pagination" aria-label="People pages">
      <button
        type="button"
        onClick={() => setPage((current) => Math.max(1, current - 1))}
        disabled={currentPage === 1}
      >
        Previous
      </button>
      <span aria-live="polite">
        Page {currentPage} of {totalPages}
      </span>
      <button
        type="button"
        onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
        disabled={currentPage === totalPages}
      >
        Next
      </button>
    </nav>
    </>
  )
}
