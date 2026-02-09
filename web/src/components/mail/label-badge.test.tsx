import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { LabelBadge } from './label-badge'

describe('LabelBadge', () => {
  it('renders label name correctly', () => {
    render(<LabelBadge name="Work" color="#3b82f6" />)
    expect(screen.getByText('Work')).toBeInTheDocument()
  })

  it('applies custom color to styles', () => {
    render(<LabelBadge name="Important" color="#ef4444" />)
    const badge = screen.getByText('Important').closest('div')
    expect(badge).not.toBeNull()

    expect(badge).toHaveStyle({
      backgroundColor: '#ef444420',
      borderColor: '#ef4444',
      color: '#ef4444',
    })
  })

  it('does not show remove button when onRemove is not provided', () => {
    render(<LabelBadge name="Static" color="#22c55e" />)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('shows remove button when onRemove is provided', () => {
    render(<LabelBadge name="Removable" color="#22c55e" onRemove={() => {}} />)
    expect(screen.getByRole('button')).toBeInTheDocument()
  })

  it('calls onRemove when remove button is clicked', () => {
    const onRemove = vi.fn()
    render(<LabelBadge name="Removable" color="#22c55e" onRemove={onRemove} />)

    fireEvent.click(screen.getByRole('button'))
    expect(onRemove).toHaveBeenCalledTimes(1)
  })

  it('stops event propagation when remove button is clicked', () => {
    const onRemove = vi.fn()
    const onClick = vi.fn()

    render(
      <div onClick={onClick}>
        <LabelBadge name="Test" color="#000" onRemove={onRemove} />
      </div>
    )

    fireEvent.click(screen.getByRole('button'))
    expect(onRemove).toHaveBeenCalled()
    expect(onClick).not.toHaveBeenCalled()
  })

  it('applies outline variant from Badge', () => {
    render(<LabelBadge name="Outline" color="#000" />)
    // The badge should have the outline variant class
    const badge = screen.getByText('Outline').closest('div')
    expect(badge).not.toBeNull()
    expect(badge).toHaveClass('text-foreground')
  })

  it('merges custom className', () => {
    render(
      <LabelBadge
        name="Custom"
        color="#000"
        className="my-custom-class"
      />
    )
    const badge = screen.getByText('Custom').closest('div')
    expect(badge).not.toBeNull()
    expect(badge).toHaveClass('my-custom-class')
  })

  it('applies base styles', () => {
    render(<LabelBadge name="Styled" color="#000" />)
    const badge = screen.getByText('Styled').closest('div')
    expect(badge).not.toBeNull()
    expect(badge).toHaveClass('px-2')
    expect(badge).toHaveClass('py-0.5')
    expect(badge).toHaveClass('text-xs')
    expect(badge).toHaveClass('font-medium')
  })

  it('renders with different colors correctly', () => {
    const { rerender } = render(<LabelBadge name="Blue" color="#3b82f6" />)
    let badge = screen.getByText('Blue').closest('div')
    expect(badge).not.toBeNull()
    expect(badge).toHaveStyle({ borderColor: '#3b82f6' })

    rerender(<LabelBadge name="Green" color="#22c55e" />)
    badge = screen.getByText('Green').closest('div')
    expect(badge).not.toBeNull()
    expect(badge).toHaveStyle({ borderColor: '#22c55e' })

    rerender(<LabelBadge name="Red" color="#ef4444" />)
    badge = screen.getByText('Red').closest('div')
    expect(badge).not.toBeNull()
    expect(badge).toHaveStyle({ borderColor: '#ef4444' })
  })
})
