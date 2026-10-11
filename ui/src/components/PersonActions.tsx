import { useState } from 'react'
import type { NewPerson, Person } from '../api/types'
import { ApiError } from '../api/ApiError'
import { AddPersonDialog } from './AddPersonDialog'
import { ConfirmDialog } from './ui/ConfirmDialog'
import { ErrorDialog } from './ui/ErrorDialog'
import { PencilIcon } from './ui/icons/PencilIcon'
import { PlusIcon } from './ui/icons/PlusIcon'
import { TrashIcon } from './ui/icons/TrashIcon'

interface PersonActionsProps {
  onAdd: (person: NewPerson) => Promise<void>
  onDelete: (id: number) => Promise<void>
  /** Start editing the selected person's row. */
  onEdit: () => void
  selectedPerson: Person | undefined
  /** A row is being edited: every action is disabled until it is saved or cancelled. */
  isEditing: boolean
}

function deleteErrorMessage(err: unknown): string {
  if (err instanceof ApiError) {
    if (err.status === 403) return "You don't have permission to delete this person."
    if (err.status === 404) return 'This person no longer exists.'
  }
  return 'Delete failed. Please try again.'
}

export function PersonActions({ onAdd, onDelete, onEdit, selectedPerson, isEditing }: PersonActionsProps) {
  const [isAddOpen, setIsAddOpen] = useState(false)
  const [personToDelete, setPersonToDelete] = useState<Person | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)

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

  return (
    <>
      <div className="person-actions">
        <button type="button" disabled={isEditing} onClick={() => setIsAddOpen(true)}>
          <PlusIcon />
          Add
        </button>
        <button type="button" className="button-secondary" disabled={!selectedPerson || isEditing} onClick={onEdit}>
          <PencilIcon />
          Edit
        </button>
        <button type="button" className="button-danger" disabled={!selectedPerson || isEditing} onClick={() => setPersonToDelete(selectedPerson ?? null)}>
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
