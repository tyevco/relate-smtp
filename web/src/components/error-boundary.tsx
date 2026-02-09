import { Component, type ReactNode } from 'react'
import { AlertTriangle } from 'lucide-react'

interface Props {
  children: ReactNode
  fallback?: ReactNode
}

interface State {
  hasError: boolean
  error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false, error: null }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error) {
    console.error('ErrorBoundary caught:', error)
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback ?? <ErrorFallback error={this.state.error} />
    }
    return this.props.children
  }
}

interface ErrorFallbackProps {
  error: Error | null
}

function ErrorFallback({ error }: ErrorFallbackProps) {
  return (
    <div className="flex flex-col items-center justify-center p-8 text-muted-foreground">
      <AlertTriangle className="h-8 w-8 mb-2" />
      <p>Something went wrong</p>
      {error?.message && (
        <p className="mt-2 text-sm text-center max-w-md">{error.message}</p>
      )}
    </div>
  )
}
