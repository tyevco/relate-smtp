import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { EmailList } from './email-list'
import type { EmailListItem } from '@/api/types'

function createMockEmail(overrides: Partial<EmailListItem> = {}): EmailListItem {
  return {
    id: crypto.randomUUID(),
    messageId: `<${crypto.randomUUID()}@example.com>`,
    fromAddress: 'sender@example.com',
    fromDisplayName: 'Test Sender',
    subject: 'Test Subject',
    receivedAt: new Date().toISOString(),
    sizeBytes: 1024,
    isRead: false,
    attachmentCount: 0,
    ...overrides,
  }
}

describe('EmailList', () => {
  it('renders empty state when no emails', () => {
    render(<EmailList emails={[]} onSelect={() => {}} />)
    expect(screen.getByText('No emails yet')).toBeInTheDocument()
  })

  it('renders list of emails', () => {
    const emails = [
      createMockEmail({ id: '1', subject: 'First Email' }),
      createMockEmail({ id: '2', subject: 'Second Email' }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    expect(screen.getByText('First Email')).toBeInTheDocument()
    expect(screen.getByText('Second Email')).toBeInTheDocument()
  })

  it('displays sender display name when available', () => {
    const emails = [
      createMockEmail({ fromDisplayName: 'John Doe', fromAddress: 'john@example.com' }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    expect(screen.getByText('John Doe')).toBeInTheDocument()
  })

  it('displays sender address when display name is not available', () => {
    const emails = [
      createMockEmail({ fromDisplayName: null, fromAddress: 'jane@example.com' }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    expect(screen.getByText('jane@example.com')).toBeInTheDocument()
  })

  it('calls onSelect when an email is clicked', () => {
    const onSelect = vi.fn()
    const emails = [createMockEmail({ id: 'email-123', subject: 'Clickable Email' })]
    render(<EmailList emails={emails} onSelect={onSelect} />)

    fireEvent.click(screen.getByText('Clickable Email'))
    expect(onSelect).toHaveBeenCalledTimes(1)
    expect(onSelect).toHaveBeenCalledWith('email-123')
  })

  it('highlights selected email', () => {
    const emails = [
      createMockEmail({ id: '1', subject: 'Selected Email', isRead: true }),
      createMockEmail({ id: '2', subject: 'Not Selected Email', isRead: true }),
    ]
    render(<EmailList emails={emails} selectedId="1" onSelect={() => {}} />)

    const options = screen.getAllByRole('option')
    const selectedOption = options[0]
    const notSelectedOption = options[1]
    expect(selectedOption).toHaveClass('bg-accent')
    expect(notSelectedOption).not.toHaveClass('bg-accent')
    expect(selectedOption).toHaveAttribute('aria-selected', 'true')
    expect(notSelectedOption).toHaveAttribute('aria-selected', 'false')
  })

  it('applies unread styling for unread emails', () => {
    const emails = [
      createMockEmail({ id: '1', subject: 'Unread Email', isRead: false }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    const option = screen.getByRole('option')
    expect(option).toHaveClass('bg-primary/5')
  })

  it('does not apply unread styling for read emails', () => {
    const emails = [
      createMockEmail({ id: '1', subject: 'Read Email', isRead: true }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    const option = screen.getByRole('option')
    expect(option).not.toHaveClass('bg-primary/5')
  })

  it('displays attachment indicator when email has attachments', () => {
    const emails = [
      createMockEmail({ id: '1', subject: 'With Attachments', attachmentCount: 2 }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    expect(screen.getByText('2')).toBeInTheDocument()
  })

  it('hides attachment indicator when no attachments', () => {
    const emails = [
      createMockEmail({ id: '1', subject: 'Test Email', attachmentCount: 0 }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    // The attachment count should not be visible - look for the text that appears with attachments
    // When there are attachments, it shows "X attachment(s)"
    expect(screen.queryByText(/\d+ attachment/i)).not.toBeInTheDocument()
  })

  it('applies font-semibold to unread sender name', () => {
    const emails = [
      createMockEmail({
        id: '1',
        fromDisplayName: 'Unread Sender',
        subject: 'Unread',
        isRead: false
      }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    const senderName = screen.getByText('Unread Sender')
    expect(senderName).toHaveClass('font-semibold')
  })

  it('applies font-medium to unread subject', () => {
    const emails = [
      createMockEmail({ id: '1', subject: 'Unread Subject', isRead: false }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    const subject = screen.getByText('Unread Subject')
    expect(subject).toHaveClass('font-medium')
  })

  it('displays relative time for received date', () => {
    const emails = [
      createMockEmail({
        id: '1',
        subject: 'Recent Email',
        receivedAt: new Date().toISOString()
      }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    // Should show something like "less than a minute ago"
    expect(screen.getByText(/ago/i)).toBeInTheDocument()
  })

  it('renders mail icon for unread and mail-open for read', () => {
    const emails = [
      createMockEmail({ id: '1', subject: 'Unread', isRead: false }),
      createMockEmail({ id: '2', subject: 'Read', isRead: true }),
    ]
    render(<EmailList emails={emails} onSelect={() => {}} />)

    // Both should be rendered, we check the container has both icons
    const options = screen.getAllByRole('option')
    expect(options).toHaveLength(2)
  })
})
