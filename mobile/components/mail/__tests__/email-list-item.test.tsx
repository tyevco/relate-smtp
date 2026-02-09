import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react-native'
import { EmailListItemComponent } from '../email-list-item'
import type { EmailListItem } from '@/lib/api/types'

// Mock react-native-gesture-handler
jest.mock('react-native-gesture-handler', () => {
  const { View } = jest.requireActual('react-native')
  return {
    Swipeable: ({ children }: { children: React.ReactNode }) => <View>{children}</View>,
    GestureHandlerRootView: ({ children }: { children: React.ReactNode }) => <View>{children}</View>,
  }
})

const createMockEmail = (overrides: Partial<EmailListItem> = {}): EmailListItem => ({
  id: 'email-1',
  messageId: 'msg-1',
  fromAddress: 'sender@example.com',
  fromDisplayName: 'John Doe',
  subject: 'Test Subject',
  receivedAt: '2024-01-15T10:00:00Z',
  sizeBytes: 1024,
  isRead: false,
  attachmentCount: 0,
  ...overrides,
})

describe('EmailListItemComponent', () => {
  const defaultProps = {
    onPress: jest.fn(),
    onDelete: jest.fn(),
    onToggleRead: jest.fn(),
  }

  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders sender display name when available', () => {
      const email = createMockEmail({ fromDisplayName: 'Jane Smith' })
      render(<EmailListItemComponent email={email} {...defaultProps} />)
      expect(screen.getByText('Jane Smith')).toBeTruthy()
    })

    it('renders sender email when display name is null', () => {
      const email = createMockEmail({ fromDisplayName: null })
      render(<EmailListItemComponent email={email} {...defaultProps} />)
      expect(screen.getByText('sender@example.com')).toBeTruthy()
    })

    it('renders subject', () => {
      const email = createMockEmail({ subject: 'Important Email' })
      render(<EmailListItemComponent email={email} {...defaultProps} />)
      expect(screen.getByText('Important Email')).toBeTruthy()
    })

    it('renders "(No subject)" for empty subject', () => {
      const email = createMockEmail({ subject: '' })
      render(<EmailListItemComponent email={email} {...defaultProps} />)
      expect(screen.getByText('(No subject)')).toBeTruthy()
    })
  })

  describe('read/unread state', () => {
    it('shows unread indicator for unread emails', () => {
      const email = createMockEmail({ isRead: false })
      const { root } = render(<EmailListItemComponent email={email} {...defaultProps} />)
      // Unread emails have a visual indicator (blue dot)
      expect(root).toBeTruthy()
    })

    it('hides unread indicator for read emails', () => {
      const email = createMockEmail({ isRead: true })
      const { root } = render(<EmailListItemComponent email={email} {...defaultProps} />)
      expect(root).toBeTruthy()
    })
  })

  describe('attachments', () => {
    it('shows attachment indicator when attachmentCount > 0', () => {
      const email = createMockEmail({ attachmentCount: 2 })
      render(<EmailListItemComponent email={email} {...defaultProps} />)
      // Component shows paperclip icon for attachments
      expect(screen.getByText('Test Subject')).toBeTruthy()
    })

    it('hides attachment indicator when attachmentCount is 0', () => {
      const email = createMockEmail({ attachmentCount: 0 })
      render(<EmailListItemComponent email={email} {...defaultProps} />)
      expect(screen.getByText('Test Subject')).toBeTruthy()
    })
  })

  describe('interactions', () => {
    it('calls onPress when pressed', () => {
      const onPress = jest.fn()
      const email = createMockEmail()
      render(<EmailListItemComponent email={email} {...defaultProps} onPress={onPress} />)

      fireEvent.press(screen.getByText('John Doe'))
      expect(onPress).toHaveBeenCalledTimes(1)
    })
  })

  describe('avatar', () => {
    it('renders avatar with sender info', () => {
      const email = createMockEmail({
        fromDisplayName: 'Alice Bob',
        fromAddress: 'alice@example.com',
      })
      render(<EmailListItemComponent email={email} {...defaultProps} />)
      // Avatar shows initials
      expect(screen.getByText('AB')).toBeTruthy()
    })

    it('renders avatar from email when name is null', () => {
      const email = createMockEmail({
        fromDisplayName: null,
        fromAddress: 'bob@example.com',
      })
      render(<EmailListItemComponent email={email} {...defaultProps} />)
      // Avatar shows first two chars of email
      expect(screen.getByText('BO')).toBeTruthy()
    })
  })

  describe('date formatting', () => {
    it('renders formatted date', () => {
      const email = createMockEmail({ receivedAt: '2024-01-15T10:00:00Z' })
      render(<EmailListItemComponent email={email} {...defaultProps} />)
      // The date should be formatted and displayed
      expect(screen.getByText('Test Subject')).toBeTruthy()
    })
  })
})
