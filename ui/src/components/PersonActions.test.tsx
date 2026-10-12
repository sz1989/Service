import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { Person } from '../api/types'
import { ApiError } from '../api/ApiError'
import { PersonActions } from './PersonActions'

const person: Person = {
  id: 1,
  name: 'Alex Smith',
  dateOfBirth: '1990-01-01',
  managerId: null,
  salary: 50000,
}

describe('PersonActions', () => {
  it('disables Edit and Delete when no person is selected', () => {
    render(<PersonActions onAdd={vi.fn()} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={undefined} isEditing={false} />)

    expect(screen.getByRole('button', { name: 'Edit' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Delete' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Add' })).toBeEnabled()
  })

  it('enables all actions when a person is selected', () => {
    render(<PersonActions onAdd={vi.fn()} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

    expect(screen.getByRole('button', { name: 'Add' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Edit' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Delete' })).toBeEnabled()
  })

  it('calls onEdit when Edit is clicked', async () => {
    const user = userEvent.setup()
    const onEdit = vi.fn()
    render(<PersonActions onAdd={vi.fn()} onDelete={vi.fn()} onEdit={onEdit} selectedPerson={person} isEditing={false} />)

    await user.click(screen.getByRole('button', { name: 'Edit' }))

    expect(onEdit).toHaveBeenCalledTimes(1)
  })

  it('disables Add, Edit and Delete while a row is being edited', () => {
    render(<PersonActions onAdd={vi.fn()} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={person} isEditing={true} />)

    expect(screen.getByRole('button', { name: 'Add' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Edit' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Delete' })).toBeDisabled()
  })

  describe('Add dialog', () => {
    async function fillForm(user: ReturnType<typeof userEvent.setup>) {
      await user.type(screen.getByLabelText('Name'), 'Jamie Lee')
      const dateOfBirth = screen.getByLabelText('Date of birth')
      await user.clear(dateOfBirth) // it defaults to today
      await user.type(dateOfBirth, '1985-06-15')
      await user.type(screen.getByLabelText('Salary'), '72000')
    }

    it('opens the dialog when Add is clicked', async () => {
      const user = userEvent.setup()
      render(<PersonActions onAdd={vi.fn()} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

      await user.click(screen.getByRole('button', { name: 'Add' }))

      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('calls onAdd with the entered values and closes the dialog on Ok', async () => {
      const user = userEvent.setup()
      const onAdd = vi.fn().mockResolvedValue(undefined)
      render(<PersonActions onAdd={onAdd} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await fillForm(user)
      await user.click(screen.getByRole('button', { name: 'Ok' }))

      expect(onAdd).toHaveBeenCalledTimes(1)
      expect(onAdd).toHaveBeenCalledWith({
        name: 'Jamie Lee',
        dateOfBirth: '1985-06-15',
        managerId: null,
        salary: 72000,
      })
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    })

    it('sends the manager id when one is entered', async () => {
      const user = userEvent.setup()
      const onAdd = vi.fn().mockResolvedValue(undefined)
      render(<PersonActions onAdd={onAdd} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await fillForm(user)
      await user.type(screen.getByLabelText('Manager id'), '3')
      await user.click(screen.getByRole('button', { name: 'Ok' }))

      expect(onAdd).toHaveBeenCalledWith(expect.objectContaining({ managerId: 3 }))
    })

    it('does not call onAdd when required fields are empty', async () => {
      const user = userEvent.setup()
      const onAdd = vi.fn().mockResolvedValue(undefined)
      render(<PersonActions onAdd={onAdd} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await user.click(screen.getByRole('button', { name: 'Ok' }))

      expect(onAdd).not.toHaveBeenCalled()
      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('closes without calling onAdd on Cancel', async () => {
      const user = userEvent.setup()
      const onAdd = vi.fn().mockResolvedValue(undefined)
      render(<PersonActions onAdd={onAdd} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await user.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(onAdd).not.toHaveBeenCalled()
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('stays open, shows the error and re-enables Ok when onAdd fails', async () => {
      const user = userEvent.setup()
      const onAdd = vi.fn().mockRejectedValue(new Error('Request failed with status 403.'))
      render(<PersonActions onAdd={onAdd} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await fillForm(user)
      await user.click(screen.getByRole('button', { name: 'Ok' }))

      expect(await screen.findByRole('alert')).toHaveTextContent('Request failed with status 403.')
      expect(screen.getByRole('dialog')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Ok' })).toBeEnabled()
    })

    it('disables Ok while the save is in progress', async () => {
      const user = userEvent.setup()
      let finishSave: () => void = () => {}
      const onAdd = vi.fn().mockReturnValue(new Promise<void>((resolve) => { finishSave = resolve }))
      render(<PersonActions onAdd={onAdd} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await fillForm(user)
      await user.click(screen.getByRole('button', { name: 'Ok' }))

      expect(screen.getByRole('button', { name: 'Ok' })).toBeDisabled()

      finishSave()
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    })
  })

  describe('Delete', () => {
    const other: Person = { ...person, id: 2, name: 'Sam Lee' }

    // The toolbar also has a Delete button, so look for the confirm button inside the dialog.
    async function confirmDelete(user: ReturnType<typeof userEvent.setup>) {
      await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Delete' }))
    }

    it('asks for confirmation before deleting and names the selected person', async () => {
      const user = userEvent.setup()
      const onDelete = vi.fn().mockResolvedValue(undefined)

      render(<PersonActions onAdd={vi.fn()} onDelete={onDelete} onEdit={vi.fn()} selectedPerson={other} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Delete' }))

      expect(screen.getByRole('dialog')).toHaveTextContent('Sam Lee')
      expect(onDelete).not.toHaveBeenCalled()
    })

    it('calls onDelete with the selected person id once confirmed', async () => {
      const user = userEvent.setup()
      const onDelete = vi.fn().mockResolvedValue(undefined)

      render(<PersonActions onAdd={vi.fn()} onDelete={onDelete} onEdit={vi.fn()} selectedPerson={other} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Delete' }))
      await confirmDelete(user)

      expect(onDelete).toHaveBeenCalledTimes(1)
      expect(onDelete).toHaveBeenCalledWith(other.id)
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    })

    it('does not call onDelete when the confirmation is cancelled', async () => {
      const user = userEvent.setup()
      const onDelete = vi.fn().mockResolvedValue(undefined)

      render(<PersonActions onAdd={vi.fn()} onDelete={onDelete} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Delete' }))
      await user.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(onDelete).not.toHaveBeenCalled()
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('disables Delete when nothing is selected', () => {
      render(<PersonActions onAdd={vi.fn()} onDelete={vi.fn()} onEdit={vi.fn()} selectedPerson={undefined} isEditing={false} />)

      expect(screen.getByRole('button', { name: 'Delete' })).toBeDisabled()
    })

    it.each([
      [new ApiError(403, 'Request failed with status 403.'), "You don't have permission to delete this person."],
      [new ApiError(404, 'Request failed with status 404.'), 'This person no longer exists.'],
      [new ApiError(500, 'Request failed with status 500.'), 'Delete failed. Please try again.'],
      [new Error('Network error.'), 'Delete failed. Please try again.'],
    ])('shows an error dialog when onDelete fails with %s', async (error, expectedMessage) => {
      const user = userEvent.setup()
      const onDelete = vi.fn().mockRejectedValue(error)

      render(<PersonActions onAdd={vi.fn()} onDelete={onDelete} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Delete' }))
      await confirmDelete(user)

      expect(await screen.findByRole('alert')).toHaveTextContent(expectedMessage)
      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('closes the error dialog on Ok', async () => {
      const user = userEvent.setup()
      const onDelete = vi.fn().mockRejectedValue(new ApiError(403, 'Request failed with status 403.'))

      render(<PersonActions onAdd={vi.fn()} onDelete={onDelete} onEdit={vi.fn()} selectedPerson={person} isEditing={false} />)

      await user.click(screen.getByRole('button', { name: 'Delete' }))
      await confirmDelete(user)
      await screen.findByRole('alert')
      await user.click(screen.getByRole('button', { name: 'Ok' }))

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })
})
