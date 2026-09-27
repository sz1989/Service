import { render, screen } from '@testing-library/react'
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

  it('enables all actions and calls onAdd when Add is clicked', async () => {
    const user = userEvent.setup()
    const onAdd = vi.fn()

    render(<PersonActions persons={[person]} onAdd={onAdd} onDelete={vi.fn()} onUpdate={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Delete' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Update' })).toBeEnabled()

    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(onAdd).toHaveBeenCalledTimes(1)
  })

  it('calls onDelete with the only existing person id', async () => {
    const user = userEvent.setup()
    const onDelete = vi.fn()

    render(<PersonActions persons={[person]} onAdd={vi.fn()} onDelete={onDelete} onUpdate={vi.fn()} />)

    await user.click(screen.getByRole('button', { name: 'Delete' }))

    expect(onDelete).toHaveBeenCalledWith(person.id)
  })
})
