import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
} from './card'

describe('Card', () => {
  it('renders children correctly', () => {
    render(<Card>Card content</Card>)
    expect(screen.getByText('Card content')).toBeInTheDocument()
  })

  it('applies default styles', () => {
    render(<Card data-testid="card">Content</Card>)
    const card = screen.getByTestId('card')
    expect(card).toHaveClass('rounded-lg')
    expect(card).toHaveClass('border')
    expect(card).toHaveClass('bg-card')
    expect(card).toHaveClass('shadow')
  })

  it('merges custom className', () => {
    render(
      <Card className="custom-card" data-testid="card">
        Content
      </Card>
    )
    expect(screen.getByTestId('card')).toHaveClass('custom-card')
  })

  it('forwards ref correctly', () => {
    const ref = { current: null as HTMLDivElement | null }
    render(<Card ref={ref}>Content</Card>)
    expect(ref.current).toBeInstanceOf(HTMLDivElement)
  })
})

describe('CardHeader', () => {
  it('renders children correctly', () => {
    render(<CardHeader>Header content</CardHeader>)
    expect(screen.getByText('Header content')).toBeInTheDocument()
  })

  it('applies default styles', () => {
    render(<CardHeader data-testid="header">Header</CardHeader>)
    const header = screen.getByTestId('header')
    expect(header).toHaveClass('flex')
    expect(header).toHaveClass('flex-col')
    expect(header).toHaveClass('space-y-1.5')
    expect(header).toHaveClass('p-6')
  })

  it('merges custom className', () => {
    render(
      <CardHeader className="custom-header" data-testid="header">
        Header
      </CardHeader>
    )
    expect(screen.getByTestId('header')).toHaveClass('custom-header')
  })
})

describe('CardTitle', () => {
  it('renders children correctly', () => {
    render(<CardTitle>Card Title</CardTitle>)
    expect(screen.getByText('Card Title')).toBeInTheDocument()
  })

  it('renders as h3 element', () => {
    render(<CardTitle>Title</CardTitle>)
    expect(screen.getByRole('heading', { level: 3 })).toBeInTheDocument()
  })

  it('applies default styles', () => {
    render(<CardTitle data-testid="title">Title</CardTitle>)
    const title = screen.getByTestId('title')
    expect(title).toHaveClass('font-semibold')
    expect(title).toHaveClass('leading-none')
    expect(title).toHaveClass('tracking-tight')
  })

  it('merges custom className', () => {
    render(
      <CardTitle className="custom-title" data-testid="title">
        Title
      </CardTitle>
    )
    expect(screen.getByTestId('title')).toHaveClass('custom-title')
  })
})

describe('CardDescription', () => {
  it('renders children correctly', () => {
    render(<CardDescription>Description text</CardDescription>)
    expect(screen.getByText('Description text')).toBeInTheDocument()
  })

  it('applies default styles', () => {
    render(
      <CardDescription data-testid="description">Description</CardDescription>
    )
    const desc = screen.getByTestId('description')
    expect(desc).toHaveClass('text-sm')
    expect(desc).toHaveClass('text-muted-foreground')
  })

  it('merges custom className', () => {
    render(
      <CardDescription className="custom-desc" data-testid="description">
        Description
      </CardDescription>
    )
    expect(screen.getByTestId('description')).toHaveClass('custom-desc')
  })
})

describe('CardContent', () => {
  it('renders children correctly', () => {
    render(<CardContent>Content area</CardContent>)
    expect(screen.getByText('Content area')).toBeInTheDocument()
  })

  it('applies default styles', () => {
    render(<CardContent data-testid="content">Content</CardContent>)
    const content = screen.getByTestId('content')
    expect(content).toHaveClass('p-6')
    expect(content).toHaveClass('pt-0')
  })

  it('merges custom className', () => {
    render(
      <CardContent className="custom-content" data-testid="content">
        Content
      </CardContent>
    )
    expect(screen.getByTestId('content')).toHaveClass('custom-content')
  })
})

describe('CardFooter', () => {
  it('renders children correctly', () => {
    render(<CardFooter>Footer content</CardFooter>)
    expect(screen.getByText('Footer content')).toBeInTheDocument()
  })

  it('applies default styles', () => {
    render(<CardFooter data-testid="footer">Footer</CardFooter>)
    const footer = screen.getByTestId('footer')
    expect(footer).toHaveClass('flex')
    expect(footer).toHaveClass('items-center')
    expect(footer).toHaveClass('p-6')
    expect(footer).toHaveClass('pt-0')
  })

  it('merges custom className', () => {
    render(
      <CardFooter className="custom-footer" data-testid="footer">
        Footer
      </CardFooter>
    )
    expect(screen.getByTestId('footer')).toHaveClass('custom-footer')
  })
})

describe('Card composition', () => {
  it('composes all card parts together correctly', () => {
    render(
      <Card data-testid="full-card">
        <CardHeader>
          <CardTitle>Test Card</CardTitle>
          <CardDescription>A test card component</CardDescription>
        </CardHeader>
        <CardContent>Main content goes here</CardContent>
        <CardFooter>Footer actions</CardFooter>
      </Card>
    )

    expect(screen.getByText('Test Card')).toBeInTheDocument()
    expect(screen.getByText('A test card component')).toBeInTheDocument()
    expect(screen.getByText('Main content goes here')).toBeInTheDocument()
    expect(screen.getByText('Footer actions')).toBeInTheDocument()
  })
})
