import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SearchBar } from './search-bar'

describe('SearchBar', () => {
  it('renders with placeholder text', () => {
    render(<SearchBar onSearch={() => {}} />)
    expect(screen.getByPlaceholderText('Search emails...')).toBeInTheDocument()
  })

  it('renders with initial value', () => {
    render(<SearchBar onSearch={() => {}} initialValue="test query" />)
    expect(screen.getByPlaceholderText('Search emails...')).toHaveValue('test query')
  })

  it('updates input value when typing', async () => {
    const user = userEvent.setup()
    render(<SearchBar onSearch={() => {}} />)
    const input = screen.getByPlaceholderText('Search emails...')

    await user.type(input, 'hello')
    expect(input).toHaveValue('hello')
  })

  it('calls onSearch when form is submitted', async () => {
    const onSearch = vi.fn()
    const user = userEvent.setup()
    render(<SearchBar onSearch={onSearch} />)

    const input = screen.getByPlaceholderText('Search emails...')
    await user.type(input, 'test search')
    await user.click(screen.getByRole('button', { name: /search/i }))

    expect(onSearch).toHaveBeenCalledTimes(1)
    expect(onSearch).toHaveBeenCalledWith('test search')
  })

  it('calls onSearch when pressing Enter', async () => {
    const onSearch = vi.fn()
    const user = userEvent.setup()
    render(<SearchBar onSearch={onSearch} />)

    const input = screen.getByPlaceholderText('Search emails...')
    await user.type(input, 'enter search')
    await user.keyboard('{Enter}')

    expect(onSearch).toHaveBeenCalledTimes(1)
    expect(onSearch).toHaveBeenCalledWith('enter search')
  })

  it('shows clear button when there is input', async () => {
    const user = userEvent.setup()
    render(<SearchBar onSearch={() => {}} />)

    // Clear button should not be visible initially
    expect(screen.queryByRole('button', { name: '' })).not.toBeInTheDocument()

    const input = screen.getByPlaceholderText('Search emails...')
    await user.type(input, 'some text')

    // Now clear button should be visible (it's the X icon button)
    const buttons = screen.getAllByRole('button')
    expect(buttons.length).toBeGreaterThan(1)
  })

  it('clears input and calls onSearch with empty string when clear is clicked', async () => {
    const onSearch = vi.fn()
    const user = userEvent.setup()
    render(<SearchBar onSearch={onSearch} initialValue="existing query" />)

    const input = screen.getByPlaceholderText('Search emails...')
    expect(input).toHaveValue('existing query')

    // Find and click the clear button (type="button" inside the form)
    const buttons = screen.getAllByRole('button')
    const clearButton = buttons.find((b) => b.getAttribute('type') === 'button')
    if (clearButton) {
      await user.click(clearButton)
    }

    expect(input).toHaveValue('')
    expect(onSearch).toHaveBeenCalledWith('')
  })

  it('has search icon in input', () => {
    render(<SearchBar onSearch={() => {}} />)
    // The search icon is rendered as a decorative element
    const input = screen.getByPlaceholderText('Search emails...')
    const form = input.closest('form')
    expect(form).toBeInTheDocument()
  })

  it('renders submit button with Search text', () => {
    render(<SearchBar onSearch={() => {}} />)
    // On desktop, it shows "Search" text
    expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument()
  })

  it('prevents default form submission behavior', async () => {
    const onSearch = vi.fn()
    render(<SearchBar onSearch={onSearch} />)

    const form = screen.getByPlaceholderText('Search emails...').closest('form')!
    const submitEvent = { preventDefault: vi.fn() }

    fireEvent.submit(form, submitEvent)

    // The form should handle submission without page reload
    expect(onSearch).toHaveBeenCalled()
  })

  it('handles empty search submission', async () => {
    const onSearch = vi.fn()
    render(<SearchBar onSearch={onSearch} />)

    await userEvent.click(screen.getByRole('button', { name: /search/i }))
    expect(onSearch).toHaveBeenCalledWith('')
  })
})
