/* eslint-disable react-hooks/rules-of-hooks */
import { test as base } from '@playwright/test'

const MOCK_OIDC_AUTHORITY = 'https://mock-oidc.example.com'

/**
 * Extended test fixture that mocks API endpoints for E2E testing without a backend
 */
export const test = base.extend({
  page: async ({ page }, use) => {
    // Mock the config endpoint with a fake OIDC authority
    await page.route('**/config/config.json', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          oidcAuthority: MOCK_OIDC_AUTHORITY,
          oidcClientId: 'mock-client-id',
          oidcRedirectUri: 'http://localhost:5492',
          oidcScope: 'openid profile email',
        }),
      })
    })

    // Mock OIDC discovery endpoint
    await page.route(`${MOCK_OIDC_AUTHORITY}/.well-known/openid-configuration`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
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
        }),
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
          body: JSON.stringify({
            items: [],
            totalCount: 0,
            pageSize: 20,
            page: 1,
            unreadCount: 0,
          }),
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
          body: JSON.stringify({
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
          }),
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
