import { AuthProvider as OidcAuthProvider } from 'react-oidc-context';
import type { AuthProviderProps } from 'react-oidc-context';
import { WebStorageStateStore } from 'oidc-client-ts';

const oidcConfig: AuthProviderProps = {
  authority: import.meta.env.VITE_OIDC_AUTHORITY,
  client_id: import.meta.env.VITE_OIDC_CLIENT_ID,
  redirect_uri: import.meta.env.VITE_OIDC_REDIRECT_URI || window.location.origin,

  // Use 'code' flow with PKCE for SPAs (no client secret needed)
  response_type: 'code',

  // Standard OIDC scopes
  scope: import.meta.env.VITE_OIDC_SCOPE || 'openid profile email',

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
    console.log('✅ Sign in callback completed');
    // Remove code and state from URL
    window.history.replaceState({}, document.title, window.location.pathname);
  },

  onSignoutCallback: () => {
    console.log('✅ Sign out callback completed');
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

export function AuthProvider({ children }: { children: React.ReactNode }) {
  // If no OIDC authority configured, render without auth
  const authority = import.meta.env.VITE_OIDC_AUTHORITY;
  if (!authority) {
    console.warn('⚠️ No OIDC authority configured, running in development mode without authentication');
    return <>{children}</>;
  }

  return <OidcAuthProvider {...oidcConfig}>{children}</OidcAuthProvider>;
}
