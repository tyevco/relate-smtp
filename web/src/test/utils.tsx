import { type ReactElement, type ReactNode } from 'react'
import { render, type RenderOptions } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import userEvent from '@testing-library/user-event'

// Create a test query client with default settings for tests
function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
        staleTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  })
}

interface AllProvidersProps {
  children: ReactNode
  queryClient?: QueryClient
}

function AllProviders({ children, queryClient }: AllProvidersProps) {
  const client = queryClient ?? createTestQueryClient()
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>
}

interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  queryClient?: QueryClient
}

function customRender(ui: ReactElement, options: CustomRenderOptions = {}) {
  const { queryClient, ...renderOptions } = options
  return render(ui, {
    wrapper: ({ children }) => (
      <AllProviders queryClient={queryClient}>{children}</AllProviders>
    ),
    ...renderOptions,
  })
}

// Helper to create user event with setup
function setupUser() {
  return userEvent.setup()
}

// Re-export everything from testing-library
export * from '@testing-library/react'

// Override render method
export { customRender as render, setupUser, createTestQueryClient }
