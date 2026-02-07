import { AuthProvider as OidcAuthProvider } from 'react-oidc-context';
import type { AuthProviderProps } from 'react-oidc-context';
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
      window.location.href = '/';
    },
  };

  // Debug: Log config in development
  if (import.meta.env.DEV) {
    console.log('🔐 OIDC Configuration:', {
      authority: oidcConfig.authority,
      client_id: oidcConfig.client_id,
      redirect_uri: oidcConfig.redirect_uri,
      scope: oidcConfig.scope,
    });
  }

  return <OidcAuthProvider {...oidcConfig}>{children}</OidcAuthProvider>;
}
