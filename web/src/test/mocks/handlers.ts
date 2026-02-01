import { http, HttpResponse } from 'msw'
import type {
  EmailListResponse,
  EmailDetail,
  Profile,
  SmtpCredentials,
  Label,
  EmailFilter,
  UserPreference,
  CreatedApiKey,
} from '@/api/types'

const BASE_URL = '/api'

// Mock data factories
export function createMockEmailListItem(overrides = {}) {
  return {
    id: crypto.randomUUID(),
    messageId: `<${crypto.randomUUID()}@example.com>`,
    fromAddress: 'sender@example.com',
    fromDisplayName: 'Test Sender',
    subject: 'Test Email Subject',
    receivedAt: new Date().toISOString(),
    sizeBytes: 1024,
    isRead: false,
    attachmentCount: 0,
    ...overrides,
  }
}

export function createMockEmailDetail(overrides = {}): EmailDetail {
  return {
    id: crypto.randomUUID(),
    messageId: `<${crypto.randomUUID()}@example.com>`,
    fromAddress: 'sender@example.com',
    fromDisplayName: 'Test Sender',
    subject: 'Test Email Subject',
    textBody: 'This is the email body text.',
    htmlBody: '<p>This is the email body HTML.</p>',
    receivedAt: new Date().toISOString(),
    sizeBytes: 2048,
    isRead: false,
    recipients: [
      {
        id: crypto.randomUUID(),
        address: 'recipient@example.com',
        displayName: 'Test Recipient',
        type: 'To',
      },
    ],
    attachments: [],
    ...overrides,
  }
}

export function createMockProfile(overrides = {}): Profile {
  return {
    id: crypto.randomUUID(),
    email: 'user@example.com',
    displayName: 'Test User',
    createdAt: new Date().toISOString(),
    lastLoginAt: new Date().toISOString(),
    additionalAddresses: [],
    ...overrides,
  }
}

export function createMockSmtpCredentials(overrides = {}): SmtpCredentials {
  return {
    connectionInfo: {
      smtpServer: 'smtp.example.com',
      smtpPort: 587,
      smtpSecurePort: 465,
      smtpEnabled: true,
      pop3Server: 'pop3.example.com',
      pop3Port: 110,
      pop3SecurePort: 995,
      pop3Enabled: true,
      imapServer: 'imap.example.com',
      imapPort: 143,
      imapSecurePort: 993,
      imapEnabled: true,
      username: 'user@example.com',
      activeKeyCount: 1,
    },
    keys: [
      {
        id: crypto.randomUUID(),
        name: 'Test Key',
        createdAt: new Date().toISOString(),
        lastUsedAt: null,
        isActive: true,
        scopes: ['smtp', 'pop3', 'imap'],
      },
    ],
    ...overrides,
  }
}

export function createMockLabel(overrides = {}): Label {
  return {
    id: crypto.randomUUID(),
    name: 'Test Label',
    color: '#3b82f6',
    sortOrder: 0,
    createdAt: new Date().toISOString(),
    ...overrides,
  }
}

export function createMockFilter(overrides = {}): EmailFilter {
  return {
    id: crypto.randomUUID(),
    name: 'Test Filter',
    isEnabled: true,
    priority: 0,
    fromAddressContains: 'test@',
    subjectContains: undefined,
    bodyContains: undefined,
    hasAttachments: undefined,
    markAsRead: false,
    assignLabelId: undefined,
    assignLabelName: undefined,
    assignLabelColor: undefined,
    delete: false,
    createdAt: new Date().toISOString(),
    lastAppliedAt: undefined,
    timesApplied: 0,
    ...overrides,
  }
}

export function createMockPreferences(overrides = {}): UserPreference {
  return {
    id: crypto.randomUUID(),
    userId: crypto.randomUUID(),
    theme: 'system',
    displayDensity: 'comfortable',
    emailsPerPage: 20,
    defaultSort: 'receivedAt:desc',
    showPreview: true,
    groupByDate: true,
    desktopNotifications: false,
    emailDigest: false,
    digestFrequency: 'daily',
    digestTime: '09:00',
    updatedAt: new Date().toISOString(),
    ...overrides,
  }
}

// Default mock data
const mockEmails = [
  createMockEmailListItem({ id: '1', subject: 'First Email' }),
  createMockEmailListItem({ id: '2', subject: 'Second Email', isRead: true }),
  createMockEmailListItem({
    id: '3',
    subject: 'Third Email',
    attachmentCount: 2,
  }),
]

const mockLabels = [
  createMockLabel({ id: 'label-1', name: 'Work', color: '#ef4444' }),
  createMockLabel({ id: 'label-2', name: 'Personal', color: '#22c55e' }),
]

const mockFilters = [
  createMockFilter({ id: 'filter-1', name: 'Work Emails' }),
]

// Request handlers
export const handlers = [
  // Email endpoints
  http.get(`${BASE_URL}/emails`, ({ request }) => {
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')

    const response: EmailListResponse = {
      items: mockEmails.slice((page - 1) * pageSize, page * pageSize),
      totalCount: mockEmails.length,
      unreadCount: mockEmails.filter((e) => !e.isRead).length,
      page,
      pageSize,
    }

    return HttpResponse.json(response)
  }),

  http.get(`${BASE_URL}/emails/search`, ({ request }) => {
    const url = new URL(request.url)
    const query = url.searchParams.get('q') || ''
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')

    const filtered = mockEmails.filter(
      (e) =>
        e.subject.toLowerCase().includes(query.toLowerCase()) ||
        e.fromAddress.toLowerCase().includes(query.toLowerCase())
    )

    const response: EmailListResponse = {
      items: filtered.slice((page - 1) * pageSize, page * pageSize),
      totalCount: filtered.length,
      unreadCount: filtered.filter((e) => !e.isRead).length,
      page,
      pageSize,
    }

    return HttpResponse.json(response)
  }),

  // Sent mail routes - must be before /emails/:id to avoid conflict
  http.get(`${BASE_URL}/emails/sent/addresses`, () => {
    return HttpResponse.json(['user@example.com'])
  }),

  http.get(`${BASE_URL}/emails/sent`, ({ request }) => {
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')

    const response: EmailListResponse = {
      items: [],
      totalCount: 0,
      unreadCount: 0,
      page,
      pageSize,
    }

    return HttpResponse.json(response)
  }),

  // Thread routes - must be before /emails/:id
  http.get(`${BASE_URL}/emails/threads/:threadId`, () => {
    return HttpResponse.json([createMockEmailDetail()])
  }),

  // Bulk operations - must be before /emails/:id
  http.post(`${BASE_URL}/emails/bulk/mark-read`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  http.post(`${BASE_URL}/emails/bulk/delete`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  // Generic email by ID route - must be after more specific routes
  http.get(`${BASE_URL}/emails/:id`, ({ params }) => {
    const { id } = params
    const email = mockEmails.find((e) => e.id === id)
    if (!email) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json(createMockEmailDetail({ ...email }))
  }),

  http.patch(`${BASE_URL}/emails/:id`, async ({ params, request }) => {
    const { id } = params
    const body = (await request.json()) as { isRead?: boolean }
    const email = mockEmails.find((e) => e.id === id)
    if (!email) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json(
      createMockEmailDetail({ ...email, isRead: body.isRead ?? email.isRead })
    )
  }),

  http.delete(`${BASE_URL}/emails/:id`, ({ params }) => {
    const { id } = params
    const index = mockEmails.findIndex((e) => e.id === id)
    if (index === -1) {
      return new HttpResponse(null, { status: 404 })
    }
    return new HttpResponse(null, { status: 204 })
  }),

  // Profile endpoints
  http.get(`${BASE_URL}/profile`, () => {
    return HttpResponse.json(createMockProfile())
  }),

  http.put(`${BASE_URL}/profile`, async ({ request }) => {
    const body = (await request.json()) as { displayName?: string }
    return HttpResponse.json(createMockProfile({ displayName: body.displayName }))
  }),

  http.post(`${BASE_URL}/profile/addresses`, async ({ request }) => {
    const body = (await request.json()) as { address: string }
    return HttpResponse.json({
      id: crypto.randomUUID(),
      address: body.address,
      isVerified: false,
      addedAt: new Date().toISOString(),
    })
  }),

  http.delete(`${BASE_URL}/profile/addresses/:id`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  // SMTP Credentials endpoints
  http.get(`${BASE_URL}/smtp-credentials`, () => {
    return HttpResponse.json(createMockSmtpCredentials())
  }),

  http.post(`${BASE_URL}/smtp-credentials`, async ({ request }) => {
    const body = (await request.json()) as { name: string; scopes?: string[] }
    const response: CreatedApiKey = {
      id: crypto.randomUUID(),
      name: body.name,
      apiKey: 'test-api-key-' + crypto.randomUUID(),
      scopes: body.scopes || ['smtp'],
      createdAt: new Date().toISOString(),
    }
    return HttpResponse.json(response)
  }),

  http.delete(`${BASE_URL}/smtp-credentials/:id`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  // Label endpoints
  http.get(`${BASE_URL}/labels`, () => {
    return HttpResponse.json(mockLabels)
  }),

  http.post(`${BASE_URL}/labels`, async ({ request }) => {
    const body = (await request.json()) as { name: string; color: string; sortOrder?: number }
    return HttpResponse.json(createMockLabel(body))
  }),

  http.put(`${BASE_URL}/labels/:id`, async ({ params, request }) => {
    const { id } = params
    const body = (await request.json()) as { name?: string; color?: string; sortOrder?: number }
    const label = mockLabels.find((l) => l.id === id)
    if (!label) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json({ ...label, ...body })
  }),

  http.delete(`${BASE_URL}/labels/:id`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  http.post(`${BASE_URL}/labels/emails/:emailId`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  http.delete(`${BASE_URL}/labels/emails/:emailId/:labelId`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  http.get(`${BASE_URL}/labels/:labelId/emails`, ({ request }) => {
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')

    const response: EmailListResponse = {
      items: mockEmails.slice(0, 1),
      totalCount: 1,
      unreadCount: 1,
      page,
      pageSize,
    }

    return HttpResponse.json(response)
  }),

  // Filter endpoints
  http.get(`${BASE_URL}/filters`, () => {
    return HttpResponse.json(mockFilters)
  }),

  http.post(`${BASE_URL}/filters`, async ({ request }) => {
    const body = (await request.json()) as Record<string, unknown>
    return HttpResponse.json(createMockFilter(body))
  }),

  http.put(`${BASE_URL}/filters/:id`, async ({ params, request }) => {
    const { id } = params
    const body = (await request.json()) as Record<string, unknown>
    const filter = mockFilters.find((f) => f.id === id)
    if (!filter) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json({ ...filter, ...body })
  }),

  http.delete(`${BASE_URL}/filters/:id`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  http.post(`${BASE_URL}/filters/:id/test`, () => {
    return HttpResponse.json({ matchCount: 5, matchedEmailIds: ['1', '2', '3', '4', '5'] })
  }),

  // Preferences endpoints
  http.get(`${BASE_URL}/preferences`, () => {
    return HttpResponse.json(createMockPreferences())
  }),

  http.put(`${BASE_URL}/preferences`, async ({ request }) => {
    const body = (await request.json()) as Record<string, unknown>
    return HttpResponse.json(createMockPreferences(body))
  }),
]
