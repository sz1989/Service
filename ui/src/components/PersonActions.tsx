import type { Person } from '../api/types'

interface PersonActionsProps {
  persons: Person[]
  onAdd: (person: Person) => void
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
      <button type="button" onClick={() => onAdd(randomPerson(persons))}>
        Add
      </button>
      <button type="button" disabled={!hasPersons} onClick={handleDelete}>
        Delete
      </button>
      <button type="button" disabled={!hasPersons} onClick={handleUpdate}>
        Update
      </button>
    </>
  )
}
