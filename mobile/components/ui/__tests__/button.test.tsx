import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react-native'
import { Button } from '../button'

describe('Button Component', () => {
  describe('rendering', () => {
    it('renders with text children', () => {
      render(<Button>Click me</Button>)
      expect(screen.getByText('Click me')).toBeTruthy()
    })

    it('renders with custom React node children', () => {
      render(
        <Button testID="custom-button">
          <></>
        </Button>
      )
      expect(screen.getByTestId('custom-button')).toBeTruthy()
    })
  })

  describe('variants', () => {
    it('renders default variant', () => {
      render(<Button variant="default">Default</Button>)
      expect(screen.getByText('Default')).toBeTruthy()
    })

    it('renders destructive variant', () => {
      render(<Button variant="destructive">Delete</Button>)
      expect(screen.getByText('Delete')).toBeTruthy()
    })

    it('renders outline variant', () => {
      render(<Button variant="outline">Outline</Button>)
      expect(screen.getByText('Outline')).toBeTruthy()
    })

    it('renders secondary variant', () => {
      render(<Button variant="secondary">Secondary</Button>)
      expect(screen.getByText('Secondary')).toBeTruthy()
    })

    it('renders ghost variant', () => {
      render(<Button variant="ghost">Ghost</Button>)
      expect(screen.getByText('Ghost')).toBeTruthy()
    })

    it('renders link variant', () => {
      render(<Button variant="link">Link</Button>)
      expect(screen.getByText('Link')).toBeTruthy()
    })
  })

  describe('sizes', () => {
    it('renders default size', () => {
      render(<Button size="default">Default Size</Button>)
      expect(screen.getByText('Default Size')).toBeTruthy()
    })

    it('renders sm size', () => {
      render(<Button size="sm">Small</Button>)
      expect(screen.getByText('Small')).toBeTruthy()
    })

    it('renders lg size', () => {
      render(<Button size="lg">Large</Button>)
      expect(screen.getByText('Large')).toBeTruthy()
    })

    it('renders icon size', () => {
      render(<Button size="icon">+</Button>)
      expect(screen.getByText('+')).toBeTruthy()
    })
  })

  describe('states', () => {
    it('shows loading indicator when loading', () => {
      render(<Button loading>Submit</Button>)
      // Text should not be visible when loading
      expect(screen.queryByText('Submit')).toBeNull()
    })

    it('is disabled when disabled prop is true', () => {
      const onPress = jest.fn()
      render(<Button disabled onPress={onPress}>Disabled</Button>)

      fireEvent.press(screen.getByText('Disabled'))
      expect(onPress).not.toHaveBeenCalled()
    })

    it('is disabled when loading', () => {
      const onPress = jest.fn()
      render(<Button loading onPress={onPress} testID="loading-button">Loading</Button>)

      fireEvent.press(screen.getByTestId('loading-button'))
      expect(onPress).not.toHaveBeenCalled()
    })
  })

  describe('interaction', () => {
    it('calls onPress when pressed', () => {
      const onPress = jest.fn()
      render(<Button onPress={onPress}>Press me</Button>)

      fireEvent.press(screen.getByText('Press me'))
      expect(onPress).toHaveBeenCalledTimes(1)
    })

    it('does not call onPress when disabled', () => {
      const onPress = jest.fn()
      render(<Button disabled onPress={onPress}>Disabled</Button>)

      fireEvent.press(screen.getByText('Disabled'))
      expect(onPress).not.toHaveBeenCalled()
    })
  })

  describe('className prop', () => {
    it('accepts custom className', () => {
      render(<Button className="mt-4">Styled</Button>)
      expect(screen.getByText('Styled')).toBeTruthy()
    })
  })
})
