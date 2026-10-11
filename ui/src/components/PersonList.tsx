import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { NewPerson, Person } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { ApiError, useCreate, useFetch } from '../hooks/useFetch'
import { useDelete } from '../hooks/useDelete'
import { RefreshIcon } from './ui/icons/RefreshIcon'
import { PersonActions } from './PersonActions'

const PERSON_URL = '/V2/Person'

export function PersonList() {
  const pageSize = 4
  const { token, logout } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [personsState, setPersonsState] = useState<Person[]>([])
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const { data: persons, error, isLoading, execute } = useFetch<Person[]>()
  const { execute: addExecute } = useCreate<NewPerson>()
  const { execute: deleteExecute } = useDelete()

  const totalPages = Math.max(1, Math.ceil(personsState.length / pageSize))
  const currentPage = Math.min(page, totalPages)
  const visiblePersons = personsState.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize,
  )
  const selectedPerson = visiblePersons.find((p) => p.id === selectedId) ?? visiblePersons[0]

  const loadPersons = () => {
    execute(`${PERSON_URL}/All`, {
      headers: { Authorization: `Bearer ${token}` },
    }).catch((err) => {
      if (err instanceof ApiError && err.status === 401) {
        logout()
        navigate('/login', { replace: true })
      }
    })
  }

  const deletePerson = async (id: number) => {
    try {
      await deleteExecute(`${PERSON_URL}/${id}`, {
        headers: { Authorization: `Bearer ${token}` },
      })
      loadPersons() // or filiter out setPersonsState((current) => current.filter((person) => person.id !== id))}
      } catch(err) {
      // console.error
      if (err instanceof ApiError && err.status === 401) {
        logout()
        navigate('/login', { replace: true })
      } 
      throw err
    }
  }

  const addPerson = async (newPerson: NewPerson) => {
    try{
    await addExecute(PERSON_URL,
      newPerson, {
      headers: { Authorization: `Bearer ${token}` },
    })
    loadPersons()
    } catch(err) {
      if (err instanceof ApiError && err.status === 401) {
        logout()
        navigate('/login', { replace: true })
      } 
      throw err
    }
  }

  useEffect(() => {
    loadPersons()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, execute, logout, navigate])

  useEffect(() => {
    setPage(1)
    setPersonsState(persons ?? [])
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
          <tr key={person.id} 
            className={person.id === selectedPerson?.id ? 'selected': undefined} 
            onClick={() => setSelectedId(person.id)}>
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
    <div className="list-toolbar">
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
      <PersonActions
        persons={personsState}
        onAdd={addPerson}
        onDelete={deletePerson}
        onUpdate={(updated) =>
          setPersonsState((current) =>
            current.map((person) => (person.id === updated.id ? updated : person)),
          )
        }
        selectedPerson = {selectedPerson}
      />
      <button type="button" className="pagination refresh-button" onClick={loadPersons}>
        Refresh
        <RefreshIcon />
      </button>
    </div>
    </>
  )
}
