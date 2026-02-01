import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Label } from './label'

describe('Label', () => {
  it('renders children correctly', () => {
    render(<Label>Email</Label>)
    expect(screen.getByText('Email')).toBeInTheDocument()
  })

  it('renders as a label element', () => {
    render(<Label>Name</Label>)
    const label = screen.getByText('Name')
    expect(label.tagName).toBe('LABEL')
  })

  it('applies default styles', () => {
    render(<Label data-testid="label">Field</Label>)
    const label = screen.getByTestId('label')
    expect(label).toHaveClass('text-sm')
    expect(label).toHaveClass('font-medium')
    expect(label).toHaveClass('leading-none')
  })

  it('applies peer-disabled styles', () => {
    render(<Label data-testid="label">Field</Label>)
    const label = screen.getByTestId('label')
    expect(label).toHaveClass('peer-disabled:cursor-not-allowed')
    expect(label).toHaveClass('peer-disabled:opacity-70')
  })

  it('merges custom className', () => {
    render(
      <Label className="custom-label" data-testid="label">
        Custom
      </Label>
    )
    expect(screen.getByTestId('label')).toHaveClass('custom-label')
  })

  it('associates with input via htmlFor', () => {
    render(
      <>
        <Label htmlFor="email-input">Email</Label>
        <input id="email-input" type="email" />
      </>
    )
    const label = screen.getByText('Email')
    expect(label).toHaveAttribute('for', 'email-input')
  })

  it('forwards ref correctly', () => {
    const ref = { current: null as HTMLLabelElement | null }
    render(<Label ref={ref}>Ref Label</Label>)
    expect(ref.current).toBeInstanceOf(HTMLLabelElement)
  })

  it('passes through HTML label props', () => {
    render(
      <Label data-testid="label" id="my-label" aria-describedby="help-text">
        Field
      </Label>
    )
    const label = screen.getByTestId('label')
    expect(label).toHaveAttribute('id', 'my-label')
    expect(label).toHaveAttribute('aria-describedby', 'help-text')
  })

  it('renders with nested elements', () => {
    render(
      <Label>
        <span>Required</span>
        <span className="text-red-500">*</span>
      </Label>
    )
    expect(screen.getByText('Required')).toBeInTheDocument()
    expect(screen.getByText('*')).toBeInTheDocument()
  })
})
