/* eslint-disable react-hooks/rules-of-hooks */
import { test as base } from '@playwright/test'

// ============================================
// Mock OIDC Configuration
// ============================================
const MOCK_OIDC_AUTHORITY = 'https://mock-oidc.example.com'
const MOCK_OIDC_CLIENT_ID = 'mock-client-id'
const MOCK_OIDC_REDIRECT_URI = 'http://localhost:5492'

// ============================================
// Mock Data Factory Functions
// ============================================

/** Create a mock OIDC user with optional overrides */
export function createMockOidcUser(overrides?: Partial<{
  id_token: string
  access_token: string
  profile: { sub: string; name: string; email: string }
}>) {
  return {
    id_token: overrides?.id_token ?? 'mock-id-token',
    access_token: overrides?.access_token ?? 'mock-access-token',
    token_type: 'Bearer',
    scope: 'openid profile email',
    profile: {
      sub: overrides?.profile?.sub ?? 'mock-user-id',
      name: overrides?.profile?.name ?? 'Test User',
      email: overrides?.profile?.email ?? 'test@example.com',
    },
    expires_at: Math.floor(Date.now() / 1000) + 3600,
  }
}

/** Create a mock emails list response with optional overrides */
export function createMockEmailsResponse(overrides?: Partial<{
  items: unknown[]
  totalCount: number
  page: number
  pageSize: number
  unreadCount: number
}>) {
  return {
    items: overrides?.items ?? [],
    totalCount: overrides?.totalCount ?? 0,
    pageSize: overrides?.pageSize ?? 20,
    page: overrides?.page ?? 1,
    unreadCount: overrides?.unreadCount ?? 0,
  }
}

/** Create mock user preferences with optional overrides */
export function createMockPreferences(overrides?: Partial<{
  theme: string
  displayDensity: string
  emailsPerPage: number
  showPreview: boolean
}>) {
  return {
    theme: overrides?.theme ?? 'system',
    displayDensity: overrides?.displayDensity ?? 'comfortable',
    emailsPerPage: overrides?.emailsPerPage ?? 20,
    defaultSort: 'date',
    showPreview: overrides?.showPreview ?? true,
    groupByDate: false,
    desktopNotifications: false,
    emailDigest: false,
    digestFrequency: 'daily',
    digestTime: '09:00',
  }
}

/** Create mock SMTP credentials with optional overrides */
export function createMockSmtpCredentials(overrides?: Partial<{
  username: string
  smtpServer: string
}>) {
  return {
    smtpServer: overrides?.smtpServer ?? 'smtp.example.com',
    smtpPort: 587,
    smtpSecurePort: 465,
    pop3Server: 'pop3.example.com',
    pop3Port: 110,
    pop3SecurePort: 995,
    imapServer: 'imap.example.com',
    imapPort: 143,
    imapSecurePort: 993,
    username: overrides?.username ?? 'test@example.com',
  }
}

/** Create mock OIDC discovery response */
export function createMockOidcDiscovery(authority = MOCK_OIDC_AUTHORITY) {
  return {
    issuer: authority,
    authorization_endpoint: `${authority}/authorize`,
    token_endpoint: `${authority}/token`,
    userinfo_endpoint: `${authority}/userinfo`,
    jwks_uri: `${authority}/.well-known/jwks.json`,
    response_types_supported: ['code'],
    subject_types_supported: ['public'],
    id_token_signing_alg_values_supported: ['RS256'],
    scopes_supported: ['openid', 'profile', 'email'],
    token_endpoint_auth_methods_supported: ['none'],
    code_challenge_methods_supported: ['S256'],
  }
}

// Pre-built instances for default test scenarios
const mockOidcUser = createMockOidcUser()
const MOCK_EMAILS_RESPONSE = createMockEmailsResponse()
const MOCK_PREFERENCES_RESPONSE = createMockPreferences()
const MOCK_SMTP_CREDENTIALS_RESPONSE = createMockSmtpCredentials()
const MOCK_OIDC_DISCOVERY_RESPONSE = createMockOidcDiscovery()

/**
 * Extended test fixture that mocks API endpoints for E2E testing without a backend
 */
export const test = base.extend({
  page: async ({ page }, use) => {
    // Inject mock auth tokens into sessionStorage before any navigation
    await page.addInitScript(
      ({ authority, clientId, user }) => {
        const storageKey = `oidc.user:${authority}:${clientId}`
        sessionStorage.setItem(storageKey, JSON.stringify(user))
      },
      { authority: MOCK_OIDC_AUTHORITY, clientId: MOCK_OIDC_CLIENT_ID, user: mockOidcUser }
    )

    // Mock the config endpoint with a fake OIDC authority
    await page.route('**/config/config.json', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          oidcAuthority: MOCK_OIDC_AUTHORITY,
          oidcClientId: MOCK_OIDC_CLIENT_ID,
          oidcRedirectUri: MOCK_OIDC_REDIRECT_URI,
          oidcScope: 'openid profile email',
        }),
      })
    })

    // Mock OIDC discovery endpoint
    await page.route(`${MOCK_OIDC_AUTHORITY}/.well-known/openid-configuration`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_OIDC_DISCOVERY_RESPONSE),
      })
    })

    // Mock OIDC JWKS endpoint
    await page.route(`${MOCK_OIDC_AUTHORITY}/.well-known/jwks.json`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ keys: [] }),
      })
    })

    // Mock API endpoints to return empty/default responses
    await page.route('**/api/emails**', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(MOCK_EMAILS_RESPONSE),
        })
      } else {
        await route.continue()
      }
    })

    await page.route('**/api/labels**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
    })

    await page.route('**/api/preferences**', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(MOCK_PREFERENCES_RESPONSE),
        })
      } else {
        await route.continue()
      }
    })

    await page.route('**/api/smtp-api-keys**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
    })

    await page.route('**/api/smtp-credentials**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_SMTP_CREDENTIALS_RESPONSE),
      })
    })

    // Mock SignalR hub negotiation
    await page.route('**/hubs/email/negotiate**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          connectionId: 'mock-connection-id',
          availableTransports: [],
        }),
      })
    })

    await use(page)
  },
})

export { expect } from '@playwright/test'
