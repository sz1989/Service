import { useNavigate } from 'react-router-dom'
import { ChatPanel } from '../components/ChatPanel'
import { PersonList } from '../components/PersonList'
import { useAuth } from '../auth/AuthContext'

export function DashboardPage() {
  const { logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="dashboard-page">
      <header className="dashboard-header">
        <div>
          <h1>Dashboard</h1>
          <p className="dashboard-subtitle">Overview and tools</p>
        </div>
        <button type="button" className="button-secondary" onClick={handleLogout}>
          Log out
        </button>
      </header>

      <ChatPanel />

      <section className="dashboard-card">
        <h2>Persons</h2>
        <PersonList />
      </section>
    </div>
  )
}
