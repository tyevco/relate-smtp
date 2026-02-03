// Jest setup for React Native with Expo

// Mock react-native-css-interop (NativeWind's runtime)
jest.mock('react-native-css-interop', () => ({
  cssInterop: jest.fn((component) => component),
  remapProps: jest.fn((component) => component),
  useColorScheme: jest.fn(() => ({ colorScheme: 'light', setColorScheme: jest.fn(), toggleColorScheme: jest.fn() })),
  useUnstableNativeVariable: jest.fn(() => undefined),
  vars: jest.fn(() => ({})),
  createInteropElement: jest.fn((component) => component),
}))

// Mock nativewind
jest.mock('nativewind', () => ({
  styled: (component) => component,
  useColorScheme: () => ({ colorScheme: 'light', setColorScheme: jest.fn() }),
  cssInterop: jest.fn((component) => component),
  remapProps: jest.fn((component) => component),
}))

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
    name: 'relate-mail-mobile',
    slug: 'relate-mail-mobile',
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
  getRandomBytes: jest.fn().mockReturnValue(new Uint8Array(32).fill(65)),
  getRandomBytesAsync: jest.fn().mockResolvedValue(new Uint8Array(32).fill(65)),
  digestStringAsync: jest.fn().mockResolvedValue('mock_hash_base64'),
  CryptoDigestAlgorithm: {
    SHA256: 'SHA-256',
  },
  CryptoEncoding: {
    BASE64: 'base64',
  },
}))

// Mock expo-auth-session
jest.mock('expo-auth-session', () => ({
  useAuthRequest: jest.fn().mockReturnValue([null, null, jest.fn()]),
  makeRedirectUri: jest.fn().mockReturnValue('app://redirect'),
  fetchDiscoveryAsync: jest.fn(),
  exchangeCodeAsync: jest.fn(),
  AuthRequest: jest.fn().mockImplementation(() => ({
    promptAsync: jest.fn(),
  })),
  ResponseType: {
    Code: 'code',
    Token: 'token',
  },
  CodeChallengeMethod: {
    S256: 'S256',
  },
}))

// Mock expo-web-browser
jest.mock('expo-web-browser', () => ({
  openBrowserAsync: jest.fn().mockResolvedValue({ type: 'success' }),
  maybeCompleteAuthSession: jest.fn(),
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

// Mock lucide-react-native icons
jest.mock('lucide-react-native', () => {
  const { View } = require('react-native')
  return new Proxy({}, {
    get: () => View,
  })
})

// Mock fetch globally if not already mocked
if (!global.fetch) {
  global.fetch = jest.fn().mockImplementation(() =>
    Promise.resolve({
      ok: true,
      status: 200,
      json: () => Promise.resolve({}),
      text: () => Promise.resolve(''),
    })
  )
}

// Silence console warnings in tests
const originalWarn = console.warn
const originalError = console.error
beforeAll(() => {
  console.warn = (...args) => {
    if (
      args[0]?.includes?.('Animated') ||
      args[0]?.includes?.('NativeEventEmitter') ||
      args[0]?.includes?.('componentWillReceiveProps') ||
      args[0]?.includes?.('componentWillMount')
    ) {
      return
    }
    originalWarn.apply(console, args)
  }
  console.error = (...args) => {
    if (
      args[0]?.includes?.('Warning: ReactDOM.render') ||
      args[0]?.includes?.('Warning: An update to')
    ) {
      return
    }
    originalError.apply(console, args)
  }
})

afterAll(() => {
  console.warn = originalWarn
  console.error = originalError
})
