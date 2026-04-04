# TLS Configuration

The SMTP server supports TLS encryption on all three listener ports, with different modes depending on the port's purpose.

## TLS Modes

### Port 587 -- STARTTLS (Explicit TLS)

The client connects in plaintext and then optionally issues the `STARTTLS` command to upgrade the connection to TLS. This is the standard mechanism defined in RFC 3207.

**SMTP session flow:**
```
Client: (connects on port 587)
Server: 220 mail.example.com ESMTP
Client: EHLO client.example.com
Server: 250-mail.example.com
Server: 250-STARTTLS
Server: 250 OK
Client: STARTTLS
Server: 220 Ready to start TLS
        (TLS handshake occurs)
Client: EHLO client.example.com    (re-issue EHLO after TLS)
Client: AUTH PLAIN ...
```

The `AllowUnsecureAuthentication` option is enabled on this port, which means clients can authenticate before issuing STARTTLS. While this transmits credentials in plaintext (risky on untrusted networks), it is necessary for compatibility with some older mail clients. In practice, most modern clients will STARTTLS before authenticating.

### Port 465 -- Implicit TLS

TLS is negotiated immediately on connection -- the client initiates the TLS handshake as the first step, with no plaintext phase. This is conceptually similar to HTTPS.

**Session flow:**
```
Client: (connects on port 465, initiates TLS handshake)
        (TLS handshake completes)
Server: 220 mail.example.com ESMTP
Client: EHLO client.example.com
Client: AUTH PLAIN ...
```

This port **requires** a TLS certificate to be configured. If no certificate is provided and `SecurePort` is set to 0, the port is not activated.

### Port 25 -- Opportunistic STARTTLS

The MX endpoint supports STARTTLS when a TLS certificate is configured. Connecting servers can optionally upgrade to TLS, but it is not required. This follows the standard practice for server-to-server SMTP, where encryption is opportunistic rather than mandatory.

When a certificate is configured, the server advertises `STARTTLS` in its EHLO response on port 25, and connecting MTAs may choose to upgrade.

## Certificate Configuration

### Configuration Settings

| Setting | Description |
|---------|-------------|
| `Smtp:CertificatePath` | Path to the certificate file (PFX or DER format) |
| `Smtp:CertificatePassword` | Password for PFX files (optional for DER) |

### Certificate Loading

The `LoadCertificate()` method in `SmtpServerHostedService` supports two certificate formats:

**PFX (PKCS#12)** -- when a password is provided:
```csharp
X509CertificateLoader.LoadPkcs12FromFile(path, password);
```

PFX files bundle the certificate and private key together, protected by a password. This is the most common format for Windows environments and many certificate providers.

**DER** -- when no password is provided:
```csharp
X509CertificateLoader.LoadCertificateFromFile(path);
```

DER is a binary certificate format. When using DER, the private key must be accessible through the system's certificate store or bundled in the file.

### Self-Signed Certificates for Development

For local development, generate a self-signed certificate using OpenSSL:

```bash
# Generate a private key and self-signed certificate (valid for 365 days)
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes \
  -subj "/CN=localhost"

# Convert to PFX format (required by .NET)
openssl pkcs12 -export -out cert.pfx -inkey key.pem -in cert.pem -password pass:devpassword
```

Then configure:
```json
{
  "Smtp": {
    "CertificatePath": "/path/to/cert.pfx",
    "CertificatePassword": "devpassword"
  }
}
```

::: warning
Self-signed certificates will cause TLS warnings in mail clients and may cause other mail servers to reject connections. Use self-signed certificates only for development and testing.
:::

### Production Certificates

For production, use a certificate from a trusted Certificate Authority (CA). Common options:

**Let's Encrypt (free, automated):**
```bash
# Using certbot to obtain a certificate
certbot certonly --standalone -d mail.example.com

# Convert Let's Encrypt PEM files to PFX
openssl pkcs12 -export \
  -out /etc/relate-mail/cert.pfx \
  -inkey /etc/letsencrypt/live/mail.example.com/privkey.pem \
  -in /etc/letsencrypt/live/mail.example.com/fullchain.pem \
  -password pass:your-password
```

**Important:** Let's Encrypt certificates expire every 90 days. Set up automated renewal and conversion to PFX format, then restart the SMTP server to load the new certificate.

**Commercial CA certificates:**

Most commercial CAs provide certificates in PEM or PFX format. If you receive PEM files (certificate + chain + private key), convert them to PFX:

```bash
openssl pkcs12 -export \
  -out cert.pfx \
  -inkey private.key \
  -in certificate.crt \
  -certfile ca-chain.crt \
  -password pass:your-password
```

### Certificate Health Check

The Infrastructure layer includes a `CertificateExpiryHealthCheck` that is automatically registered when a certificate path is configured. It monitors the certificate's expiration date and reports:

- **Healthy** -- certificate is valid and not nearing expiration
- **Degraded** -- certificate is approaching expiration (warning threshold)
- **Unhealthy** -- certificate has expired

This check appears in the `/healthz` endpoint response with the `tls` tag.

## Message Size Limits

TLS configuration is often paired with message size limits to protect server resources:

| Setting | Default | Description |
|---------|---------|-------------|
| `Smtp:MaxAttachmentSizeBytes` | 25 MB (26,214,400 bytes) | Maximum size for a single attachment |
| `Smtp:MaxMessageSizeBytes` | 50 MB (52,428,800 bytes) | Maximum total message size (all parts) |

These limits are enforced in the `CustomMessageStore`:

- **Message size** is checked against the raw buffer before any parsing occurs. Oversized messages receive `SmtpReplyCode.SizeLimitExceeded`
- **Attachment size** is checked after decoding each MIME attachment. If any single attachment exceeds the limit, the entire message is rejected

::: tip
The message size limit should be larger than the attachment limit because the MIME encoding overhead (Base64 encoding increases size by ~33%) and message headers add to the total size.
:::

## DNS and MX Records

For the MX endpoint to receive internet mail, your domain's DNS must have an MX record pointing to your server:

```
example.com.    IN  MX  10  mail.example.com.
mail.example.com.  IN  A   203.0.113.1
```

Additionally, for good email deliverability, configure:

- **SPF record** -- declares which servers can send mail for your domain
- **DKIM signing** -- cryptographic signature on outbound mail
- **DMARC policy** -- tells receivers how to handle authentication failures
- **Reverse DNS (PTR)** -- the server IP should resolve back to the mail hostname

These DNS records are outside the scope of the SMTP server configuration but are essential for production mail delivery.
