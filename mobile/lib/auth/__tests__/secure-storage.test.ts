import * as SecureStore from 'expo-secure-store'
import { storeApiKey, getApiKey, deleteApiKey, isSecureStorageAvailable } from '../secure-storage'

// Mock expo-secure-store
jest.mock('expo-secure-store', () => ({
  setItemAsync: jest.fn().mockResolvedValue(undefined),
  getItemAsync: jest.fn().mockResolvedValue(null),
  deleteItemAsync: jest.fn().mockResolvedValue(undefined),
  isAvailableAsync: jest.fn().mockResolvedValue(true),
  WHEN_UNLOCKED: 'when_unlocked',
}))

// Mock Platform
jest.mock('react-native', () => ({
  Platform: {
    OS: 'ios', // Default to ios for tests
  },
}))

describe('secure-storage', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('storeApiKey', () => {
    it('stores API key with correct key prefix', async () => {
      await storeApiKey('account-123', 'test-api-key')

      expect(SecureStore.setItemAsync).toHaveBeenCalledWith(
        'relate_api_key_account-123',
        'test-api-key',
        expect.objectContaining({
          keychainAccessible: SecureStore.WHEN_UNLOCKED,
        })
      )
    })

    it('stores different keys for different accounts', async () => {
      await storeApiKey('account-1', 'key-1')
      await storeApiKey('account-2', 'key-2')

      expect(SecureStore.setItemAsync).toHaveBeenNthCalledWith(
        1,
        'relate_api_key_account-1',
        'key-1',
        expect.any(Object)
      )
      expect(SecureStore.setItemAsync).toHaveBeenNthCalledWith(
        2,
        'relate_api_key_account-2',
        'key-2',
        expect.any(Object)
      )
    })
  })

  describe('getApiKey', () => {
    it('retrieves API key with correct key prefix', async () => {
      await getApiKey('account-123')

      expect(SecureStore.getItemAsync).toHaveBeenCalledWith(
        'relate_api_key_account-123'
      )
    })

    it('returns the stored API key', async () => {
      ;(SecureStore.getItemAsync as jest.Mock).mockResolvedValueOnce('stored-key')

      const result = await getApiKey('account-123')

      expect(result).toBe('stored-key')
    })

    it('returns null if no key is stored', async () => {
      ;(SecureStore.getItemAsync as jest.Mock).mockResolvedValueOnce(null)

      const result = await getApiKey('account-123')

      expect(result).toBeNull()
    })
  })

  describe('deleteApiKey', () => {
    it('deletes API key with correct key prefix', async () => {
      await deleteApiKey('account-123')

      expect(SecureStore.deleteItemAsync).toHaveBeenCalledWith(
        'relate_api_key_account-123'
      )
    })
  })

  describe('isSecureStorageAvailable', () => {
    it('returns true when secure store is available', async () => {
      ;(SecureStore.isAvailableAsync as jest.Mock).mockResolvedValueOnce(true)

      const result = await isSecureStorageAvailable()

      expect(result).toBe(true)
    })

    it('returns false when secure store is not available', async () => {
      ;(SecureStore.isAvailableAsync as jest.Mock).mockResolvedValueOnce(false)

      const result = await isSecureStorageAvailable()

      expect(result).toBe(false)
    })
  })
})
