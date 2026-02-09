import * as LocalAuthentication from 'expo-local-authentication'
import { Platform } from 'react-native'
import {
  isBiometricAvailable,
  getBiometricType,
  authenticateWithBiometrics,
  useBiometricStore,
  useBiometricEnabled,
} from '../biometric'
import { renderHook, act } from '@testing-library/react-native'

// Mock expo-local-authentication
jest.mock('expo-local-authentication', () => ({
  hasHardwareAsync: jest.fn().mockResolvedValue(true),
  isEnrolledAsync: jest.fn().mockResolvedValue(true),
  supportedAuthenticationTypesAsync: jest.fn().mockResolvedValue([1]),
  authenticateAsync: jest.fn().mockResolvedValue({ success: true }),
  AuthenticationType: {
    FINGERPRINT: 1,
    FACIAL_RECOGNITION: 2,
    IRIS: 3,
  },
}))

// Mock Platform
jest.mock('react-native', () => ({
  Platform: {
    OS: 'ios',
  },
}))

describe('biometric', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    useBiometricStore.setState({ enabled: false })
  })

  describe('isBiometricAvailable', () => {
    it('returns true when hardware is available and enrolled', async () => {
      ;(LocalAuthentication.hasHardwareAsync as jest.Mock).mockResolvedValueOnce(true)
      ;(LocalAuthentication.isEnrolledAsync as jest.Mock).mockResolvedValueOnce(true)

      const result = await isBiometricAvailable()
      expect(result).toBe(true)
    })

    it('returns false when hardware is not available', async () => {
      ;(LocalAuthentication.hasHardwareAsync as jest.Mock).mockResolvedValueOnce(false)

      const result = await isBiometricAvailable()
      expect(result).toBe(false)
    })

    it('returns false when not enrolled', async () => {
      ;(LocalAuthentication.hasHardwareAsync as jest.Mock).mockResolvedValueOnce(true)
      ;(LocalAuthentication.isEnrolledAsync as jest.Mock).mockResolvedValueOnce(false)

      const result = await isBiometricAvailable()
      expect(result).toBe(false)
    })

    it('returns false on web platform', async () => {
      const originalOS = Platform.OS
      ;(Platform as any).OS = 'web'

      const result = await isBiometricAvailable()
      expect(result).toBe(false)

      ;(Platform as any).OS = originalOS
    })
  })

  describe('getBiometricType', () => {
    it('returns "Touch ID" for fingerprint on iOS', async () => {
      ;(LocalAuthentication.supportedAuthenticationTypesAsync as jest.Mock)
        .mockResolvedValueOnce([LocalAuthentication.AuthenticationType.FINGERPRINT])

      const result = await getBiometricType()
      expect(result).toBe('Touch ID')
    })

    it('returns "Face ID" for facial recognition on iOS', async () => {
      ;(LocalAuthentication.supportedAuthenticationTypesAsync as jest.Mock)
        .mockResolvedValueOnce([LocalAuthentication.AuthenticationType.FACIAL_RECOGNITION])

      const result = await getBiometricType()
      expect(result).toBe('Face ID')
    })

    it('returns "Fingerprint" on Android', async () => {
      const originalOS = Platform.OS
      ;(Platform as any).OS = 'android'
      ;(LocalAuthentication.supportedAuthenticationTypesAsync as jest.Mock)
        .mockResolvedValueOnce([LocalAuthentication.AuthenticationType.FINGERPRINT])

      const result = await getBiometricType()
      expect(result).toBe('Fingerprint')

      ;(Platform as any).OS = originalOS
    })

    it('returns "Face Recognition" on Android', async () => {
      const originalOS = Platform.OS
      ;(Platform as any).OS = 'android'
      ;(LocalAuthentication.supportedAuthenticationTypesAsync as jest.Mock)
        .mockResolvedValueOnce([LocalAuthentication.AuthenticationType.FACIAL_RECOGNITION])

      const result = await getBiometricType()
      expect(result).toBe('Face Recognition')

      ;(Platform as any).OS = originalOS
    })

    it('returns null on web platform', async () => {
      const originalOS = Platform.OS
      ;(Platform as any).OS = 'web'

      const result = await getBiometricType()
      expect(result).toBeNull()

      ;(Platform as any).OS = originalOS
    })

    it('returns null when no types are supported', async () => {
      ;(LocalAuthentication.supportedAuthenticationTypesAsync as jest.Mock)
        .mockResolvedValueOnce([])

      const result = await getBiometricType()
      expect(result).toBeNull()
    })

    it('returns "Iris" for iris authentication', async () => {
      ;(LocalAuthentication.supportedAuthenticationTypesAsync as jest.Mock)
        .mockResolvedValueOnce([LocalAuthentication.AuthenticationType.IRIS])

      const result = await getBiometricType()
      expect(result).toBe('Iris')
    })
  })

  describe('authenticateWithBiometrics', () => {
    it('returns true on successful authentication', async () => {
      ;(LocalAuthentication.authenticateAsync as jest.Mock)
        .mockResolvedValueOnce({ success: true })

      const result = await authenticateWithBiometrics()
      expect(result).toBe(true)
    })

    it('returns false on failed authentication', async () => {
      ;(LocalAuthentication.authenticateAsync as jest.Mock)
        .mockResolvedValueOnce({ success: false, error: 'user_cancel' })

      const result = await authenticateWithBiometrics()
      expect(result).toBe(false)
    })

    it('passes the prompt message to authenticateAsync', async () => {
      ;(LocalAuthentication.authenticateAsync as jest.Mock)
        .mockResolvedValueOnce({ success: true })

      await authenticateWithBiometrics('Custom message')

      expect(LocalAuthentication.authenticateAsync).toHaveBeenCalledWith(
        expect.objectContaining({
          promptMessage: 'Custom message',
        })
      )
    })

    it('uses default prompt message', async () => {
      ;(LocalAuthentication.authenticateAsync as jest.Mock)
        .mockResolvedValueOnce({ success: true })

      await authenticateWithBiometrics()

      expect(LocalAuthentication.authenticateAsync).toHaveBeenCalledWith(
        expect.objectContaining({
          promptMessage: 'Authenticate to access Relate Mail',
        })
      )
    })

    it('allows device passcode fallback', async () => {
      ;(LocalAuthentication.authenticateAsync as jest.Mock)
        .mockResolvedValueOnce({ success: true })

      await authenticateWithBiometrics()

      expect(LocalAuthentication.authenticateAsync).toHaveBeenCalledWith(
        expect.objectContaining({
          disableDeviceFallback: false,
        })
      )
    })
  })

  describe('useBiometricStore', () => {
    it('defaults to disabled', () => {
      const state = useBiometricStore.getState()
      expect(state.enabled).toBe(false)
    })

    it('can enable biometric', () => {
      act(() => {
        useBiometricStore.getState().setEnabled(true)
      })

      const state = useBiometricStore.getState()
      expect(state.enabled).toBe(true)
    })

    it('can disable biometric', () => {
      act(() => {
        useBiometricStore.getState().setEnabled(true)
        useBiometricStore.getState().setEnabled(false)
      })

      const state = useBiometricStore.getState()
      expect(state.enabled).toBe(false)
    })
  })

  describe('useBiometricEnabled hook', () => {
    it('returns false when disabled', () => {
      const { result } = renderHook(() => useBiometricEnabled())
      expect(result.current).toBe(false)
    })

    it('returns true when enabled', () => {
      act(() => {
        useBiometricStore.getState().setEnabled(true)
      })

      const { result } = renderHook(() => useBiometricEnabled())
      expect(result.current).toBe(true)
    })
  })
})
