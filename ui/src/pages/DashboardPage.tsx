import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChatPanel } from '../components/ChatPanel'
import { PersonList } from '../components/PersonList'
import { RagSearchPanel } from '../components/RagSearchPanel'
import { useAuth } from '../auth/AuthContext'

type DashboardTab = 'overview' | 'rag-search'

export function DashboardPage() {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [activeTab, setActiveTab] = useState<DashboardTab>('overview')

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

      <nav className="dashboard-tabs" role="tablist">
        <button
          type="button"
          role="tab"
          aria-selected={activeTab === 'overview'}
          className={activeTab === 'overview' ? 'active' : ''}
          onClick={() => setActiveTab('overview')}
        >
          Overview
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={activeTab === 'rag-search'}
          className={activeTab === 'rag-search' ? 'active' : ''}
          onClick={() => setActiveTab('rag-search')}
        >
          RAG Search
        </button>
      </nav>

      {activeTab === 'overview' && (
        <>
          <ChatPanel />

          <section className="dashboard-card">
            <h2>Persons</h2>
            <PersonList />
          </section>
        </>
      )}

      {activeTab === 'rag-search' && <RagSearchPanel />}
    </div>
  )
}
