import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Person } from '../api/types'
import { API_BASE_URL } from '../api/config'
import { PersonList } from './PersonList'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

const logout = vi.fn()

function renderWithProviders(ui: React.ReactElement){
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false}}
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  )
}

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({ token: 'test-token', logout }),
}))

const alex: Person = { id: 1, name: 'Alex Smith', dateOfBirth: '1990-01-01', managerId: null, salary: 50000 }
const sam: Person = { id: 2, name: 'Sam Lee', dateOfBirth: '1985-05-05', managerId: 1, salary: 60000 }

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } })
}

// GET returns the list; the PUT response is chosen by each test.
function stubApi(putResponse: (body: Person) => Response = (body) => jsonResponse(body)) {
  const fetchMock = vi.fn((_url: string, init?: RequestInit) =>
    Promise.resolve(
      init?.method === 'PUT' ? putResponse(JSON.parse(init.body as string)) : jsonResponse([alex, sam]),
    ),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function putCalls(fetchMock: ReturnType<typeof stubApi>) {
  return fetchMock.mock.calls.filter(([, init]) => init?.method === 'PUT')
}

async function renderList() {
  renderWithProviders(<PersonList />)
  await screen.findByText('Alex Smith')
}

type User = ReturnType<typeof userEvent.setup>

// Replace the text of a field in the row that is being edited.
async function fill(user: User, label: string, text: string) {
  const input = screen.getByLabelText(label)
  await user.clear(input)
  if (text !== '') await user.type(input, text)
}

beforeEach(() => {
  logout.mockClear()
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('PersonList: row edit mode', () => {
  it('starts editing the selected row with the Edit button and fills the inputs', async () => {
    const user = userEvent.setup()
    stubApi()
    await renderList()

    await user.click(screen.getByRole('button', { name: 'Edit' })) // the first row is selected by default

    expect(screen.getByLabelText('Name')).toHaveValue('Alex Smith')
    expect(screen.getByLabelText('Date of birth')).toHaveValue('1990-01-01')
    expect(screen.getByLabelText('Manager Id')).toHaveValue(null)
    expect(screen.getByLabelText('Salary')).toHaveValue(50000)
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument()
    expect(screen.getByText('Sam Lee')).toBeInTheDocument() // the other rows stay plain text
  })

  it('edits the row that was clicked, not just the first one', async () => {
    const user = userEvent.setup()
    stubApi()
    await renderList()

    await user.click(screen.getByText('Sam Lee'))
    await user.click(screen.getByRole('button', { name: 'Edit' }))

    expect(screen.getByLabelText('Name')).toHaveValue('Sam Lee')
    expect(screen.getByLabelText('Manager Id')).toHaveValue(1)
  })

  it('starts editing a row on double-click', async () => {
    const user = userEvent.setup()
    stubApi()
    await renderList()

    await user.dblClick(screen.getByText('Sam Lee'))

    expect(screen.getByLabelText('Name')).toHaveValue('Sam Lee')
  })

  it('keeps the Id read-only', async () => {
    const user = userEvent.setup()
    stubApi()
    await renderList()

    await user.click(screen.getByRole('button', { name: 'Edit' }))

    expect(screen.getAllByRole('textbox')).toHaveLength(1) // only Name is a text field
    expect(screen.getAllByRole('spinbutton')).toHaveLength(2) // Manager Id and Salary
    expect(within(screen.getByRole('row', { name: /Save/ })).getAllByRole('cell')[0]).toHaveTextContent('1')
  })

  it('saves every changed field with one PUT, then shows the saved row without reloading', async () => {
    const user = userEvent.setup()
    const fetchMock = stubApi()
    await renderList()
    const requestsBefore = fetchMock.mock.calls.length

    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await fill(user, 'Name', '  Alexander Smith  ')
    await fill(user, 'Date of birth', '1991-02-03')
    await fill(user, 'Manager Id', '2')
    await fill(user, 'Salary', '55000')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Alexander Smith')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument()
    expect(screen.getByText('1991-02-03')).toBeInTheDocument()
    expect(screen.getByText('55000')).toBeInTheDocument()

    expect(putCalls(fetchMock)).toHaveLength(1)
    const [[url, init]] = putCalls(fetchMock)
    expect(url).toBe(`${API_BASE_URL}/V2/Person`)
    expect(JSON.parse(init!.body as string)).toEqual({
      id: 1,
      name: 'Alexander Smith',
      dateOfBirth: '1991-02-03',
      managerId: 2,
      salary: 55000,
    })
    expect(init!.headers).toEqual({ 'Content-Type': 'application/json', Authorization: 'Bearer test-token' })
    expect(fetchMock.mock.calls.length).toBe(requestsBefore + 1) // no list reload
  })

  it('saves with Enter and sends an empty Manager Id as null', async () => {
    const user = userEvent.setup()
    const fetchMock = stubApi()
    await renderList()

    await user.click(screen.getByText('Sam Lee'))
    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await fill(user, 'Manager Id', '')
    await user.type(screen.getByLabelText('Salary'), '{Enter}')

    await waitFor(() => expect(putCalls(fetchMock)).toHaveLength(1))
    expect(JSON.parse(putCalls(fetchMock)[0][1]!.body as string)).toMatchObject({ id: 2, managerId: null })
  })

  it('only changes the edited row', async () => {
    const user = userEvent.setup()
    stubApi()
    await renderList()

    await user.click(screen.getByText('Sam Lee'))
    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await fill(user, 'Name', 'Samuel Lee')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Samuel Lee')).toBeInTheDocument()
    expect(screen.getByText('Alex Smith')).toBeInTheDocument()
  })

  it('leaves edit mode without a request when nothing changed', async () => {
    const user = userEvent.setup()
    const fetchMock = stubApi()
    await renderList()

    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument()
    expect(putCalls(fetchMock)).toHaveLength(0)
  })

  it('discards the changes on Cancel', async () => {
    const user = userEvent.setup()
    const fetchMock = stubApi()
    await renderList()

    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await fill(user, 'Name', 'Nobody')
    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(screen.getByText('Alex Smith')).toBeInTheDocument()
    expect(screen.queryByText('Nobody')).not.toBeInTheDocument()
    expect(putCalls(fetchMock)).toHaveLength(0)
  })

  it('discards the changes on Escape', async () => {
    const user = userEvent.setup()
    const fetchMock = stubApi()
    await renderList()

    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await fill(user, 'Name', 'Nobody')
    await user.keyboard('{Escape}')

    expect(screen.getByText('Alex Smith')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument()
    expect(putCalls(fetchMock)).toHaveLength(0)
  })

  it('does not send a request while a required field is empty or the salary is negative', async () => {
    const user = userEvent.setup()
    const fetchMock = stubApi()
    await renderList()

    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await fill(user, 'Name', '')
    await user.click(screen.getByRole('button', { name: 'Save' }))
    expect(putCalls(fetchMock)).toHaveLength(0)
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()

    await fill(user, 'Name', 'Alexander')
    await fill(user, 'Salary', '-1')
    await user.click(screen.getByRole('button', { name: 'Save' }))
    expect(putCalls(fetchMock)).toHaveLength(0)
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('disables the rest of the toolbar while a row is being edited', async () => {
    const user = userEvent.setup()
    stubApi()
    await renderList()

    await user.click(screen.getByRole('button', { name: 'Edit' }))

    for (const name of ['Add', 'Edit', 'Delete', 'Previous', 'Next', /Refresh/]) {
      expect(screen.getByRole('button', { name })).toBeDisabled()
    }
  })

  it('does not start a second edit while one row is open', async () => {
    const user = userEvent.setup()
    stubApi()
    await renderList()

    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await user.dblClick(screen.getByText('Sam Lee'))

    expect(screen.getAllByRole('button', { name: 'Save' })).toHaveLength(1)
    expect(screen.getByLabelText('Name')).toHaveValue('Alex Smith')
  })

  describe('when the save fails', () => {
    it('explains a 403, keeps the row open with what was typed, and lets the user cancel', async () => {
      const user = userEvent.setup()
      stubApi(() => new Response('', { status: 403 }))
      await renderList()

      await user.click(screen.getByRole('button', { name: 'Edit' }))
      await fill(user, 'Name', 'Nope')
      await user.click(screen.getByRole('button', { name: 'Save' }))

      const dialog = await screen.findByRole('dialog')
      expect(within(dialog).getByRole('alert')).toHaveTextContent("You don't have permission to update this person.")
      await user.click(within(dialog).getByRole('button', { name: 'Ok' }))
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())

      expect(screen.getByLabelText('Name')).toHaveValue('Nope') // still editing, nothing lost
      expect(screen.getByRole('button', { name: 'Save' })).toBeEnabled()

      await user.click(screen.getByRole('button', { name: 'Cancel' }))
      expect(screen.getByText('Alex Smith')).toBeInTheDocument()
    })

    it('says the person no longer exists on a 404', async () => {
      const user = userEvent.setup()
      stubApi(() => new Response('', { status: 404 }))
      await renderList()

      await user.click(screen.getByRole('button', { name: 'Edit' }))
      await fill(user, 'Name', 'Ghost')
      await user.click(screen.getByRole('button', { name: 'Save' }))

      expect(await screen.findByRole('alert')).toHaveTextContent('This person no longer exists.')
    })

    it('logs out on a 401 instead of showing an error dialog', async () => {
      const user = userEvent.setup()
      stubApi(() => new Response('', { status: 401 }))
      await renderList()

      await user.click(screen.getByRole('button', { name: 'Edit' }))
      await fill(user, 'Name', 'Anyone')
      await user.click(screen.getByRole('button', { name: 'Save' }))

      await waitFor(() => expect(logout).toHaveBeenCalledTimes(1))
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })
})

describe('PersonList: busy indicator', () => {
  // GETs that stay pending until the test answers them, in order. The first N can answer at once.
  function controlledList(immediate = 0) {
    const pending: Array<(response: Response) => void> = []
    let calls = 0
    const fetchMock = vi.fn((_url: string, _init?: RequestInit) => {
      calls += 1
      if (calls <= immediate) return Promise.resolve(jsonResponse([alex, sam]))
      return new Promise<Response>((resolve) => pending.push(resolve))
    })
    vi.stubGlobal('fetch', fetchMock)
    return { answer: (list: Person[]) => pending.shift()!(jsonResponse(list)) }
  }

  function renderPlain() {
    renderWithProviders(<PersonList />)
  }

  it('keeps the table on screen and shows a spinner in it when the first load is slow', async () => {
    const { answer } = controlledList()
    renderPlain()

    const table = await screen.findByRole('table')
    expect(table).toHaveAttribute('aria-busy', 'true')
    expect(within(table).getByRole('columnheader', { name: 'Name' })).toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument() // not yet: it waits 300 ms

    const spinner = await screen.findByRole('status')
    expect(spinner).toHaveTextContent('Loading people…')
    expect(table).toContainElement(spinner)
    expect(screen.getByRole('button', { name: /Refresh/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Refresh/ }).querySelector('svg')).toHaveClass('spin')

    answer([alex, sam])

    expect(await screen.findByText('Alex Smith')).toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
    expect(table).toHaveAttribute('aria-busy', 'false')
    expect(screen.getByRole('button', { name: /Refresh/ })).toBeEnabled()
  })

  it('never shows the spinner when the load is fast', async () => {
    stubApi()
    await renderList()

    await new Promise((resolve) => setTimeout(resolve, 400)) // longer than the 300 ms delay

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('keeps the current rows while refreshing and shows the spinner by the pagination', async () => {
    const { answer } = controlledList(1) // the first load is instant, the refresh is slow
    renderPlain()
    await screen.findByText('Alex Smith')

    await userEvent.setup().click(screen.getByRole('button', { name: /Refresh/ }))

    expect(screen.getByText('Alex Smith')).toBeInTheDocument() // the old rows stay
    const spinner = await screen.findByRole('status')
    expect(within(screen.getByRole('table')).queryByRole('status')).not.toBeInTheDocument()
    expect(within(screen.getByRole('navigation', { name: 'People pages' })).getByRole('status')).toBe(spinner)

    answer([alex, { ...sam, name: 'Samuel Lee' }])

    expect(await screen.findByText('Samuel Lee')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('status')).not.toBeInTheDocument())
  })
})
