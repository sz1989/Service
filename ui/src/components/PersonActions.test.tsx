import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { Person } from '../api/types'
import { PersonActions } from './PersonActions'

const person: Person = {
  id: 1,
  name: 'Alex Smith',
  dateOfBirth: '1990-01-01',
  managerId: null,
  salary: 50000,
}

describe('PersonActions', () => {
  it('disables Delete and Update when there are no persons', () => {
    render(<PersonActions persons={[]} onAdd={vi.fn()} onDelete={vi.fn()} onUpdate={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Delete' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Update' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Add' })).toBeEnabled()
  })

  it('enables all actions when there are persons', () => {
    render(<PersonActions persons={[person]} onAdd={vi.fn()} onDelete={vi.fn()} onUpdate={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Add' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Delete' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Update' })).toBeEnabled()
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
      render(<PersonActions persons={[person]} onAdd={vi.fn()} onDelete={vi.fn()} onUpdate={vi.fn()} />)

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

      await user.click(screen.getByRole('button', { name: 'Add' }))

      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('calls onAdd with the entered values and closes the dialog on Ok', async () => {
      const user = userEvent.setup()
      const onAdd = vi.fn().mockResolvedValue(undefined)
      render(<PersonActions persons={[person]} onAdd={onAdd} onDelete={vi.fn()} onUpdate={vi.fn()} />)

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
      render(<PersonActions persons={[person]} onAdd={onAdd} onDelete={vi.fn()} onUpdate={vi.fn()} />)

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await fillForm(user)
      await user.type(screen.getByLabelText('Manager id'), '3')
      await user.click(screen.getByRole('button', { name: 'Ok' }))

      expect(onAdd).toHaveBeenCalledWith(expect.objectContaining({ managerId: 3 }))
    })

    it('does not call onAdd when required fields are empty', async () => {
      const user = userEvent.setup()
      const onAdd = vi.fn().mockResolvedValue(undefined)
      render(<PersonActions persons={[person]} onAdd={onAdd} onDelete={vi.fn()} onUpdate={vi.fn()} />)

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await user.click(screen.getByRole('button', { name: 'Ok' }))

      expect(onAdd).not.toHaveBeenCalled()
      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('closes without calling onAdd on Cancel', async () => {
      const user = userEvent.setup()
      const onAdd = vi.fn().mockResolvedValue(undefined)
      render(<PersonActions persons={[person]} onAdd={onAdd} onDelete={vi.fn()} onUpdate={vi.fn()} />)

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await user.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(onAdd).not.toHaveBeenCalled()
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('stays open, shows the error and re-enables Ok when onAdd fails', async () => {
      const user = userEvent.setup()
      const onAdd = vi.fn().mockRejectedValue(new Error('Request failed with status 403.'))
      render(<PersonActions persons={[person]} onAdd={onAdd} onDelete={vi.fn()} onUpdate={vi.fn()} />)

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
      render(<PersonActions persons={[person]} onAdd={onAdd} onDelete={vi.fn()} onUpdate={vi.fn()} />)

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await fillForm(user)
      await user.click(screen.getByRole('button', { name: 'Ok' }))

      expect(screen.getByRole('button', { name: 'Ok' })).toBeDisabled()

      finishSave()
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    })
  })

  it('calls onDelete with the only existing person id', async () => {
    const user = userEvent.setup()
    const onDelete = vi.fn()

    render(<PersonActions persons={[person]} onAdd={vi.fn()} onDelete={onDelete} onUpdate={vi.fn()} />)

    await user.click(screen.getByRole('button', { name: 'Delete' }))

    expect(onDelete).toHaveBeenCalledWith(person.id)
  })
})
