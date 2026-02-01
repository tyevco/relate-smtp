import React from 'react'
import { render, screen } from '@testing-library/react-native'
import { Text, View } from 'react-native'
import { EmptyState } from '../empty-state'

describe('EmptyState Component', () => {
  describe('rendering', () => {
    it('renders with title only', () => {
      render(<EmptyState title="No items found" />)
      expect(screen.getByText('No items found')).toBeTruthy()
    })

    it('renders with title and description', () => {
      render(
        <EmptyState
          title="No items"
          description="Your inbox is empty"
        />
      )
      expect(screen.getByText('No items')).toBeTruthy()
      expect(screen.getByText('Your inbox is empty')).toBeTruthy()
    })
  })

  describe('optional props', () => {
    it('renders icon when provided', () => {
      render(
        <EmptyState
          title="No items"
          icon={<View testID="custom-icon" />}
        />
      )
      expect(screen.getByTestId('custom-icon')).toBeTruthy()
    })

    it('does not render icon when not provided', () => {
      render(<EmptyState title="No items" />)
      expect(screen.queryByTestId('custom-icon')).toBeNull()
    })

    it('renders action when provided', () => {
      render(
        <EmptyState
          title="No items"
          action={<Text testID="action-button">Refresh</Text>}
        />
      )
      expect(screen.getByTestId('action-button')).toBeTruthy()
    })

    it('does not render action when not provided', () => {
      render(<EmptyState title="No items" />)
      expect(screen.queryByTestId('action-button')).toBeNull()
    })

    it('does not render description when not provided', () => {
      render(<EmptyState title="Empty" />)
      expect(screen.queryByText('Your inbox is empty')).toBeNull()
    })
  })

  describe('className', () => {
    it('accepts className prop', () => {
      render(
        <EmptyState
          title="No items"
          className="custom-empty-state"
        />
      )
      expect(screen.getByText('No items')).toBeTruthy()
    })
  })

  describe('complete composition', () => {
    it('renders with all props', () => {
      render(
        <EmptyState
          icon={<View testID="icon" />}
          title="No emails"
          description="Your inbox is empty"
          action={<Text>Check later</Text>}
          className="custom"
        />
      )
      expect(screen.getByTestId('icon')).toBeTruthy()
      expect(screen.getByText('No emails')).toBeTruthy()
      expect(screen.getByText('Your inbox is empty')).toBeTruthy()
      expect(screen.getByText('Check later')).toBeTruthy()
    })
  })
})
