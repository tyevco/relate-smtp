import * as SecureStore from "expo-secure-store";
import {
  extractDomain,
  isPinExpired,
  getPin,
  setPin,
  removePin,
  hasPinForDomain,
  validatePin,
  pinOnFirstUse,
  updatePinFingerprints,
  validateServerPin,
  CertificatePinError,
  CertificatePin,
} from "../certificate-pinning";

// Mock expo-secure-store
jest.mock("expo-secure-store", () => ({
  setItemAsync: jest.fn().mockResolvedValue(undefined),
  getItemAsync: jest.fn().mockResolvedValue(null),
  deleteItemAsync: jest.fn().mockResolvedValue(undefined),
  WHEN_UNLOCKED: "when_unlocked",
}));

// Mock Platform
jest.mock("react-native", () => ({
  Platform: {
    OS: "ios",
  },
}));

const mockGetItem = SecureStore.getItemAsync as jest.MockedFunction<
  typeof SecureStore.getItemAsync
>;
const mockSetItem = SecureStore.setItemAsync as jest.MockedFunction<
  typeof SecureStore.setItemAsync
>;
const mockDeleteItem = SecureStore.deleteItemAsync as jest.MockedFunction<
  typeof SecureStore.deleteItemAsync
>;

function makePinJson(overrides: Partial<CertificatePin> = {}): string {
  const pin: CertificatePin = {
    domain: "mail.example.com",
    sha256Fingerprints: ["abc123fingerprint"],
    includeSubdomains: false,
    createdAt: new Date().toISOString(),
    trustOnFirstUse: true,
    ...overrides,
  };
  return JSON.stringify(pin);
}

describe("extractDomain", () => {
  it("extracts hostname from HTTPS URL", () => {
    expect(extractDomain("https://mail.example.com")).toBe("mail.example.com");
  });

  it("extracts hostname from URL with port", () => {
    expect(extractDomain("https://mail.example.com:8443")).toBe(
      "mail.example.com"
    );
  });

  it("extracts hostname from URL with path", () => {
    expect(extractDomain("https://mail.example.com/api/test")).toBe(
      "mail.example.com"
    );
  });

  it("extracts hostname from HTTP URL", () => {
    expect(extractDomain("http://localhost:5000")).toBe("localhost");
  });

  it("handles plain domain as fallback", () => {
    expect(extractDomain("mail.example.com")).toBe("mail.example.com");
  });
});

describe("isPinExpired", () => {
  it("returns false when no expiration is set", () => {
    const pin: CertificatePin = {
      domain: "example.com",
      sha256Fingerprints: ["abc"],
      includeSubdomains: false,
      createdAt: new Date().toISOString(),
      trustOnFirstUse: true,
    };
    expect(isPinExpired(pin)).toBe(false);
  });

  it("returns false when expiration is in the future", () => {
    const pin: CertificatePin = {
      domain: "example.com",
      sha256Fingerprints: ["abc"],
      includeSubdomains: false,
      createdAt: new Date().toISOString(),
      expiresAt: new Date(Date.now() + 86400000).toISOString(),
      trustOnFirstUse: true,
    };
    expect(isPinExpired(pin)).toBe(false);
  });

  it("returns true when expiration is in the past", () => {
    const pin: CertificatePin = {
      domain: "example.com",
      sha256Fingerprints: ["abc"],
      includeSubdomains: false,
      createdAt: new Date().toISOString(),
      expiresAt: new Date(Date.now() - 86400000).toISOString(),
      trustOnFirstUse: true,
    };
    expect(isPinExpired(pin)).toBe(true);
  });
});

describe("CertificatePinError", () => {
  it("creates error with code and domain", () => {
    const error = new CertificatePinError(
      "PIN_MISMATCH",
      "example.com",
      "Certificate mismatch"
    );
    expect(error.code).toBe("PIN_MISMATCH");
    expect(error.domain).toBe("example.com");
    expect(error.message).toBe("Certificate mismatch");
    expect(error.name).toBe("CertificatePinError");
    expect(error).toBeInstanceOf(Error);
  });
});

describe("getPin", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("returns null when no pin is stored", async () => {
    mockGetItem.mockResolvedValueOnce(null);
    const result = await getPin("example.com");
    expect(result).toBeNull();
    expect(mockGetItem).toHaveBeenCalledWith("relate_cert_pin_example.com");
  });

  it("returns parsed pin when stored", async () => {
    const pinJson = makePinJson({ domain: "example.com" });
    mockGetItem.mockResolvedValueOnce(pinJson);

    const result = await getPin("example.com");
    expect(result).not.toBeNull();
    expect(result!.domain).toBe("example.com");
    expect(result!.sha256Fingerprints).toEqual(["abc123fingerprint"]);
  });

  it("returns null for invalid JSON", async () => {
    mockGetItem.mockResolvedValueOnce("not-json");
    const result = await getPin("example.com");
    expect(result).toBeNull();
  });
});

describe("setPin", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("stores pin with correct key and value", async () => {
    const pin: CertificatePin = {
      domain: "mail.example.com",
      sha256Fingerprints: ["fingerprint1", "fingerprint2"],
      includeSubdomains: false,
      createdAt: "2026-01-01T00:00:00.000Z",
      trustOnFirstUse: true,
    };

    await setPin(pin);

    expect(mockSetItem).toHaveBeenCalledWith(
      "relate_cert_pin_mail.example.com",
      JSON.stringify(pin),
      expect.objectContaining({
        keychainAccessible: SecureStore.WHEN_UNLOCKED,
      })
    );
  });
});

describe("removePin", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("deletes pin with correct key", async () => {
    await removePin("example.com");
    expect(mockDeleteItem).toHaveBeenCalledWith(
      "relate_cert_pin_example.com"
    );
  });
});

describe("hasPinForDomain", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("returns false when no pin exists", async () => {
    mockGetItem.mockResolvedValueOnce(null);
    expect(await hasPinForDomain("example.com")).toBe(false);
  });

  it("returns true when valid pin exists", async () => {
    mockGetItem.mockResolvedValueOnce(makePinJson());
    expect(await hasPinForDomain("example.com")).toBe(true);
  });

  it("returns false when pin is expired", async () => {
    mockGetItem.mockResolvedValueOnce(
      makePinJson({
        expiresAt: new Date(Date.now() - 86400000).toISOString(),
      })
    );
    expect(await hasPinForDomain("example.com")).toBe(false);
  });
});

describe("validatePin", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("returns valid with NO_PINS_CONFIGURED when no pin stored", async () => {
    mockGetItem.mockResolvedValueOnce(null);

    const result = await validatePin("example.com", ["fingerprint1"]);
    expect(result.valid).toBe(true);
    expect(result.error).toBe("NO_PINS_CONFIGURED");
  });

  it("returns invalid for expired pin", async () => {
    mockGetItem.mockResolvedValueOnce(
      makePinJson({
        expiresAt: new Date(Date.now() - 86400000).toISOString(),
      })
    );

    const result = await validatePin("example.com", ["abc123fingerprint"]);
    expect(result.valid).toBe(false);
    expect(result.error).toBe("PIN_EXPIRED");
  });

  it("returns valid when fingerprint matches", async () => {
    mockGetItem.mockResolvedValueOnce(
      makePinJson({ sha256Fingerprints: ["fp1", "fp2"] })
    );

    const result = await validatePin("example.com", ["fp2"]);
    expect(result.valid).toBe(true);
    expect(result.error).toBeUndefined();
  });

  it("returns invalid on PIN_MISMATCH", async () => {
    mockGetItem.mockResolvedValueOnce(
      makePinJson({ sha256Fingerprints: ["fp1"] })
    );

    const result = await validatePin("example.com", ["wrong-fp"]);
    expect(result.valid).toBe(false);
    expect(result.error).toBe("PIN_MISMATCH");
    expect(result.message).toContain("man-in-the-middle");
  });

  it("matches when any fingerprint in the list matches", async () => {
    mockGetItem.mockResolvedValueOnce(
      makePinJson({ sha256Fingerprints: ["fp1", "fp2", "fp3"] })
    );

    const result = await validatePin("example.com", ["other", "fp3"]);
    expect(result.valid).toBe(true);
  });
});

describe("pinOnFirstUse", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("creates a new pin when none exists", async () => {
    mockGetItem.mockResolvedValueOnce(null);

    const created = await pinOnFirstUse("example.com", ["fp1"]);
    expect(created).toBe(true);
    expect(mockSetItem).toHaveBeenCalledWith(
      "relate_cert_pin_example.com",
      expect.stringContaining('"domain":"example.com"'),
      expect.any(Object)
    );
  });

  it("does not overwrite an existing valid pin", async () => {
    mockGetItem.mockResolvedValueOnce(makePinJson());

    const created = await pinOnFirstUse("example.com", ["new-fp"]);
    expect(created).toBe(false);
    expect(mockSetItem).not.toHaveBeenCalled();
  });

  it("replaces an expired pin", async () => {
    mockGetItem.mockResolvedValueOnce(
      makePinJson({
        expiresAt: new Date(Date.now() - 86400000).toISOString(),
      })
    );

    const created = await pinOnFirstUse("example.com", ["new-fp"]);
    expect(created).toBe(true);
    expect(mockSetItem).toHaveBeenCalled();
  });

  it("sets expiration when expiresInDays is provided", async () => {
    mockGetItem.mockResolvedValueOnce(null);

    await pinOnFirstUse("example.com", ["fp1"], { expiresInDays: 30 });

    const setCall = mockSetItem.mock.calls[0];
    const storedPin = JSON.parse(setCall[1]) as CertificatePin;
    expect(storedPin.expiresAt).toBeDefined();

    const expiresAt = new Date(storedPin.expiresAt!);
    const expectedMin = new Date(Date.now() + 29 * 24 * 60 * 60 * 1000);
    const expectedMax = new Date(Date.now() + 31 * 24 * 60 * 60 * 1000);
    expect(expiresAt.getTime()).toBeGreaterThan(expectedMin.getTime());
    expect(expiresAt.getTime()).toBeLessThan(expectedMax.getTime());
  });

  it("marks pin as trustOnFirstUse", async () => {
    mockGetItem.mockResolvedValueOnce(null);

    await pinOnFirstUse("example.com", ["fp1"]);

    const setCall = mockSetItem.mock.calls[0];
    const storedPin = JSON.parse(setCall[1]) as CertificatePin;
    expect(storedPin.trustOnFirstUse).toBe(true);
  });
});

describe("updatePinFingerprints", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("updates fingerprints of existing pin", async () => {
    mockGetItem.mockResolvedValueOnce(
      makePinJson({ sha256Fingerprints: ["old-fp"] })
    );

    await updatePinFingerprints("example.com", ["new-fp1", "new-fp2"]);

    const setCall = mockSetItem.mock.calls[0];
    const storedPin = JSON.parse(setCall[1]) as CertificatePin;
    expect(storedPin.sha256Fingerprints).toEqual(["new-fp1", "new-fp2"]);
    expect(storedPin.trustOnFirstUse).toBe(false);
  });

  it("throws when no existing pin found", async () => {
    mockGetItem.mockResolvedValueOnce(null);

    await expect(
      updatePinFingerprints("example.com", ["fp"])
    ).rejects.toThrow("No existing pin for domain: example.com");
  });
});

describe("validateServerPin", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("extracts domain from URL and validates", async () => {
    mockGetItem.mockResolvedValueOnce(
      makePinJson({
        domain: "mail.example.com",
        sha256Fingerprints: ["server-fp"],
      })
    );

    const result = await validateServerPin("https://mail.example.com:8443", [
      "server-fp",
    ]);
    expect(result.valid).toBe(true);
    expect(result.domain).toBe("mail.example.com");
  });
});
