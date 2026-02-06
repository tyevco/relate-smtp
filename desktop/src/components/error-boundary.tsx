import { Component, ReactNode } from 'react'
import { AlertTriangle } from 'lucide-react'

interface Props {
  children: ReactNode
  fallback?: ReactNode
}

interface State {
  hasError: boolean
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  componentDidCatch(error: Error) {
    console.error('ErrorBoundary caught:', error)
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback ?? <ErrorFallback />
    }
    return this.props.children
  }
}

function ErrorFallback() {
  return (
    <div className="flex flex-col items-center justify-center p-8 text-muted-foreground">
      <AlertTriangle className="h-8 w-8 mb-2" />
      <p>Something went wrong</p>
    </div>
  )
}
