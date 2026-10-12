import { useEffect, useState } from 'react'
import type { NewPerson, Person } from '../api/types'
import { ApiError } from '../api/ApiError'
import { useAuthedRequest } from '../hooks/useAuthedRequest'
import { useCreate } from '../hooks/useCreate'
import { useFetch } from '../hooks/useFetch'
import { useDelayedFlag } from '../hooks/useDelayedFlag'
import { useDelete } from '../hooks/useDelete'
import { useUpdate } from '../hooks/useUpdate'
import { RefreshIcon } from './ui/icons/RefreshIcon'
import { PersonActions } from './PersonActions'
import { PersonEditRow } from './PersonEditRow'
import { ErrorDialog } from './ui/ErrorDialog'
import { Spinner } from './ui/Spinner'

const PERSON_URL = '/V2/Person'
// A load that finishes sooner than this never shows the busy indicator (it would only flash).
const SPINNER_DELAY_MS = 300

function updateErrorMessage(err: unknown): string {
  if (err instanceof ApiError) {
    if (err.status === 403) return "You don't have permission to update this person."
    if (err.status === 404) return 'This person no longer exists.'
  }
  return 'Update failed. Please try again.'
}

export function PersonList() {
  const pageSize = 4
  const { authHeaders, handleUnauthorized } = useAuthedRequest()
  const [page, setPage] = useState(1)
  const [personsState, setPersonsState] = useState<Person[]>([])
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const { data: persons, error, isLoading, execute } = useFetch<Person[]>()
  const { execute: addExecute } = useCreate<NewPerson>()
  const { execute: deleteExecute } = useDelete()
  const { execute: updateExecute } = useUpdate<Person>()
  const [updateError, setUpdateError] = useState<string | null>(null)
  const [editingId, setEditingId] = useState<number | null>(null)
  const isEditing = editingId !== null
  const showSpinner = useDelayedFlag(isLoading, SPINNER_DELAY_MS)

  const totalPages = Math.max(1, Math.ceil(personsState.length / pageSize))
  const currentPage = Math.min(page, totalPages)
  const visiblePersons = personsState.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize,
  )
  const selectedPerson = visiblePersons.find((p) => p.id === selectedId) ?? visiblePersons[0]

  const loadPersons = () => {
    execute(`${PERSON_URL}/All`, {
      headers: authHeaders,
    }).catch(handleUnauthorized)
  }

  const updatePerson = async (person: Person) => {
    try {
      const saved = await updateExecute(PERSON_URL, person, {
        headers: authHeaders,
      })
      // Patch just this row: reloading the list would send the user back to page 1.
      setPersonsState((current) => current.map((p) => (p.id === saved.id ? saved : p)))
    } catch (err) {
      if (!handleUnauthorized(err)) {
        setUpdateError(updateErrorMessage(err))
      }
      throw err
    }
  }

  const savePerson = async (person: Person) => {
    await updatePerson(person) // rejects on failure, which keeps the row open for another try
    setEditingId(null)
  }

  const startEditing = (id: number) => {
    if (editingId === null) {
      setSelectedId(id)
      setEditingId(id)
    }
  }

  const deletePerson = async (id: number) => {
    try {
      await deleteExecute(`${PERSON_URL}/${id}`, {
        headers: authHeaders,
      })
      loadPersons() // or filiter out setPersonsState((current) => current.filter((person) => person.id !== id))}
    } catch (err) {
      handleUnauthorized(err)
      throw err
    }
  }

  const addPerson = async (newPerson: NewPerson) => {
    try{
    await addExecute(PERSON_URL,
      newPerson, {
      headers: authHeaders,
    })
    loadPersons()
    } catch (err) {
      handleUnauthorized(err)
      throw err
    }
  }

  useEffect(() => {
    loadPersons()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authHeaders, execute, handleUnauthorized])

  useEffect(() => {
    setPage(1)
    setPersonsState(persons ?? [])
  }, [persons])

  if (error) {
    return <p className="error">{error}</p>
  }

  return (
    <>
    <table aria-busy={isLoading} className={showSpinner ? 'is-loading' : undefined}>
      <thead>
        <tr>
          <th>Id</th>
          <th>Name</th>
          <th>Date of birth</th>
          <th>Manager Id</th>
          <th>Salary</th>
          <th aria-label="Actions"></th>
        </tr>
      </thead>
      <tbody>
        {visiblePersons.map((person) =>
          person.id === editingId ? (
            <PersonEditRow
              key={person.id}
              person={person}
              onSave={savePerson}
              onCancel={() => setEditingId(null)}
            />
          ) : (
            <tr key={person.id}
              className={person.id === selectedPerson?.id ? 'selected': undefined}
              onClick={() => setSelectedId(person.id)}
              onDoubleClick={() => startEditing(person.id)}>
              <td>{person.id}</td>
              <td>{person.name}</td>
              <td>{person.dateOfBirth}</td>
              <td>{person.managerId ?? '—'}</td>
              <td>{person.salary}</td>
              <td />
            </tr>
          ),
        )}
        {visiblePersons.length === 0 && (
          <tr>
            <td colSpan={6}>
              {persons === null ? (
                showSpinner && <Spinner label="Loading people…" />
              ) : (
                'No persons found.'
              )}
            </td>
          </tr>
        )}
      </tbody>
    </table>
    <div className="list-toolbar">
      <nav className="pagination" aria-label="People pages">
        <button
          type="button"
          onClick={() => setPage((current) => Math.max(1, current - 1))}
          disabled={currentPage === 1 || isEditing}
        >
          Previous
        </button>
        <span aria-live="polite">
          Page {currentPage} of {totalPages}
        </span>
        {showSpinner && visiblePersons.length > 0 && <Spinner label="Loading people…" />}
        <button
          type="button"
          onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
          disabled={currentPage === totalPages || isEditing}
        >
          Next
        </button>
          <span aria-live="polite">
            TOTAL: {personsState.length}
          </span>
      </nav>
      <PersonActions
        onAdd={addPerson}
        onDelete={deletePerson}
        onEdit={() => selectedPerson && startEditing(selectedPerson.id)}
        selectedPerson={selectedPerson}
        isEditing={isEditing}
      />
      <button type="button" className="pagination refresh-button" disabled={isEditing || isLoading} onClick={loadPersons}>
        Refresh
        <RefreshIcon className={isLoading ? 'spin' : undefined} />
      </button>
    </div>
    {updateError && <ErrorDialog message={updateError} onClose={() => setUpdateError(null)} />}
    </>
  )
}
