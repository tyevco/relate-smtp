import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { api, apiRequest, ApiError } from './client'

describe('ApiError', () => {
  it('creates an error with status and message', () => {
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

describe('apiRequest', () => {
  const mockFetch = vi.fn()
  const originalFetch = global.fetch

  beforeEach(() => {
    global.fetch = mockFetch
    sessionStorage.clear()
    localStorage.clear()
    vi.clearAllMocks()
  })

  afterEach(() => {
    global.fetch = originalFetch
  })

  it('makes a GET request with correct URL', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ data: 'test' }),
    })

    await apiRequest('/test')

    expect(mockFetch).toHaveBeenCalledWith(
      '/api/test',
      expect.objectContaining({
        headers: expect.objectContaining({
          'Content-Type': 'application/json',
        }),
      })
    )
  })

  it('returns parsed JSON response', async () => {
    const testData = { id: 1, name: 'Test' }
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve(testData),
    })

    const result = await apiRequest('/test')
    expect(result).toEqual(testData)
  })

  it('returns undefined for 204 No Content', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 204,
    })

    const result = await apiRequest('/test')
    expect(result).toBeUndefined()
  })

  it('throws ApiError for non-OK response', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 404,
      statusText: 'Not Found',
      text: () => Promise.resolve('Resource not found'),
    })

    try {
      await apiRequest('/test')
      expect.fail('Expected ApiError to be thrown')
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError)
      expect((error as ApiError).status).toBe(404)
      expect((error as ApiError).message).toBe('Resource not found')
    }
  })

  it('uses statusText when no error message body', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      statusText: 'Internal Server Error',
      text: () => Promise.resolve(''),
    })

    try {
      await apiRequest('/test')
      expect.fail('Expected ApiError to be thrown')
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError)
      expect((error as ApiError).status).toBe(500)
      expect((error as ApiError).message).toBe('Internal Server Error')
    }
  })

  it('includes auth header when OIDC token is present in sessionStorage', async () => {
    const mockToken = {
      access_token: 'test-access-token',
    }
    sessionStorage.setItem('oidc.user:https://auth.example.com', JSON.stringify(mockToken))

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({}),
    })

    await apiRequest('/test')

    expect(mockFetch).toHaveBeenCalledWith(
      '/api/test',
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: 'Bearer test-access-token',
        }),
      })
    )
  })

  it('includes auth header when OIDC token is in localStorage', async () => {
    const mockToken = {
      access_token: 'local-access-token',
    }
    localStorage.setItem('oidc.user', JSON.stringify(mockToken))

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({}),
    })

    await apiRequest('/test')

    expect(mockFetch).toHaveBeenCalledWith(
      '/api/test',
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: 'Bearer local-access-token',
        }),
      })
    )
  })

  it('handles invalid JSON in storage gracefully', async () => {
    sessionStorage.setItem('oidc.user:https://auth.example.com', 'invalid-json')

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({}),
    })

    // Should not throw, should just skip auth header
    await expect(apiRequest('/test')).resolves.toEqual({})
  })

  it('merges custom headers with defaults', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({}),
    })

    await apiRequest('/test', {
      headers: {
        'X-Custom-Header': 'custom-value',
      },
    })

    expect(mockFetch).toHaveBeenCalledWith(
      '/api/test',
      expect.objectContaining({
        headers: expect.objectContaining({
          'Content-Type': 'application/json',
          'X-Custom-Header': 'custom-value',
        }),
      })
    )
  })
})

describe('api convenience methods', () => {
  const mockFetch = vi.fn()
  const originalFetch = global.fetch

  beforeEach(() => {
    global.fetch = mockFetch
    sessionStorage.clear()
    localStorage.clear()
    vi.clearAllMocks()
  })

  afterEach(() => {
    global.fetch = originalFetch
  })

  describe('api.get', () => {
    it('makes a GET request', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ data: 'test' }),
      })

      await api.get('/test')

      expect(mockFetch).toHaveBeenCalledWith(
        '/api/test',
        expect.objectContaining({
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
          }),
        })
      )
    })
  })

  describe('api.post', () => {
    it('makes a POST request with JSON body', async () => {
      const postData = { name: 'Test', value: 123 }
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 201,
        json: () => Promise.resolve({ id: 1, ...postData }),
      })

      await api.post('/test', postData)

      expect(mockFetch).toHaveBeenCalledWith(
        '/api/test',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(postData),
        })
      )
    })
  })

  describe('api.put', () => {
    it('makes a PUT request with JSON body', async () => {
      const putData = { name: 'Updated' }
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(putData),
      })

      await api.put('/test/1', putData)

      expect(mockFetch).toHaveBeenCalledWith(
        '/api/test/1',
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify(putData),
        })
      )
    })
  })

  describe('api.patch', () => {
    it('makes a PATCH request with JSON body', async () => {
      const patchData = { isRead: true }
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(patchData),
      })

      await api.patch('/test/1', patchData)

      expect(mockFetch).toHaveBeenCalledWith(
        '/api/test/1',
        expect.objectContaining({
          method: 'PATCH',
          body: JSON.stringify(patchData),
        })
      )
    })
  })

  describe('api.delete', () => {
    it('makes a DELETE request', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 204,
      })

      await api.delete('/test/1')

      expect(mockFetch).toHaveBeenCalledWith(
        '/api/test/1',
        expect.objectContaining({
          method: 'DELETE',
        })
      )
    })
  })

  describe('api.getHeaders', () => {
    it('returns headers with Content-Type', async () => {
      const headers = await api.getHeaders()
      expect(headers['Content-Type']).toBe('application/json')
    })

    it('includes auth header when token is present', async () => {
      const mockToken = {
        access_token: 'header-test-token',
      }
      sessionStorage.setItem('oidc.user:https://auth.example.com', JSON.stringify(mockToken))

      const headers = await api.getHeaders() as Record<string, string>
      expect(headers['Authorization']).toBe('Bearer header-test-token')
    })
  })

  describe('api.baseUrl', () => {
    it('returns the API base URL', () => {
      expect(api.baseUrl).toBe('/api')
    })
  })
})
