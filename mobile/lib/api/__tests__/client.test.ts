import { ApiError, getActiveApiClient, getApiClientForAccount, createTempApiClient, createPublicApiClient } from '../client'
import { useAccountStore } from '../../auth/account-store'
import { getApiKey } from '../../auth/secure-storage'

// Mock secure-storage
jest.mock('../../auth/secure-storage', () => ({
  getApiKey: jest.fn(),
}))

// Mock fetch
global.fetch = jest.fn()

const mockFetch = global.fetch as jest.MockedFunction<typeof fetch>

describe('ApiError', () => {
  it('creates error with status and message', () => {
    const error = new ApiError(404, 'Not Found')

    expect(error.status).toBe(404)
    expect(error.message).toBe('Not Found')
    expect(error.name).toBe('ApiError')
  })

  it('is an instance of Error', () => {
    const error = new ApiError(500, 'Server Error')

    expect(error).toBeInstanceOf(Error)
  })
})

describe('createTempApiClient', () => {
  beforeEach(() => {
    mockFetch.mockReset()
  })

  it('creates client with correct base URL', () => {
    const client = createTempApiClient('https://api.example.com', 'jwt-token')

    expect(client.baseUrl).toBe('https://api.example.com')
  })

  it('includes JWT token as Bearer authorization', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ data: 'test' }),
    } as Response)

    const client = createTempApiClient('https://api.example.com', 'jwt-token')
    await client.get('/test')

    expect(mockFetch).toHaveBeenCalledWith(
      'https://api.example.com/api/test',
      expect.objectContaining({
        headers: expect.objectContaining({
          'Authorization': 'Bearer jwt-token',
        }),
      })
    )
  })
})

describe('createPublicApiClient', () => {
  beforeEach(() => {
    mockFetch.mockReset()
  })

  it('creates client without authorization', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ data: 'test' }),
    } as Response)

    const client = createPublicApiClient('https://api.example.com')
    await client.get('/discovery')

    expect(mockFetch).toHaveBeenCalledWith(
      'https://api.example.com/api/discovery',
      expect.objectContaining({
        headers: expect.not.objectContaining({
          'Authorization': expect.any(String),
        }),
      })
    )
  })
})

describe('API client methods', () => {
  beforeEach(() => {
    mockFetch.mockReset()
  })

  describe('get', () => {
    it('makes GET request to correct endpoint', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ id: 1 }),
      } as Response)

      const client = createPublicApiClient('https://api.example.com')
      await client.get('/users/1')

      expect(mockFetch).toHaveBeenCalledWith(
        'https://api.example.com/api/users/1',
        expect.objectContaining({
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
          }),
        })
      )
    })

    it('returns parsed JSON response', async () => {
      const responseData = { id: 1, name: 'Test' }
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(responseData),
      } as Response)

      const client = createPublicApiClient('https://api.example.com')
      const result = await client.get('/test')

      expect(result).toEqual(responseData)
    })
  })

  describe('post', () => {
    it('makes POST request with JSON body', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 201,
        json: () => Promise.resolve({ id: 1 }),
      } as Response)

      const client = createPublicApiClient('https://api.example.com')
      await client.post('/users', { name: 'Test' })

      expect(mockFetch).toHaveBeenCalledWith(
        'https://api.example.com/api/users',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ name: 'Test' }),
        })
      )
    })
  })

  describe('put', () => {
    it('makes PUT request with JSON body', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ id: 1 }),
      } as Response)

      const client = createPublicApiClient('https://api.example.com')
      await client.put('/users/1', { name: 'Updated' })

      expect(mockFetch).toHaveBeenCalledWith(
        'https://api.example.com/api/users/1',
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify({ name: 'Updated' }),
        })
      )
    })
  })

  describe('patch', () => {
    it('makes PATCH request with JSON body', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ id: 1 }),
      } as Response)

      const client = createPublicApiClient('https://api.example.com')
      await client.patch('/users/1', { isActive: true })

      expect(mockFetch).toHaveBeenCalledWith(
        'https://api.example.com/api/users/1',
        expect.objectContaining({
          method: 'PATCH',
          body: JSON.stringify({ isActive: true }),
        })
      )
    })
  })

  describe('delete', () => {
    it('makes DELETE request', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 204,
      } as Response)

      const client = createPublicApiClient('https://api.example.com')
      await client.delete('/users/1')

      expect(mockFetch).toHaveBeenCalledWith(
        'https://api.example.com/api/users/1',
        expect.objectContaining({
          method: 'DELETE',
        })
      )
    })
  })

  describe('error handling', () => {
    it('throws ApiError for non-OK response', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        statusText: 'Not Found',
        text: () => Promise.resolve('Resource not found'),
      } as Response)

      const client = createPublicApiClient('https://api.example.com')

      await expect(client.get('/notfound')).rejects.toThrow(ApiError)
    })

    it('includes status code in error', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 403,
        statusText: 'Forbidden',
        text: () => Promise.resolve('Access denied'),
      } as Response)

      const client = createPublicApiClient('https://api.example.com')

      try {
        await client.get('/forbidden')
        fail('Should have thrown')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        expect((error as ApiError).status).toBe(403)
        expect((error as ApiError).message).toBe('Access denied')
      }
    })

    it('returns undefined for 204 status', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 204,
      } as Response)

      const client = createPublicApiClient('https://api.example.com')
      const result = await client.delete('/users/1')

      expect(result).toBeUndefined()
    })
  })
})

describe('getActiveApiClient', () => {
  beforeEach(() => {
    mockFetch.mockReset()
    ;(getApiKey as jest.Mock).mockReset()
    useAccountStore.setState({
      accounts: [],
      activeAccountId: null,
    })
  })

  it('throws error when no active account', async () => {
    await expect(getActiveApiClient()).rejects.toThrow('No active account')
  })

  it('throws error when API key not found', async () => {
    useAccountStore.setState({
      accounts: [{
        id: 'test-1',
        displayName: 'Test',
        serverUrl: 'https://example.com',
        userEmail: 'test@example.com',
        apiKeyId: 'key-1',
        scopes: ['smtp'],
        createdAt: new Date().toISOString(),
        lastUsedAt: new Date().toISOString(),
        isActive: true,
      }],
      activeAccountId: 'test-1',
    })
    ;(getApiKey as jest.Mock).mockResolvedValueOnce(null)

    await expect(getActiveApiClient()).rejects.toThrow('API key not found')
  })

  it('returns configured client with API key', async () => {
    useAccountStore.setState({
      accounts: [{
        id: 'test-1',
        displayName: 'Test',
        serverUrl: 'https://example.com',
        userEmail: 'test@example.com',
        apiKeyId: 'key-1',
        scopes: ['smtp'],
        createdAt: new Date().toISOString(),
        lastUsedAt: new Date().toISOString(),
        isActive: true,
      }],
      activeAccountId: 'test-1',
    })
    ;(getApiKey as jest.Mock).mockResolvedValueOnce('stored-api-key')

    const client = await getActiveApiClient()

    expect(client.baseUrl).toBe('https://example.com')
  })
})

describe('getApiClientForAccount', () => {
  beforeEach(() => {
    mockFetch.mockReset()
    ;(getApiKey as jest.Mock).mockReset()
    useAccountStore.setState({
      accounts: [],
      activeAccountId: null,
    })
  })

  it('throws error when account not found', async () => {
    await expect(getApiClientForAccount('nonexistent')).rejects.toThrow('Account not found')
  })

  it('throws error when API key not found', async () => {
    useAccountStore.setState({
      accounts: [{
        id: 'test-1',
        displayName: 'Test',
        serverUrl: 'https://example.com',
        userEmail: 'test@example.com',
        apiKeyId: 'key-1',
        scopes: ['smtp'],
        createdAt: new Date().toISOString(),
        lastUsedAt: new Date().toISOString(),
        isActive: true,
      }],
      activeAccountId: 'test-1',
    })
    ;(getApiKey as jest.Mock).mockResolvedValueOnce(null)

    await expect(getApiClientForAccount('test-1')).rejects.toThrow('API key not found')
  })

  it('returns configured client for specific account', async () => {
    useAccountStore.setState({
      accounts: [{
        id: 'test-1',
        displayName: 'Test',
        serverUrl: 'https://server1.example.com',
        userEmail: 'test@example.com',
        apiKeyId: 'key-1',
        scopes: ['smtp'],
        createdAt: new Date().toISOString(),
        lastUsedAt: new Date().toISOString(),
        isActive: true,
      }],
      activeAccountId: 'test-1',
    })
    ;(getApiKey as jest.Mock).mockResolvedValueOnce('api-key-1')

    const client = await getApiClientForAccount('test-1')

    expect(client.baseUrl).toBe('https://server1.example.com')
  })
})
