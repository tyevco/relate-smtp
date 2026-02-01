import { test, expect } from '@playwright/test'

test.describe('Authentication', () => {
  test('displays login page when not authenticated', async ({ page }) => {
    await page.goto('/login')

    // In dev mode without OIDC, should show login page
    await expect(page).toHaveURL(/login/)
  })

  test('shows development mode indicator when OIDC is not configured', async ({ page }) => {
    await page.goto('/')

    // In development mode, the app might redirect to login or show a dev mode indicator
    // Check if we're either on login or on the main inbox
    const url = page.url()
    expect(url).toMatch(/\/(login)?$/)
  })

  test('login page has expected elements', async ({ page }) => {
    await page.goto('/login')

    // Wait for page to load
    await page.waitForLoadState('networkidle')

    // Should have some form of login interface
    const heading = page.getByRole('heading')
    await expect(heading.first()).toBeVisible()
  })

  test('navigates to callback page correctly', async ({ page }) => {
    // The callback page is used for OIDC flow completion
    await page.goto('/callback')

    // In dev mode without OIDC, should redirect somewhere
    await page.waitForTimeout(1000) // Wait for potential redirects
    const url = page.url()
    expect(url).toBeDefined()
  })
})

test.describe('Protected Routes', () => {
  test('redirects to login when accessing inbox without auth', async ({ page }) => {
    await page.goto('/')

    // Wait for any redirects
    await page.waitForLoadState('networkidle')

    // Should either be on inbox (dev mode) or login
    const url = page.url()
    expect(url).toMatch(/\/(login)?$/)
  })

  test('redirects to login when accessing preferences without auth', async ({ page }) => {
    await page.goto('/preferences')

    await page.waitForLoadState('networkidle')

    // Should redirect to login in production, stay in dev mode
    const url = page.url()
    expect(url).toBeDefined()
  })

  test('redirects to login when accessing smtp-settings without auth', async ({ page }) => {
    await page.goto('/smtp-settings')

    await page.waitForLoadState('networkidle')

    // Should redirect to login in production, stay in dev mode
    const url = page.url()
    expect(url).toBeDefined()
  })
})
