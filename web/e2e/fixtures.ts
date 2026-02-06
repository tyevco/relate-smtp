/* eslint-disable react-hooks/rules-of-hooks */
import { test as base } from '@playwright/test'

/**
 * Extended test fixture that mocks API endpoints for E2E testing without a backend
 */
export const test = base.extend({
  page: async ({ page }, use) => {
    // Mock the config endpoint
    await page.route('**/config/config.json', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          oidcAuthority: '',
          oidcClientId: '',
          oidcRedirectUri: '',
          oidcScope: 'openid profile email',
        }),
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
