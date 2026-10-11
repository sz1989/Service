import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { EditableCell } from './EditableCell'

type CellProps = Partial<Parameters<typeof EditableCell>[0]>

function renderCell(props: CellProps = {}) {
  const onSave = props.onSave ?? vi.fn().mockResolvedValue(undefined)
  render(
    <table>
      <tbody>
        <tr>
          <EditableCell value="Alex" label="Name" {...props} onSave={onSave} />
        </tr>
      </tbody>
    </table>,
  )
  return { onSave }
}

async function startEditing(user: ReturnType<typeof userEvent.setup>, text = 'Alex') {
  await user.dblClick(screen.getByText(text))
  return screen.getByLabelText('Name')
}

describe('EditableCell', () => {
  it('shows the value as text, with a dash when it is empty', () => {
    renderCell({ value: '', emptyLabel: '—' })

    expect(screen.getByText('—')).toBeInTheDocument()
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
  })

  it('turns into an input holding the value on double-click', async () => {
    const user = userEvent.setup()
    renderCell()

    const input = await startEditing(user)

    expect(input).toHaveValue('Alex')
    expect(input).toHaveFocus()
  })

  it('opens with Enter or F2 when the cell has the keyboard focus', async () => {
    const user = userEvent.setup()
    renderCell()

    await user.tab()
    await user.keyboard('{Enter}')
    expect(screen.getByRole('textbox', { name: 'Name' })).toBeInTheDocument()

    await user.keyboard('{Escape}')
    await waitFor(() => expect(screen.queryByRole('textbox')).not.toBeInTheDocument())
    await user.keyboard('{F2}')
    expect(screen.getByRole('textbox', { name: 'Name' })).toBeInTheDocument()
  })

  it('saves the trimmed new text on Enter, closes the editor and gives the focus back to the cell', async () => {
    const user = userEvent.setup()
    const { onSave } = renderCell()

    const input = await startEditing(user)
    await user.clear(input)
    await user.type(input, '  Sam  {Enter}')

    expect(onSave).toHaveBeenCalledTimes(1)
    expect(onSave).toHaveBeenCalledWith('Sam')
    await waitFor(() => expect(screen.queryByRole('textbox')).not.toBeInTheDocument())
    expect(screen.getByRole('cell')).toHaveFocus()
  })

  it('saves when the field loses the focus, and only once', async () => {
    const user = userEvent.setup()
    const { onSave } = renderCell()

    const input = await startEditing(user)
    await user.clear(input)
    await user.type(input, 'Sam')
    await user.tab()

    expect(onSave).toHaveBeenCalledTimes(1)
    expect(onSave).toHaveBeenCalledWith('Sam')
  })

  it('does not call onSave when the text did not change', async () => {
    const user = userEvent.setup()
    const { onSave } = renderCell()

    const input = await startEditing(user)
    await user.type(input, '{Enter}')

    expect(onSave).not.toHaveBeenCalled()
    await waitFor(() => expect(screen.queryByRole('textbox')).not.toBeInTheDocument())
  })

  it('discards the edit on Escape', async () => {
    const user = userEvent.setup()
    const { onSave } = renderCell()

    const input = await startEditing(user)
    await user.clear(input)
    await user.type(input, 'Sam{Escape}')

    expect(onSave).not.toHaveBeenCalled()
    await waitFor(() => expect(screen.queryByRole('textbox')).not.toBeInTheDocument())
    expect(screen.getByText('Alex')).toBeInTheDocument()
  })

  it('keeps the editor open on Enter when a required field is empty, and drops the edit on blur', async () => {
    const user = userEvent.setup()
    const { onSave } = renderCell({ required: true })

    const input = await startEditing(user)
    await user.clear(input)
    await user.type(input, '   {Enter}')

    expect(onSave).not.toHaveBeenCalled()
    expect(screen.getByRole('textbox', { name: 'Name' })).toBeInTheDocument()

    await user.tab()

    expect(onSave).not.toHaveBeenCalled()
    await waitFor(() => expect(screen.queryByRole('textbox')).not.toBeInTheDocument())
    expect(screen.getByText('Alex')).toBeInTheDocument()
  })

  it('allows an empty value when the field is not required', async () => {
    const user = userEvent.setup()
    const { onSave } = renderCell({ value: '7', type: 'number' })

    const input = await startEditing(user, '7')
    await user.clear(input)
    await user.type(input, '{Enter}')

    expect(onSave).toHaveBeenCalledWith('')
  })

  it('does not save a number below the minimum', async () => {
    const user = userEvent.setup()
    const { onSave } = renderCell({ value: '100', type: 'number', min: 0 })

    const input = await startEditing(user, '100')
    await user.clear(input)
    await user.type(input, '-5{Enter}')

    expect(onSave).not.toHaveBeenCalled()
    expect(screen.getByRole('spinbutton', { name: 'Name' })).toBeInTheDocument()
  })

  it('discards the edit and keeps the old value when onSave rejects', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn().mockRejectedValue(new Error('403'))
    renderCell({ onSave })

    const input = await startEditing(user)
    await user.clear(input)
    await user.type(input, 'Sam{Enter}')

    expect(onSave).toHaveBeenCalledWith('Sam')
    await waitFor(() => expect(screen.queryByRole('textbox')).not.toBeInTheDocument())
    expect(screen.getByText('Alex')).toBeInTheDocument()
  })

  it('is read-only while saving and does not save twice', async () => {
    const user = userEvent.setup()
    let finish: () => void = () => {}
    const onSave = vi.fn().mockReturnValue(new Promise<void>((resolve) => { finish = resolve }))
    renderCell({ onSave })

    const input = await startEditing(user)
    await user.clear(input)
    await user.type(input, 'Sam{Enter}')

    expect(input).toHaveAttribute('readonly')
    await user.keyboard('{Enter}')
    await user.tab()
    expect(onSave).toHaveBeenCalledTimes(1)

    finish()
    await waitFor(() => expect(screen.queryByRole('textbox')).not.toBeInTheDocument())
  })
})
