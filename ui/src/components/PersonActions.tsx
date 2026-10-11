import { useState } from 'react'
import type { NewPerson, Person } from '../api/types'
import { AddPersonDialog } from './AddPersonDialog'
import { PencilIcon } from './icons/PencilIcon'
import { PlusIcon } from './icons/PlusIcon'
import { TrashIcon } from './icons/TrashIcon'

interface PersonActionsProps {
  persons: Person[]
  onAdd: (person: NewPerson) => Promise<void>
  onDelete: (id: number) => void
  onUpdate: (person: Person) => void
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

export function PersonActions({ persons, onAdd, onDelete, onUpdate }: PersonActionsProps) {
  const [isAddOpen, setIsAddOpen] = useState(false)
  const hasPersons = persons.length > 0

  function handleDelete() {
    onDelete(randomExisting(persons).id)
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
        <button type="button" className="button-danger" disabled={!hasPersons} onClick={handleDelete}>
          <TrashIcon />
          Delete
        </button>
      </div>
      {isAddOpen && <AddPersonDialog
        onAdd={async (p) => {await onAdd(p); setIsAddOpen(false)}}
        onCancel={() => setIsAddOpen(false)} />}
    </>
  )
}
