import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { Input } from './input'

describe('Input', () => {
  it('renders correctly', () => {
    render(<Input placeholder="Enter text" />)
    expect(screen.getByPlaceholderText('Enter text')).toBeInTheDocument()
  })

  it('accepts user input', () => {
    render(<Input placeholder="Type here" />)
    const input = screen.getByPlaceholderText('Type here')

    fireEvent.change(input, { target: { value: 'Hello World' } })
    expect(input).toHaveValue('Hello World')
  })

  it('handles onChange events', () => {
    const handleChange = vi.fn()
    render(<Input onChange={handleChange} />)
    const input = screen.getByRole('textbox')

    fireEvent.change(input, { target: { value: 'test' } })
    expect(handleChange).toHaveBeenCalledTimes(1)
  })

  it('can be disabled', () => {
    render(<Input disabled placeholder="Disabled" />)
    const input = screen.getByPlaceholderText('Disabled')
    expect(input).toBeDisabled()
  })

  it('renders with correct type', () => {
    render(<Input type="email" data-testid="email-input" />)
    const input = screen.getByTestId('email-input')
    expect(input).toHaveAttribute('type', 'email')
  })

  it('renders as password input', () => {
    render(<Input type="password" data-testid="password-input" />)
    const input = screen.getByTestId('password-input')
    expect(input).toHaveAttribute('type', 'password')
  })

  it('applies default styles', () => {
    render(<Input data-testid="styled-input" />)
    const input = screen.getByTestId('styled-input')
    expect(input).toHaveClass('flex')
    expect(input).toHaveClass('h-9')
    expect(input).toHaveClass('w-full')
    expect(input).toHaveClass('rounded-md')
    expect(input).toHaveClass('border')
    expect(input).toHaveClass('border-input')
  })

  it('merges custom className', () => {
    render(<Input className="custom-input" data-testid="custom-input" />)
    const input = screen.getByTestId('custom-input')
    expect(input).toHaveClass('custom-input')
  })

  it('forwards ref correctly', () => {
    const ref = { current: null as HTMLInputElement | null }
    render(<Input ref={ref} />)
    expect(ref.current).toBeInstanceOf(HTMLInputElement)
  })

  it('passes through HTML input props', () => {
    render(
      <Input
        id="test-input"
        name="testField"
        required
        minLength={5}
        maxLength={100}
        aria-label="Test input"
        data-testid="full-props"
      />
    )
    const input = screen.getByTestId('full-props')
    expect(input).toHaveAttribute('id', 'test-input')
    expect(input).toHaveAttribute('name', 'testField')
    expect(input).toHaveAttribute('required')
    expect(input).toHaveAttribute('minLength', '5')
    expect(input).toHaveAttribute('maxLength', '100')
    expect(input).toHaveAttribute('aria-label', 'Test input')
  })

  it('supports controlled value', () => {
    const { rerender } = render(<Input value="initial" onChange={() => {}} />)
    const input = screen.getByRole('textbox')
    expect(input).toHaveValue('initial')

    rerender(<Input value="updated" onChange={() => {}} />)
    expect(input).toHaveValue('updated')
  })

  it('handles focus and blur events', () => {
    const onFocus = vi.fn()
    const onBlur = vi.fn()
    render(<Input onFocus={onFocus} onBlur={onBlur} />)
    const input = screen.getByRole('textbox')

    fireEvent.focus(input)
    expect(onFocus).toHaveBeenCalledTimes(1)

    fireEvent.blur(input)
    expect(onBlur).toHaveBeenCalledTimes(1)
  })
})
