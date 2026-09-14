import { useNavigate } from 'react-router-dom'
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
        <h1>Dashboard</h1>
        <button type="button" onClick={handleLogout}>
          Log out
        </button>
      </header>

      <section>
        <h2>Persons</h2>
        <PersonList />
      </section>
    </div>
  )
}
