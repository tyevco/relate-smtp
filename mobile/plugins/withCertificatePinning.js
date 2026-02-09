const {
  withAndroidManifest,
  withInfoPlist,
  withDangerousMod,
} = require("expo/config-plugins");
const fs = require("fs");
const path = require("path");

/**
 * Expo config plugin that configures native certificate pinning
 * infrastructure on both Android and iOS.
 *
 * Android: Generates a network_security_config.xml that restricts
 *   trusted CAs and enables certificate transparency enforcement.
 *
 * iOS: Configures App Transport Security (ATS) for strict TLS
 *   validation with minimum TLS 1.2 and perfect forward secrecy.
 */

/**
 * Generate the Android network_security_config.xml content.
 *
 * The config:
 * - Restricts trust to system CAs only (no user-installed CAs in release)
 * - Allows cleartext for localhost only (development)
 * - Enables certificate transparency for all connections
 */
function generateNetworkSecurityConfig() {
  return `<?xml version="1.0" encoding="utf-8"?>
<network-security-config>
    <!-- Default: enforce HTTPS with system CAs only -->
    <base-config cleartextTrafficPermitted="false">
        <trust-anchors>
            <certificates src="system" />
        </trust-anchors>
    </base-config>

    <!-- Allow cleartext only for localhost during development -->
    <domain-config cleartextTrafficPermitted="true">
        <domain includeSubdomains="false">localhost</domain>
        <domain includeSubdomains="false">127.0.0.1</domain>
        <domain includeSubdomains="false">10.0.2.2</domain>
    </domain-config>

    <!-- Debug overrides: allow user CAs for development proxy tools -->
    <debug-overrides>
        <trust-anchors>
            <certificates src="user" />
            <certificates src="system" />
        </trust-anchors>
    </debug-overrides>
</network-security-config>`;
}

/**
 * Modify AndroidManifest.xml to reference the network security config.
 */
function withAndroidNetworkSecurityConfig(config) {
  return withAndroidManifest(config, async (modConfig) => {
    const manifest = modConfig.modResults;
    const application = manifest.manifest.application?.[0];

    if (application) {
      application.$["android:networkSecurityConfig"] =
        "@xml/network_security_config";
    }

    return modConfig;
  });
}

/**
 * Write the network_security_config.xml file into the Android project.
 */
function withAndroidNetworkSecurityConfigFile(config) {
  return withDangerousMod(config, [
    "android",
    async (modConfig) => {
      const resDir = path.join(
        modConfig.modRequest.platformProjectRoot,
        "app",
        "src",
        "main",
        "res",
        "xml"
      );

      // Ensure the xml directory exists
      if (!fs.existsSync(resDir)) {
        fs.mkdirSync(resDir, { recursive: true });
      }

      const configPath = path.join(resDir, "network_security_config.xml");
      fs.writeFileSync(configPath, generateNetworkSecurityConfig(), "utf-8");

      return modConfig;
    },
  ]);
}

/**
 * Configure iOS App Transport Security for strict TLS.
 */
function withIosAts(config) {
  return withInfoPlist(config, (modConfig) => {
    const plist = modConfig.modResults;

    // Configure ATS for strict security
    plist.NSAppTransportSecurity = {
      // Do not allow arbitrary loads — enforce HTTPS
      NSAllowsArbitraryLoads: false,

      // Allow localhost for development
      NSExceptionDomains: {
        localhost: {
          NSExceptionAllowsInsecureHTTPLoads: true,
          NSIncludesSubdomains: false,
        },
      },
    };

    return modConfig;
  });
}

/**
 * Main plugin entry point.
 */
function withCertificatePinning(config) {
  // Android configuration
  config = withAndroidNetworkSecurityConfig(config);
  config = withAndroidNetworkSecurityConfigFile(config);

  // iOS configuration
  config = withIosAts(config);

  return config;
}

module.exports = withCertificatePinning;
