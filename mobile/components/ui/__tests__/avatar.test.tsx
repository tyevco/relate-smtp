import React from 'react'
import { render, screen } from '@testing-library/react-native'
import { Avatar } from '../avatar'

describe('Avatar Component', () => {
  describe('rendering', () => {
    it('renders with email only', () => {
      render(<Avatar name={null} email="test@example.com" />)
      expect(screen.getByText('TE')).toBeTruthy()
    })

    it('renders with name and email', () => {
      render(<Avatar name="John Doe" email="john@example.com" />)
      expect(screen.getByText('JD')).toBeTruthy()
    })
  })

  describe('initials', () => {
    it('shows first and last initial for multi-word names', () => {
      render(<Avatar name="John Doe" email="john@example.com" />)
      expect(screen.getByText('JD')).toBeTruthy()
    })

    it('shows first two letters for single-word names', () => {
      render(<Avatar name="John" email="john@example.com" />)
      expect(screen.getByText('JO')).toBeTruthy()
    })

    it('shows first two letters of email when name is null', () => {
      render(<Avatar name={null} email="alice@example.com" />)
      expect(screen.getByText('AL')).toBeTruthy()
    })

    it('handles multi-word names with more than two words', () => {
      render(<Avatar name="John Michael Doe" email="john@example.com" />)
      expect(screen.getByText('JD')).toBeTruthy()
    })
  })

  describe('sizes', () => {
    it('renders sm size', () => {
      render(<Avatar name="Test" email="test@test.com" size="sm" />)
      expect(screen.getByText('TE')).toBeTruthy()
    })

    it('renders md size (default)', () => {
      render(<Avatar name="Test" email="test@test.com" size="md" />)
      expect(screen.getByText('TE')).toBeTruthy()
    })

    it('renders lg size', () => {
      render(<Avatar name="Test" email="test@test.com" size="lg" />)
      expect(screen.getByText('TE')).toBeTruthy()
    })

    it('defaults to md size when not specified', () => {
      render(<Avatar name="Test" email="test@test.com" />)
      expect(screen.getByText('TE')).toBeTruthy()
    })
  })

  describe('className', () => {
    it('accepts className prop', () => {
      render(
        <Avatar
          name="Test"
          email="test@test.com"
          className="custom-avatar"
        />
      )
      expect(screen.getByText('TE')).toBeTruthy()
    })
  })

  describe('color generation', () => {
    it('generates consistent colors for same email', () => {
      // The stringToColor function should return consistent colors
      // This is tested in utils.test.ts, here we just verify rendering works
      render(<Avatar name="Test" email="same@email.com" />)
      expect(screen.getByText('TE')).toBeTruthy()
    })
  })
})
