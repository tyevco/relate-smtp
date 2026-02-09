import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { Switch } from './switch'

describe('Switch', () => {
  it('renders correctly', () => {
    render(<Switch />)
    expect(screen.getByRole('switch')).toBeInTheDocument()
  })

  it('is unchecked by default', () => {
    render(<Switch />)
    expect(screen.getByRole('switch')).toHaveAttribute('aria-checked', 'false')
  })

  it('can be checked initially', () => {
    render(<Switch checked />)
    expect(screen.getByRole('switch')).toHaveAttribute('aria-checked', 'true')
  })

  it('calls onCheckedChange when clicked', () => {
    const handleChange = vi.fn()
    render(<Switch onCheckedChange={handleChange} />)

    fireEvent.click(screen.getByRole('switch'))
    expect(handleChange).toHaveBeenCalledTimes(1)
    expect(handleChange).toHaveBeenCalledWith(true)
  })

  it('toggles from checked to unchecked', () => {
    const handleChange = vi.fn()
    render(<Switch checked onCheckedChange={handleChange} />)

    fireEvent.click(screen.getByRole('switch'))
    expect(handleChange).toHaveBeenCalledWith(false)
  })

  it('can be disabled', () => {
    const handleChange = vi.fn()
    render(<Switch disabled onCheckedChange={handleChange} />)

    const switchElement = screen.getByRole('switch')
    expect(switchElement).toBeDisabled()

    fireEvent.click(switchElement)
    expect(handleChange).not.toHaveBeenCalled()
  })

  it('applies checked styles when checked', () => {
    render(<Switch checked />)
    const switchElement = screen.getByRole('switch')
    expect(switchElement).toHaveClass('bg-primary')
  })

  it('applies unchecked styles when not checked', () => {
    render(<Switch />)
    const switchElement = screen.getByRole('switch')
    expect(switchElement).toHaveClass('bg-input')
  })

  it('applies disabled styles when disabled', () => {
    render(<Switch disabled />)
    const switchElement = screen.getByRole('switch')
    expect(switchElement).toHaveClass('disabled:cursor-not-allowed')
    expect(switchElement).toHaveClass('disabled:opacity-50')
  })

  it('has type="button" to prevent form submission', () => {
    render(<Switch />)
    expect(screen.getByRole('switch')).toHaveAttribute('type', 'button')
  })

  it('merges custom className', () => {
    render(<Switch className="custom-switch" />)
    expect(screen.getByRole('switch')).toHaveClass('custom-switch')
  })

  it('forwards ref correctly', () => {
    const ref = { current: null as HTMLButtonElement | null }
    render(<Switch ref={ref} />)
    expect(ref.current).toBeInstanceOf(HTMLButtonElement)
  })

  it('contains a thumb element for visual toggle indicator', () => {
    render(<Switch />)
    const switchElement = screen.getByRole('switch')
    const thumb = switchElement.querySelector('span')
    expect(thumb).toBeInTheDocument()
    expect(thumb).toHaveClass('rounded-full')
    expect(thumb).toHaveClass('bg-background')
  })

  it('applies translation to thumb when checked', () => {
    render(<Switch checked />)
    const switchElement = screen.getByRole('switch')
    const thumb = switchElement.querySelector('span')
    expect(thumb).toHaveClass('translate-x-5')
  })

  it('applies no translation to thumb when unchecked', () => {
    render(<Switch />)
    const switchElement = screen.getByRole('switch')
    const thumb = switchElement.querySelector('span')
    expect(thumb).toHaveClass('translate-x-0')
  })
})
