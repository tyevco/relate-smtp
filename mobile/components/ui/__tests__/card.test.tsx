import React from 'react'
import { render, screen } from '@testing-library/react-native'
import { Text } from 'react-native'
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
} from '../card'

describe('Card Components', () => {
  describe('Card', () => {
    it('renders children', () => {
      render(
        <Card>
          <Text>Card Content</Text>
        </Card>
      )
      expect(screen.getByText('Card Content')).toBeTruthy()
    })

    it('accepts className prop', () => {
      render(
        <Card className="custom-class">
          <Text>Content</Text>
        </Card>
      )
      expect(screen.getByText('Content')).toBeTruthy()
    })

    it('passes through ViewProps', () => {
      render(
        <Card testID="test-card">
          <Text>Content</Text>
        </Card>
      )
      expect(screen.getByTestId('test-card')).toBeTruthy()
    })
  })

  describe('CardHeader', () => {
    it('renders children', () => {
      render(
        <CardHeader>
          <Text>Header Content</Text>
        </CardHeader>
      )
      expect(screen.getByText('Header Content')).toBeTruthy()
    })

    it('accepts className prop', () => {
      render(
        <CardHeader className="custom-header">
          <Text>Header</Text>
        </CardHeader>
      )
      expect(screen.getByText('Header')).toBeTruthy()
    })
  })

  describe('CardTitle', () => {
    it('renders text content', () => {
      render(<CardTitle>My Title</CardTitle>)
      expect(screen.getByText('My Title')).toBeTruthy()
    })

    it('accepts className prop', () => {
      render(<CardTitle className="custom-title">Title</CardTitle>)
      expect(screen.getByText('Title')).toBeTruthy()
    })
  })

  describe('CardDescription', () => {
    it('renders text content', () => {
      render(<CardDescription>My Description</CardDescription>)
      expect(screen.getByText('My Description')).toBeTruthy()
    })

    it('accepts className prop', () => {
      render(
        <CardDescription className="custom-desc">Description</CardDescription>
      )
      expect(screen.getByText('Description')).toBeTruthy()
    })
  })

  describe('CardContent', () => {
    it('renders children', () => {
      render(
        <CardContent>
          <Text>Main Content</Text>
        </CardContent>
      )
      expect(screen.getByText('Main Content')).toBeTruthy()
    })

    it('accepts className prop', () => {
      render(
        <CardContent className="custom-content">
          <Text>Content</Text>
        </CardContent>
      )
      expect(screen.getByText('Content')).toBeTruthy()
    })
  })

  describe('CardFooter', () => {
    it('renders children', () => {
      render(
        <CardFooter>
          <Text>Footer Content</Text>
        </CardFooter>
      )
      expect(screen.getByText('Footer Content')).toBeTruthy()
    })

    it('accepts className prop', () => {
      render(
        <CardFooter className="custom-footer">
          <Text>Footer</Text>
        </CardFooter>
      )
      expect(screen.getByText('Footer')).toBeTruthy()
    })
  })

  describe('Composed Card', () => {
    it('renders a complete card with all subcomponents', () => {
      render(
        <Card>
          <CardHeader>
            <CardTitle>Test Card</CardTitle>
            <CardDescription>A test card description</CardDescription>
          </CardHeader>
          <CardContent>
            <Text>Body content here</Text>
          </CardContent>
          <CardFooter>
            <Text>Footer action</Text>
          </CardFooter>
        </Card>
      )

      expect(screen.getByText('Test Card')).toBeTruthy()
      expect(screen.getByText('A test card description')).toBeTruthy()
      expect(screen.getByText('Body content here')).toBeTruthy()
      expect(screen.getByText('Footer action')).toBeTruthy()
    })
  })
})
