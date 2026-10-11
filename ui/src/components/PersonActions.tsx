import { useState } from 'react'
import type { NewPerson, Person } from '../api/types'
import { ApiError } from '../hooks/useFetch'
import { AddPersonDialog } from './AddPersonDialog'
import { ConfirmDialog } from './ui/ConfirmDialog'
import { ErrorDialog } from './ui/ErrorDialog'
import { PencilIcon } from './ui/icons/PencilIcon'
import { PlusIcon } from './ui/icons/PlusIcon'
import { TrashIcon } from './ui/icons/TrashIcon'

interface PersonActionsProps {
  persons: Person[]
  onAdd: (person: NewPerson) => Promise<void>
  onDelete: (id: number) => Promise<void>
  onUpdate: (person: Person) => void
  selectedPerson: Person | undefined
}

const FIRST_NAMES = ['Alex', 'Sam', 'Jordan', 'Taylor', 'Casey', 'Morgan']
const LAST_NAMES = ['Smith', 'Johnson', 'Lee', 'Brown', 'Garcia', 'Nguyen']

function randomPerson(persons: Person[]): Person {
  const nextId = persons.reduce((maxId, person) => Math.max(maxId, person.id), 0) + 1
  const firstName = FIRST_NAMES[Math.floor(Math.random() * FIRST_NAMES.length)]
  const lastName = LAST_NAMES[Math.floor(Math.random() * LAST_NAMES.length)]
  const year = 1970 + Math.floor(Math.random() * 40)
  const month = String(1 + Math.floor(Math.random() * 12)).padStart(2, '0')
  const day = String(1 + Math.floor(Math.random() * 28)).padStart(2, '0')

  return {
    id: nextId,
    name: `${firstName} ${lastName}`,
    dateOfBirth: `${year}-${month}-${day}`,
    managerId: null,
    salary: 40000 + Math.floor(Math.random() * 60000),
  }
}

function randomExisting(persons: Person[]): Person {
  return persons[Math.floor(Math.random() * persons.length)]
}

function deleteErrorMessage(err: unknown): string {
  if (err instanceof ApiError) {
    if (err.status === 403) return "You don't have permission to delete this person."
    if (err.status === 404) return 'This person no longer exists.'
  }
  return 'Delete failed. Please try again.'
}

export function PersonActions({ persons, onAdd, onDelete, onUpdate, selectedPerson }: PersonActionsProps) {
  const [isAddOpen, setIsAddOpen] = useState(false)
  const [personToDelete, setPersonToDelete] = useState<Person | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const hasPersons = persons.length > 0

  async function confirmDelete() {
    if (!personToDelete) return
    const { id } = personToDelete
    setPersonToDelete(null)
    try {
      await onDelete(id)
    } catch (err) {
      setDeleteError(deleteErrorMessage(err))
    }
  }

  function handleUpdate() {
    const target = randomExisting(persons)
    onUpdate({ ...randomPerson(persons), id: target.id })
  }

  return (
    <>
      <div className="person-actions">
        <button type="button" onClick={() => setIsAddOpen(true)}>
          <PlusIcon />
          Add
        </button>
        <button type="button" className="button-secondary" disabled={!hasPersons} onClick={handleUpdate}>
          <PencilIcon />
          Update
        </button>
        <button type="button" className="button-danger" disabled={!selectedPerson} onClick={() => setPersonToDelete(selectedPerson ?? null)}>
          <TrashIcon />
          Delete
        </button>
      </div>
      {isAddOpen && <AddPersonDialog
        onAdd={async (p) => {await onAdd(p); setIsAddOpen(false)}}
        onCancel={() => setIsAddOpen(false)} />}
      {personToDelete && <ConfirmDialog
        title='Delete Person'
        message={`Delete ${personToDelete.name} (id ${personToDelete.id})? This cannot be undone.`}
        confirmLabel='Delete'
        onConfirm={confirmDelete}
        onCancel={() => setPersonToDelete(null)} />}
      {deleteError && <ErrorDialog message={deleteError} onClose={() => setDeleteError(null)} />}
    </>
  )
}
