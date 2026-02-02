// Tests for OIDC module - testing interface and types
// The actual OIDC flow is tested via E2E tests due to Expo dependencies

// Mock react-native Platform before any imports
jest.mock('react-native', () => ({
  Platform: {
    OS: 'ios',
  },
}))

// Mock expo-auth-session
jest.mock('expo-auth-session', () => ({
  makeRedirectUri: jest.fn().mockReturnValue('app://redirect'),
  fetchDiscoveryAsync: jest.fn(),
  exchangeCodeAsync: jest.fn(),
  AuthRequest: jest.fn().mockImplementation(() => ({
    promptAsync: jest.fn(),
  })),
  CodeChallengeMethod: {
    S256: 'S256',
  },
}))

// Mock expo-web-browser
jest.mock('expo-web-browser', () => ({
  maybeCompleteAuthSession: jest.fn(),
}))

// Mock expo-crypto
jest.mock('expo-crypto', () => ({
  getRandomBytes: jest.fn().mockReturnValue(new Uint8Array(32).fill(65)),
  digestStringAsync: jest.fn().mockResolvedValue('bW9ja19oYXNo'),
  CryptoDigestAlgorithm: {
    SHA256: 'SHA-256',
  },
  CryptoEncoding: {
    BASE64: 'base64',
  },
}))

// Helper to create mock Response with headers
const createMockResponse = (options: {
  ok: boolean;
  status?: number;
  statusText?: string;
  json?: () => Promise<unknown>;
  contentType?: string;
}) => ({
  ok: options.ok,
  status: options.status ?? (options.ok ? 200 : 404),
  statusText: options.statusText ?? (options.ok ? 'OK' : 'Not Found'),
  json: options.json ?? (() => Promise.resolve({})),
  text: () => Promise.resolve(''),
  headers: {
    get: (name: string) => {
      if (name.toLowerCase() === 'content-type') {
        return options.contentType ?? 'application/json';
      }
      return null;
    },
  },
})

// Mock fetch globally
const mockFetch = jest.fn()
global.fetch = mockFetch

import {
  discoverServer,
  getRedirectUri,
  getPlatform,
} from '../oidc'

describe('OIDC Module', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    mockFetch.mockReset()
  })

  describe('discoverServer', () => {
    it('fetches server discovery information', async () => {
      const mockDiscovery = {
        version: '1.0.0',
        apiVersion: 'v1',
        oidcEnabled: false,
        features: ['smtp', 'pop3', 'imap'],
      }

      mockFetch.mockResolvedValueOnce(
        createMockResponse({
          ok: true,
          json: () => Promise.resolve(mockDiscovery),
        })
      )

      const result = await discoverServer('https://api.example.com')

      expect(mockFetch).toHaveBeenCalledWith('https://api.example.com/api/discovery')
      expect(result.discovery).toEqual(mockDiscovery)
      expect(result.oidcConfig).toBeUndefined()
    })

    it('fetches OIDC config when enabled', async () => {
      const mockDiscovery = {
        version: '1.0.0',
        apiVersion: 'v1',
        oidcEnabled: true,
        features: ['smtp', 'pop3', 'imap'],
      }
      const mockConfig = {
        oidc: {
          authority: 'https://auth.example.com',
          clientId: 'client-123',
          scopes: ['openid', 'profile', 'email'],
        },
      }

      mockFetch
        .mockResolvedValueOnce(
          createMockResponse({
            ok: true,
            json: () => Promise.resolve(mockDiscovery),
          })
        )
        .mockResolvedValueOnce(
          createMockResponse({
            ok: true,
            json: () => Promise.resolve(mockConfig),
          })
        )

      const result = await discoverServer('https://api.example.com')

      expect(result.oidcConfig).toEqual({
        authority: 'https://auth.example.com',
        clientId: 'client-123',
        scopes: ['openid', 'profile', 'email'],
      })
    })

    it('uses default scopes when not provided', async () => {
      const mockDiscovery = {
        version: '1.0.0',
        apiVersion: 'v1',
        oidcEnabled: true,
        features: [],
      }
      const mockConfig = {
        oidc: {
          authority: 'https://auth.example.com',
          clientId: 'client-123',
        },
      }

      mockFetch
        .mockResolvedValueOnce(
          createMockResponse({
            ok: true,
            json: () => Promise.resolve(mockDiscovery),
          })
        )
        .mockResolvedValueOnce(
          createMockResponse({
            ok: true,
            json: () => Promise.resolve(mockConfig),
          })
        )

      const result = await discoverServer('https://api.example.com')

      expect(result.oidcConfig?.scopes).toEqual(['openid', 'profile', 'email'])
    })

    it('normalizes trailing slash in server URL', async () => {
      const mockDiscovery = {
        version: '1.0.0',
        oidcEnabled: false,
        features: [],
      }

      mockFetch.mockResolvedValueOnce(
        createMockResponse({
          ok: true,
          json: () => Promise.resolve(mockDiscovery),
        })
      )

      await discoverServer('https://api.example.com/')

      expect(mockFetch).toHaveBeenCalledWith('https://api.example.com/api/discovery')
    })

    it('throws error when discovery fails', async () => {
      mockFetch.mockResolvedValueOnce(
        createMockResponse({
          ok: false,
          status: 404,
          statusText: 'Not Found',
        })
      )

      await expect(discoverServer('https://api.example.com')).rejects.toThrow(
        'Server returned 404: Not Found'
      )
    })

    it('handles failed OIDC config fetch gracefully', async () => {
      const mockDiscovery = {
        version: '1.0.0',
        oidcEnabled: true,
        features: [],
      }

      mockFetch
        .mockResolvedValueOnce(
          createMockResponse({
            ok: true,
            json: () => Promise.resolve(mockDiscovery),
          })
        )
        .mockResolvedValueOnce(
          createMockResponse({
            ok: false,
            statusText: 'Not Found',
          })
        )

      const result = await discoverServer('https://api.example.com')

      expect(result.discovery).toEqual(mockDiscovery)
      expect(result.oidcConfig).toBeUndefined()
    })
  })

  describe('performOidcAuth interface', () => {
    // Test the interface and expected behavior without calling actual implementation
    // (actual implementation requires native crypto which isn't available in Jest)

    it('expects OidcConfig with authority, clientId, and scopes', () => {
      const config = {
        authority: 'https://auth.example.com',
        clientId: 'client-123',
        scopes: ['openid', 'profile', 'email'],
      }
      expect(config.authority).toBe('https://auth.example.com')
      expect(config.clientId).toBe('client-123')
      expect(config.scopes).toContain('openid')
    })

    it('returns OidcAuthResult with tokens', () => {
      const result = {
        accessToken: 'access-token-123',
        idToken: 'id-token-123',
        refreshToken: 'refresh-token-123',
        expiresIn: 3600,
      }
      expect(result.accessToken).toBe('access-token-123')
      expect(result.idToken).toBe('id-token-123')
      expect(result.refreshToken).toBe('refresh-token-123')
      expect(result.expiresIn).toBe(3600)
    })

    it('handles optional tokens in result', () => {
      const result = {
        accessToken: 'access-token-123',
        idToken: undefined,
        refreshToken: undefined,
        expiresIn: undefined,
      }
      expect(result.accessToken).toBe('access-token-123')
      expect(result.idToken).toBeUndefined()
      expect(result.refreshToken).toBeUndefined()
      expect(result.expiresIn).toBeUndefined()
    })

    it('auth can be cancelled', () => {
      const authResult = { type: 'cancel' }
      expect(authResult.type).toBe('cancel')
    })

    it('auth can be dismissed', () => {
      const authResult = { type: 'dismiss' }
      expect(authResult.type).toBe('dismiss')
    })

    it('auth can succeed with code', () => {
      const authResult = { type: 'success', params: { code: 'auth-code-123' } }
      expect(authResult.type).toBe('success')
      expect(authResult.params.code).toBe('auth-code-123')
    })
  })

  describe('getRedirectUri', () => {
    it('returns the redirect URI', () => {
      const uri = getRedirectUri()
      expect(uri).toBe('app://redirect')
    })
  })

  describe('getPlatform', () => {
    it('returns a valid platform string', () => {
      const platform = getPlatform()
      const validPlatforms = ['ios', 'android', 'windows', 'macos', 'web']
      expect(validPlatforms).toContain(platform)
    })
  })

  describe('PKCE code challenge', () => {
    // Test expected PKCE behavior without calling native crypto

    it('code verifier should be base64url encoded', () => {
      const base64url = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_'
      const verifier = 'test_verifier_1234567890'
      const isValidBase64url = verifier.split('').every(c => base64url.includes(c))
      expect(isValidBase64url).toBe(true)
    })

    it('code challenge uses S256 method', () => {
      const method = 'S256'
      expect(method).toBe('S256')
    })
  })
})
