import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Badge } from './badge'

describe('Badge', () => {
  it('renders children correctly', () => {
    render(<Badge>New</Badge>)
    expect(screen.getByText('New')).toBeInTheDocument()
  })

  it('applies default variant styles', () => {
    render(<Badge data-testid="badge">Default</Badge>)
    const badge = screen.getByTestId('badge')
    expect(badge).toHaveClass('bg-primary')
    expect(badge).toHaveClass('text-primary-foreground')
  })

  it('applies secondary variant styles', () => {
    render(
      <Badge variant="secondary" data-testid="badge">
        Secondary
      </Badge>
    )
    const badge = screen.getByTestId('badge')
    expect(badge).toHaveClass('bg-secondary')
    expect(badge).toHaveClass('text-secondary-foreground')
  })

  it('applies destructive variant styles', () => {
    render(
      <Badge variant="destructive" data-testid="badge">
        Destructive
      </Badge>
    )
    const badge = screen.getByTestId('badge')
    expect(badge).toHaveClass('bg-destructive')
    expect(badge).toHaveClass('text-destructive-foreground')
  })

  it('applies outline variant styles', () => {
    render(
      <Badge variant="outline" data-testid="badge">
        Outline
      </Badge>
    )
    const badge = screen.getByTestId('badge')
    expect(badge).toHaveClass('text-foreground')
  })

  it('applies base styles regardless of variant', () => {
    render(<Badge data-testid="badge">Test</Badge>)
    const badge = screen.getByTestId('badge')
    expect(badge).toHaveClass('inline-flex')
    expect(badge).toHaveClass('items-center')
    expect(badge).toHaveClass('rounded-md')
    expect(badge).toHaveClass('border')
    expect(badge).toHaveClass('px-2.5')
    expect(badge).toHaveClass('py-0.5')
    expect(badge).toHaveClass('text-xs')
    expect(badge).toHaveClass('font-semibold')
  })

  it('merges custom className', () => {
    render(
      <Badge className="custom-badge" data-testid="badge">
        Custom
      </Badge>
    )
    expect(screen.getByTestId('badge')).toHaveClass('custom-badge')
  })

  it('renders with multiple children', () => {
    render(
      <Badge>
        <span>Icon</span>
        <span>Label</span>
      </Badge>
    )
    expect(screen.getByText('Icon')).toBeInTheDocument()
    expect(screen.getByText('Label')).toBeInTheDocument()
  })

  it('passes through HTML div props', () => {
    render(
      <Badge data-testid="badge" role="status" aria-label="Status badge">
        Active
      </Badge>
    )
    const badge = screen.getByTestId('badge')
    expect(badge).toHaveAttribute('role', 'status')
    expect(badge).toHaveAttribute('aria-label', 'Status badge')
  })
})
