import { AuthProvider as OidcAuthProvider, type AuthProviderProps } from 'react-oidc-context';
import { WebStorageStateStore } from 'oidc-client-ts';
import { getConfig } from '../config';

export function AuthProvider({ children }: { children: React.ReactNode }) {
  // Get runtime configuration
  const config = getConfig();

  // If no OIDC authority configured, render without auth
  if (!config.oidcAuthority) {
    console.warn('⚠️ No OIDC authority configured, running in development mode without authentication');
    return <>{children}</>;
  }

  const oidcConfig: AuthProviderProps = {
    authority: config.oidcAuthority,
    client_id: config.oidcClientId,
    redirect_uri: config.oidcRedirectUri || window.location.origin,

    // Use 'code' flow with PKCE for SPAs (no client secret needed)
    response_type: 'code',

    // Standard OIDC scopes
    scope: config.oidcScope || 'openid profile email',

    // Don't send client_secret or client_assertion (this is a public client)
    client_authentication: undefined,

    // Store auth state in sessionStorage
    userStore: new WebStorageStateStore({ store: window.sessionStorage }),

    // Auto-renew tokens
    automaticSilentRenew: true,

    // Metadata discovery
    loadUserInfo: true,

    // Callback handlers
    onSigninCallback: () => {
      // Remove code and state from URL
      window.history.replaceState({}, document.title, window.location.pathname);
    },

    onSignoutCallback: () => {
      // Redirect to home page after signout
      // eslint-disable-next-line react-hooks/immutability -- intentional redirect after signout
      window.location.href = '/';
    },
  };

  // Error handler for OIDC failures
  const handleSigninError = (error: Error) => {
    console.error('OIDC sign-in error:', error);
    // Display user-friendly message based on error type
    const message = error.message.includes('network')
      ? 'Network error during authentication. Please check your connection.'
      : error.message.includes('expired')
      ? 'Your session has expired. Please sign in again.'
      : 'Authentication failed. Please try again.';
    console.error(message);
  };

  const oidcConfigWithHandlers = {
    ...oidcConfig,
    onSigninError: handleSigninError,
  };

  // Debug: Log config in development (using console.debug which is typically filtered out)
  if (import.meta.env.DEV) {
    // eslint-disable-next-line no-console
    console.debug('OIDC Configuration:', {
      authority: oidcConfig.authority,
      client_id: oidcConfig.client_id,
      redirect_uri: oidcConfig.redirect_uri,
      scope: oidcConfig.scope,
    });
  }

  return <OidcAuthProvider {...oidcConfigWithHandlers}>{children}</OidcAuthProvider>;
}
