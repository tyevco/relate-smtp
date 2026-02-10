import { logger } from './lib/logger'

// Runtime configuration loaded from /config.json
export interface AppConfig {
  oidcAuthority: string
  oidcClientId: string
  oidcRedirectUri: string
  oidcScope: string
}

/**
 * Validate that a redirect URI uses HTTPS in production.
 * Allows HTTP only for localhost during development.
 */
function validateRedirectUri(uri: string): string {
  const url = new URL(uri)
  const isLocalhost = url.hostname === 'localhost' || url.hostname === '127.0.0.1'
  const isHttps = url.protocol === 'https:'

  if (!isHttps && !isLocalhost) {
    throw new Error(
      `Security error: OIDC redirect URI must use HTTPS in production. ` +
      `Got: ${uri}. HTTP is only allowed for localhost.`
    )
  }

  return uri
}

let config: AppConfig | null = null

/**
 * Load configuration from /config.json at runtime
 */
export async function loadConfig(): Promise<AppConfig> {
  if (config) {
    return config
  }

  let response: Response | null = null
  let fetchError: Error | null = null

  const controller = new AbortController()
  const timeoutId = setTimeout(() => controller.abort(), 10_000)

  try {
    response = await fetch('/config/config.json', { signal: controller.signal })
  } catch (error) {
    fetchError = error instanceof Error ? error : new Error('Network error while fetching config')
    logger.error('Failed to fetch config.json (network error):', fetchError.message)
  } finally {
    clearTimeout(timeoutId)
  }

  if (response && response.ok) {
    try {
      const loadedConfig = await response.json() as AppConfig

      // Fallback to build-time env vars if config.json values are empty
      if (!loadedConfig.oidcAuthority) {
        loadedConfig.oidcAuthority = import.meta.env.VITE_OIDC_AUTHORITY || ''
      }
      if (!loadedConfig.oidcClientId) {
        loadedConfig.oidcClientId = import.meta.env.VITE_OIDC_CLIENT_ID || ''
      }
      if (!loadedConfig.oidcRedirectUri) {
        loadedConfig.oidcRedirectUri = validateRedirectUri(
          import.meta.env.VITE_OIDC_REDIRECT_URI || window.location.origin
        )
      } else {
        loadedConfig.oidcRedirectUri = validateRedirectUri(loadedConfig.oidcRedirectUri)
      }
      if (!loadedConfig.oidcScope) {
        loadedConfig.oidcScope = import.meta.env.VITE_OIDC_SCOPE || 'openid profile email'
      }

      config = loadedConfig
      return config
    } catch (parseError) {
      logger.error('Failed to parse config.json (invalid JSON):', parseError)
    }
  } else if (response && !response.ok) {
    logger.error(`Failed to load config.json: HTTP ${response.status} ${response.statusText}`)
  }

  // Fallback to build-time environment variables
  logger.warn('Using build-time environment variables for configuration')
  const fallbackRedirectUri = import.meta.env.VITE_OIDC_REDIRECT_URI || window.location.origin
  config = {
    oidcAuthority: import.meta.env.VITE_OIDC_AUTHORITY || '',
    oidcClientId: import.meta.env.VITE_OIDC_CLIENT_ID || '',
    oidcRedirectUri: validateRedirectUri(fallbackRedirectUri),
    oidcScope: import.meta.env.VITE_OIDC_SCOPE || 'openid profile email',
  }

  return config
}

/**
 * Get the loaded configuration
 * Must call loadConfig() first
 */
export function getConfig(): AppConfig {
  if (!config) {
    throw new Error('Configuration not loaded. Call loadConfig() first.')
  }
  return config
}
