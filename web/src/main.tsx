import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider, createRouter } from '@tanstack/react-router'
import { AuthProvider } from './auth/AuthProvider'
import { routeTree } from './routeTree.gen'
import { loadConfig } from './config'
import './index.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 30, // 30 seconds - appropriate for email app requiring fresh data
      retry: 1,
    },
  },
})

const router = createRouter({
  routeTree,
  context: {
    queryClient,
  },
  defaultPreload: 'intent',
})

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}

// Load runtime configuration before rendering the app
loadConfig().then(() => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <AuthProvider>
        <QueryClientProvider client={queryClient}>
          <RouterProvider router={router} />
        </QueryClientProvider>
      </AuthProvider>
    </StrictMode>,
  )
}).catch((error) => {
  console.error('Failed to load configuration:', error)
  // Render error state
  createRoot(document.getElementById('root')!).render(
    <div style={{ padding: '2rem', textAlign: 'center' }}>
      <h1>Configuration Error</h1>
      <p>Failed to load application configuration. Please check your deployment.</p>
      <pre style={{ textAlign: 'left', background: '#f5f5f5', padding: '1rem', borderRadius: '4px' }}>
        {error.message}
      </pre>
    </div>
  )
})
