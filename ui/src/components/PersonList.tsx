import { useState } from 'react'
import type { NewPerson, Person } from '../api/types'
import { ApiError } from '../api/ApiError'
import { PERSON_URL } from '../api/config'
import { useApiMutation } from '../hooks/useApiMutation'
import { useDelayedFlag } from '../hooks/useDelayedFlag'
import { RefreshIcon } from './ui/icons/RefreshIcon'
import { PersonActions } from './PersonActions'
import { PersonEditRow } from './PersonEditRow'
import { ErrorDialog } from './ui/ErrorDialog'
import { Spinner } from './ui/Spinner'
import { usePeople } from '../hooks/usePeople'

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
  const [page, setPage] = useState(1)
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const { data: persons, error, isFetching, refetch } = usePeople()
  const people = persons ?? []
  const { mutateAsync: addPerson } = useApiMutation<NewPerson>({
    method: 'POST',
    path: () => PERSON_URL,
  })
  const { mutateAsync: deletePerson } = useApiMutation<number>({
    method: 'DELETE',
    path: (id) => `${PERSON_URL}/${id}`,
  })
  const { mutateAsync: updatePersonMutation } = useApiMutation<Person>({
    method: 'PUT',
    path: () => PERSON_URL,
  })
  const [updateError, setUpdateError] = useState<string | null>(null)
  const [editingId, setEditingId] = useState<number | null>(null)
  const isEditing = editingId !== null
  const showSpinner = useDelayedFlag(isFetching, SPINNER_DELAY_MS)

  const totalPages = Math.max(1, Math.ceil(people.length / pageSize))
  const currentPage = Math.min(page, totalPages)
  const visiblePersons = people.slice((currentPage - 1) * pageSize, currentPage * pageSize)
  const selectedPerson = visiblePersons.find((p) => p.id === selectedId) ?? visiblePersons[0]

  const updatePerson = async (person: Person) => {
    try {
      await updatePersonMutation(person)
    } catch (err) {
      if (!(err instanceof ApiError && err.status === 401)) {
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

  if (error) {
    return <p className="error">{error instanceof ApiError ? error.message : 'Network error.'}</p>
  }

  return (
    <>
      <table aria-busy={isFetching} className={showSpinner ? 'is-loading' : undefined}>
        <thead>
          <tr>
            <th>Id</th>
            <th>Name</th>
            <th>Date of birth</th>
            <th>Manager Id</th>
            <th>Salary</th>
            <th aria-label="Actions"></th>
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
              <tr
                key={person.id}
                className={person.id === selectedPerson?.id ? 'selected' : undefined}
                onClick={() => setSelectedId(person.id)}
                onDoubleClick={() => startEditing(person.id)}
              >
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
                {persons === undefined
                  ? showSpinner && <Spinner label="Loading people…" />
                  : 'No persons found.'}
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
          <span aria-live="polite">TOTAL: {people.length}</span>
        </nav>
        <PersonActions
          onAdd={addPerson}
          onDelete={deletePerson}
          onEdit={() => selectedPerson && startEditing(selectedPerson.id)}
          selectedPerson={selectedPerson}
          isEditing={isEditing}
        />
        <button
          type="button"
          className="pagination refresh-button"
          disabled={isEditing || isFetching}
          onClick={() => refetch()}
        >
          Refresh
          <RefreshIcon className={isFetching ? 'spin' : undefined} />
        </button>
      </div>
      {updateError && <ErrorDialog message={updateError} onClose={() => setUpdateError(null)} />}
    </>
  )
}
