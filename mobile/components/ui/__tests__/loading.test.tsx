import React from 'react'
import { render, screen } from '@testing-library/react-native'
import { Loading, LoadingOverlay } from '../loading'

describe('Loading Components', () => {
  describe('Loading', () => {
    it('renders activity indicator', () => {
      const { root } = render(<Loading />)
      expect(root).toBeTruthy()
    })

    it('renders with message', () => {
      render(<Loading message="Loading data..." />)
      expect(screen.getByText('Loading data...')).toBeTruthy()
    })

    it('does not show message when not provided', () => {
      const { root } = render(<Loading />)
      expect(root).toBeTruthy()
      expect(screen.queryByText('Loading data...')).toBeNull()
    })

    it('accepts className prop', () => {
      const { root } = render(<Loading className="custom-loading" />)
      expect(root).toBeTruthy()
    })
  })

  describe('LoadingOverlay', () => {
    it('renders activity indicator', () => {
      const { root } = render(<LoadingOverlay />)
      expect(root).toBeTruthy()
    })

    it('renders with message', () => {
      render(<LoadingOverlay message="Please wait..." />)
      expect(screen.getByText('Please wait...')).toBeTruthy()
    })

    it('does not show message when not provided', () => {
      const { root } = render(<LoadingOverlay />)
      expect(root).toBeTruthy()
      expect(screen.queryByText('Please wait...')).toBeNull()
    })
  })
})
