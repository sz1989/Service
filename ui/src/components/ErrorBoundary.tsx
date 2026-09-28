import { Component, type ErrorInfo, type ReactNode } from 'react'

interface ErrorBoundaryProps {
  children: ReactNode
  fallback?: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null, hasError: false }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error, hasError: true }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo)
  }

  render() {
    if (this.state.error) {
      return (
        this.props.fallback ?? (
          <p className="error">Something went wrong. Please refresh the page.</p>
        )
      )
    }

    return this.props.children
  }
}
