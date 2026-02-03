export interface EmailListItem {
  id: string
  messageId: string
  fromAddress: string
  fromDisplayName: string | null
  subject: string
  receivedAt: string
  sizeBytes: number
  isRead: boolean
  attachmentCount: number
}

export interface EmailRecipient {
  id: string
  address: string
  displayName: string | null
  type: 'To' | 'Cc' | 'Bcc'
}

export interface EmailAttachment {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
}

export interface EmailDetail {
  id: string
  messageId: string
  fromAddress: string
  fromDisplayName: string | null
  subject: string
  textBody: string | null
  htmlBody: string | null
  receivedAt: string
  sizeBytes: number
  isRead: boolean
  recipients: EmailRecipient[]
  attachments: EmailAttachment[]
}

export interface EmailListResponse {
  items: EmailListItem[]
  totalCount: number
  unreadCount: number
  page: number
  pageSize: number
}

export interface EmailAddress {
  id: string
  address: string
  isVerified: boolean
  addedAt: string
}

export interface Profile {
  id: string
  email: string
  displayName: string | null
  createdAt: string
  lastLoginAt: string | null
  additionalAddresses: EmailAddress[]
}

export interface SmtpApiKey {
  id: string
  name: string
  createdAt: string
  lastUsedAt: string | null
  isActive: boolean
  scopes: string[]
}

export interface SmtpConnectionInfo {
  smtpServer: string
  smtpPort: number
  smtpSecurePort: number
  smtpEnabled: boolean
  pop3Server: string
  pop3Port: number
  pop3SecurePort: number
  pop3Enabled: boolean
  imapServer: string
  imapPort: number
  imapSecurePort: number
  imapEnabled: boolean
  username: string
  activeKeyCount: number
}

export interface CreateApiKeyRequest {
  name: string
  scopes?: string[]
}

export interface CreatedApiKey {
  id: string
  name: string
  apiKey: string
  scopes: string[]
  createdAt: string
}

export interface SmtpCredentials {
  connectionInfo: SmtpConnectionInfo
  keys: SmtpApiKey[]
}

export interface Label {
  id: string
  name: string
  color: string
  sortOrder: number
  createdAt: string
}

export interface CreateLabelRequest {
  name: string
  color: string
  sortOrder?: number
}

export interface UpdateLabelRequest {
  name?: string
  color?: string
  sortOrder?: number
}

export interface EmailFilter {
  id: string
  name: string
  isEnabled: boolean
  priority: number
  fromAddressContains?: string
  subjectContains?: string
  bodyContains?: string
  hasAttachments?: boolean
  markAsRead: boolean
  assignLabelId?: string
  assignLabelName?: string
  assignLabelColor?: string
  delete: boolean
  createdAt: string
  lastAppliedAt?: string
  timesApplied: number
}

export interface CreateEmailFilterRequest {
  name: string
  isEnabled?: boolean
  priority?: number
  fromAddressContains?: string
  subjectContains?: string
  bodyContains?: string
  hasAttachments?: boolean
  markAsRead?: boolean
  assignLabelId?: string
  delete?: boolean
}

export interface UpdateEmailFilterRequest {
  name?: string
  isEnabled?: boolean
  priority?: number
  fromAddressContains?: string
  subjectContains?: string
  bodyContains?: string
  hasAttachments?: boolean
  markAsRead?: boolean
  assignLabelId?: string
  delete?: boolean
}

export interface UserPreference {
  id: string
  userId: string
  theme: 'light' | 'dark' | 'system'
  displayDensity: 'compact' | 'comfortable' | 'spacious'
  emailsPerPage: number
  defaultSort: string
  showPreview: boolean
  groupByDate: boolean
  desktopNotifications: boolean
  emailDigest: boolean
  digestFrequency: 'daily' | 'weekly'
  digestTime: string
  updatedAt: string
}

export interface UpdateUserPreferenceRequest {
  theme?: 'light' | 'dark' | 'system'
  displayDensity?: 'compact' | 'comfortable' | 'spacious'
  emailsPerPage?: number
  defaultSort?: string
  showPreview?: boolean
  groupByDate?: boolean
  desktopNotifications?: boolean
  emailDigest?: boolean
  digestFrequency?: 'daily' | 'weekly'
  digestTime?: string
}
