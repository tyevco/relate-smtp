import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";

const PIN_STORAGE_PREFIX = "relate_cert_pin_";

/**
 * Represents a stored certificate pin for a server domain.
 * Uses SHA-256 hashes of the Subject Public Key Info (SPKI).
 */
export interface CertificatePin {
  /** The domain this pin applies to (e.g., "mail.example.com") */
  domain: string;
  /** Base64-encoded SHA-256 hashes of the server's SPKI */
  sha256Fingerprints: string[];
  /** Whether to apply the pin to subdomains */
  includeSubdomains: boolean;
  /** ISO date string when this pin expires (optional) */
  expiresAt?: string;
  /** ISO date string when this pin was created */
  createdAt: string;
  /** Whether this pin was set via Trust-on-First-Use */
  trustOnFirstUse: boolean;
}

export type PinValidationErrorCode =
  | "PIN_MISMATCH"
  | "PIN_EXPIRED"
  | "NO_PINS_CONFIGURED";

/**
 * Result of a certificate pin validation check.
 */
export interface PinValidationResult {
  valid: boolean;
  domain: string;
  error?: PinValidationErrorCode;
  message?: string;
}

/**
 * Error thrown when certificate pin validation fails.
 */
export class CertificatePinError extends Error {
  constructor(
    public code: PinValidationErrorCode,
    public domain: string,
    message: string
  ) {
    super(message);
    this.name = "CertificatePinError";
  }
}

/**
 * Extract a domain from a server URL.
 * @param url - Full server URL (e.g., "https://mail.example.com:8443/path")
 * @returns The domain portion (e.g., "mail.example.com")
 */
export function extractDomain(url: string): string {
  try {
    const parsed = new URL(url);
    return parsed.hostname;
  } catch {
    // Fallback: strip protocol, port, and path manually
    return url
      .replace(/^https?:\/\//, "")
      .replace(/[:/].*$/, "")
      .toLowerCase();
  }
}

/**
 * Check whether a certificate pin has expired.
 */
export function isPinExpired(pin: CertificatePin): boolean {
  if (!pin.expiresAt) {
    return false;
  }
  return new Date(pin.expiresAt) < new Date();
}

/**
 * Build the secure storage key for a domain.
 */
function storageKey(domain: string): string {
  return `${PIN_STORAGE_PREFIX}${domain}`;
}

/**
 * Retrieve a stored certificate pin for a domain.
 * Returns null if no pin is stored.
 */
export async function getPin(domain: string): Promise<CertificatePin | null> {
  const key = storageKey(domain);

  let raw: string | null;
  if (Platform.OS === "web") {
    raw = typeof sessionStorage !== "undefined"
      ? sessionStorage.getItem(key)
      : null;
  } else {
    raw = await SecureStore.getItemAsync(key);
  }

  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as CertificatePin;
  } catch {
    return null;
  }
}

/**
 * Store a certificate pin for a domain.
 * On native platforms the pin is kept in the OS Keychain / Keystore.
 * On web it is stored in sessionStorage (cleared when the browser closes).
 */
export async function setPin(pin: CertificatePin): Promise<void> {
  const key = storageKey(pin.domain);
  const value = JSON.stringify(pin);

  if (Platform.OS === "web") {
    sessionStorage.setItem(key, value);
    return;
  }

  await SecureStore.setItemAsync(key, value, {
    keychainAccessible: SecureStore.WHEN_UNLOCKED,
  });
}

/**
 * Remove a stored certificate pin for a domain.
 */
export async function removePin(domain: string): Promise<void> {
  const key = storageKey(domain);

  if (Platform.OS === "web") {
    sessionStorage.removeItem(key);
    return;
  }

  await SecureStore.deleteItemAsync(key);
}

/**
 * Check whether a stored (non-expired) pin exists for the given domain.
 */
export async function hasPinForDomain(domain: string): Promise<boolean> {
  const pin = await getPin(domain);
  if (!pin) {
    return false;
  }
  return !isPinExpired(pin);
}

/**
 * Validate a set of certificate fingerprints against a stored pin.
 *
 * If no pin is stored for the domain the result is `valid: true` with
 * error code `NO_PINS_CONFIGURED` – callers can use this to trigger
 * the Trust-on-First-Use flow.
 *
 * @param domain - The server domain to validate
 * @param fingerprints - SHA-256 SPKI hashes reported by the server
 */
export async function validatePin(
  domain: string,
  fingerprints: string[]
): Promise<PinValidationResult> {
  const pin = await getPin(domain);

  if (!pin) {
    return {
      valid: true,
      domain,
      error: "NO_PINS_CONFIGURED",
      message: "No certificate pin stored for this domain",
    };
  }

  if (isPinExpired(pin)) {
    return {
      valid: false,
      domain,
      error: "PIN_EXPIRED",
      message: `Certificate pin for ${domain} has expired (${pin.expiresAt})`,
    };
  }

  // Check whether at least one presented fingerprint matches a stored pin
  const hasMatch = fingerprints.some((fp) =>
    pin.sha256Fingerprints.includes(fp)
  );

  if (!hasMatch) {
    return {
      valid: false,
      domain,
      error: "PIN_MISMATCH",
      message:
        `Certificate pin mismatch for ${domain}. ` +
        `The server's certificate has changed. This could indicate a ` +
        `man-in-the-middle attack or a legitimate certificate rotation.`,
    };
  }

  return { valid: true, domain };
}

/**
 * Pin a server on first use (TOFU).
 *
 * Creates a new pin entry with the provided fingerprints.  If a valid
 * (non-expired) pin already exists for the domain, this is a no-op and
 * returns `false`.
 *
 * @returns `true` if a new pin was created, `false` if one already existed.
 */
export async function pinOnFirstUse(
  domain: string,
  fingerprints: string[],
  options?: { includeSubdomains?: boolean; expiresInDays?: number }
): Promise<boolean> {
  const existing = await getPin(domain);
  if (existing && !isPinExpired(existing)) {
    return false;
  }

  const expiresAt = options?.expiresInDays
    ? new Date(
        Date.now() + options.expiresInDays * 24 * 60 * 60 * 1000
      ).toISOString()
    : undefined;

  await setPin({
    domain,
    sha256Fingerprints: fingerprints,
    includeSubdomains: options?.includeSubdomains ?? false,
    expiresAt,
    createdAt: new Date().toISOString(),
    trustOnFirstUse: true,
  });

  return true;
}

/**
 * Update the fingerprints of an existing pin (e.g. after the user
 * confirms a legitimate certificate rotation).
 */
export async function updatePinFingerprints(
  domain: string,
  fingerprints: string[]
): Promise<void> {
  const existing = await getPin(domain);
  if (!existing) {
    throw new Error(`No existing pin for domain: ${domain}`);
  }

  await setPin({
    ...existing,
    sha256Fingerprints: fingerprints,
    createdAt: new Date().toISOString(),
    trustOnFirstUse: false,
  });
}

/**
 * Validate a server URL against stored pins.
 *
 * Convenience wrapper that extracts the domain from the URL before
 * delegating to `validatePin`.
 */
export async function validateServerPin(
  serverUrl: string,
  fingerprints: string[]
): Promise<PinValidationResult> {
  const domain = extractDomain(serverUrl);
  return validatePin(domain, fingerprints);
}
