// Define __DEV__ before importing react-native dependent modules
global.__DEV__ = true

// Skip @testing-library/jest-native for now - it has react-native dependencies
// that don't work well in node environment for pure unit tests

// Mock expo-secure-store
jest.mock('expo-secure-store', () => ({
  getItemAsync: jest.fn().mockResolvedValue(null),
  setItemAsync: jest.fn().mockResolvedValue(undefined),
  deleteItemAsync: jest.fn().mockResolvedValue(undefined),
}))

// Mock expo-router
jest.mock('expo-router', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
    back: jest.fn(),
    canGoBack: jest.fn().mockReturnValue(true),
    setParams: jest.fn(),
  }),
  useLocalSearchParams: () => ({}),
  useGlobalSearchParams: () => ({}),
  useSegments: () => [],
  usePathname: () => '/',
  Link: 'Link',
  Stack: {
    Screen: 'Screen',
  },
  Tabs: {
    Screen: 'Screen',
  },
  Redirect: 'Redirect',
}))

// Mock @react-native-async-storage/async-storage
jest.mock('@react-native-async-storage/async-storage', () =>
  require('@react-native-async-storage/async-storage/jest/async-storage-mock')
)

// Mock react-native-reanimated
jest.mock('react-native-reanimated', () => {
  const Reanimated = require('react-native-reanimated/mock')
  Reanimated.default.call = () => {}
  return Reanimated
})

// Mock expo-constants
jest.mock('expo-constants', () => ({
  expoConfig: {
    name: 'relate-smtp-mobile',
    slug: 'relate-smtp-mobile',
  },
}))

// Mock expo-linking
jest.mock('expo-linking', () => ({
  createURL: jest.fn().mockReturnValue('app://'),
  parse: jest.fn().mockReturnValue({ path: '/', queryParams: {} }),
}))

// Mock expo-crypto
jest.mock('expo-crypto', () => ({
  randomUUID: jest.fn().mockReturnValue('test-uuid'),
  getRandomBytesAsync: jest.fn().mockResolvedValue(new Uint8Array(16)),
}))

// Mock expo-auth-session
jest.mock('expo-auth-session', () => ({
  useAuthRequest: jest.fn().mockReturnValue([null, null, jest.fn()]),
  makeRedirectUri: jest.fn().mockReturnValue('app://redirect'),
  ResponseType: {
    Code: 'code',
    Token: 'token',
  },
}))

// Mock expo-web-browser
jest.mock('expo-web-browser', () => ({
  openBrowserAsync: jest.fn().mockResolvedValue({ type: 'success' }),
  maybeCompleteAuthSession: jest.fn(),
}))

// Mock nativewind
jest.mock('nativewind', () => ({
  styled: (component) => component,
  useColorScheme: () => ({ colorScheme: 'light', setColorScheme: jest.fn() }),
}))

// Mock @microsoft/signalr
jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn().mockImplementation(() => ({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    build: jest.fn().mockReturnValue({
      start: jest.fn().mockResolvedValue(undefined),
      stop: jest.fn().mockResolvedValue(undefined),
      on: jest.fn(),
      off: jest.fn(),
      state: 'Connected',
    }),
  })),
  HubConnectionState: {
    Disconnected: 'Disconnected',
    Connecting: 'Connecting',
    Connected: 'Connected',
    Disconnecting: 'Disconnecting',
    Reconnecting: 'Reconnecting',
  },
  LogLevel: {
    None: 0,
    Error: 1,
    Warning: 2,
    Information: 3,
    Debug: 4,
    Trace: 5,
  },
}))

// Mock fetch globally
global.fetch = jest.fn().mockImplementation(() =>
  Promise.resolve({
    ok: true,
    status: 200,
    json: () => Promise.resolve({}),
    text: () => Promise.resolve(''),
  })
)

// Silence console warnings in tests
const originalWarn = console.warn
beforeAll(() => {
  console.warn = (...args) => {
    if (
      args[0]?.includes?.('Animated') ||
      args[0]?.includes?.('NativeEventEmitter')
    ) {
      return
    }
    originalWarn.apply(console, args)
  }
})

afterAll(() => {
  console.warn = originalWarn
})
