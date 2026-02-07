/* eslint-disable react-hooks/rules-of-hooks */
import { test as base } from '@playwright/test'

// ============================================
// Mock OIDC Configuration
// ============================================
const MOCK_OIDC_AUTHORITY = 'https://mock-oidc.example.com'
const MOCK_OIDC_CLIENT_ID = 'mock-client-id'
const MOCK_OIDC_REDIRECT_URI = 'http://localhost:5492'

const mockOidcUser = {
  id_token: 'mock-id-token',
  access_token: 'mock-access-token',
  token_type: 'Bearer',
  scope: 'openid profile email',
  profile: {
    sub: 'mock-user-id',
    name: 'Test User',
    email: 'test@example.com',
  },
  expires_at: Math.floor(Date.now() / 1000) + 3600,
}

// ============================================
// Mock API Responses
// ============================================
const MOCK_EMAILS_RESPONSE = {
  items: [],
  totalCount: 0,
  pageSize: 20,
  page: 1,
  unreadCount: 0,
}

const MOCK_PREFERENCES_RESPONSE = {
  theme: 'system',
  displayDensity: 'comfortable',
  emailsPerPage: 20,
  defaultSort: 'date',
  showPreview: true,
  groupByDate: false,
  desktopNotifications: false,
  emailDigest: false,
  digestFrequency: 'daily',
  digestTime: '09:00',
}

const MOCK_SMTP_CREDENTIALS_RESPONSE = {
  smtpServer: 'smtp.example.com',
  smtpPort: 587,
  smtpSecurePort: 465,
  pop3Server: 'pop3.example.com',
  pop3Port: 110,
  pop3SecurePort: 995,
  imapServer: 'imap.example.com',
  imapPort: 143,
  imapSecurePort: 993,
  username: 'test@example.com',
}

const MOCK_OIDC_DISCOVERY_RESPONSE = {
  issuer: MOCK_OIDC_AUTHORITY,
  authorization_endpoint: `${MOCK_OIDC_AUTHORITY}/authorize`,
  token_endpoint: `${MOCK_OIDC_AUTHORITY}/token`,
  userinfo_endpoint: `${MOCK_OIDC_AUTHORITY}/userinfo`,
  jwks_uri: `${MOCK_OIDC_AUTHORITY}/.well-known/jwks.json`,
  response_types_supported: ['code'],
  subject_types_supported: ['public'],
  id_token_signing_alg_values_supported: ['RS256'],
  scopes_supported: ['openid', 'profile', 'email'],
  token_endpoint_auth_methods_supported: ['none'],
  code_challenge_methods_supported: ['S256'],
}

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
