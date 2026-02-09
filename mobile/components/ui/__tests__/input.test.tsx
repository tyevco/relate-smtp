import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react-native'
import { Input } from '../input'

describe('Input Component', () => {
  describe('rendering', () => {
    it('renders basic input', () => {
      render(<Input placeholder="Enter text" testID="input" />)
      expect(screen.getByTestId('input')).toBeTruthy()
    })

    it('renders with placeholder', () => {
      render(<Input placeholder="Enter your email" />)
      expect(screen.getByPlaceholderText('Enter your email')).toBeTruthy()
    })

    it('renders with value', () => {
      render(<Input value="test@example.com" testID="input" />)
      expect(screen.getByDisplayValue('test@example.com')).toBeTruthy()
    })
  })

  describe('label prop', () => {
    it('renders label when provided', () => {
      render(<Input label="Email Address" placeholder="Enter email" />)
      expect(screen.getByText('Email Address')).toBeTruthy()
    })

    it('does not render label when not provided', () => {
      render(<Input placeholder="Enter email" />)
      expect(screen.queryByText('Email Address')).toBeNull()
    })
  })

  describe('error prop', () => {
    it('renders error message when provided', () => {
      render(<Input error="Invalid email format" placeholder="Enter email" />)
      expect(screen.getByText('Invalid email format')).toBeTruthy()
    })

    it('does not render error when not provided', () => {
      render(<Input placeholder="Enter email" />)
      expect(screen.queryByText('Invalid email format')).toBeNull()
    })
  })

  describe('interaction', () => {
    it('calls onChangeText when text changes', () => {
      const onChangeText = jest.fn()
      render(
        <Input
          placeholder="Enter text"
          onChangeText={onChangeText}
          testID="input"
        />
      )

      fireEvent.changeText(screen.getByTestId('input'), 'new text')
      expect(onChangeText).toHaveBeenCalledWith('new text')
    })

    it('calls onBlur when blurred', () => {
      const onBlur = jest.fn()
      render(<Input placeholder="Enter text" onBlur={onBlur} testID="input" />)

      fireEvent(screen.getByTestId('input'), 'blur')
      expect(onBlur).toHaveBeenCalled()
    })

    it('calls onFocus when focused', () => {
      const onFocus = jest.fn()
      render(<Input placeholder="Enter text" onFocus={onFocus} testID="input" />)

      fireEvent(screen.getByTestId('input'), 'focus')
      expect(onFocus).toHaveBeenCalled()
    })
  })

  describe('TextInput props', () => {
    it('supports secureTextEntry for passwords', () => {
      render(
        <Input
          placeholder="Enter password"
          secureTextEntry
          testID="password-input"
        />
      )
      expect(screen.getByTestId('password-input').props.secureTextEntry).toBe(
        true
      )
    })

    it('supports keyboardType', () => {
      render(
        <Input
          placeholder="Enter email"
          keyboardType="email-address"
          testID="email-input"
        />
      )
      expect(screen.getByTestId('email-input').props.keyboardType).toBe(
        'email-address'
      )
    })

    it('supports autoCapitalize', () => {
      render(
        <Input
          placeholder="Enter text"
          autoCapitalize="none"
          testID="input"
        />
      )
      expect(screen.getByTestId('input').props.autoCapitalize).toBe('none')
    })

    it('supports editable prop', () => {
      render(
        <Input placeholder="Read only" editable={false} testID="input" />
      )
      expect(screen.getByTestId('input').props.editable).toBe(false)
    })
  })

  describe('className props', () => {
    it('accepts className prop', () => {
      render(<Input placeholder="Enter text" className="custom-input" />)
      expect(screen.getByPlaceholderText('Enter text')).toBeTruthy()
    })

    it('accepts containerClassName prop', () => {
      render(
        <Input placeholder="Enter text" containerClassName="custom-container" />
      )
      expect(screen.getByPlaceholderText('Enter text')).toBeTruthy()
    })
  })
})
