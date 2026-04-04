# Email Client Setup

Relate Mail supports standard email protocols (SMTP, IMAP, POP3), so it works with any email client that supports these protocols. This guide covers setup for the most popular email clients.

## Prerequisites

Before configuring an email client, you need:

1. **A Relate Mail account** -- sign up or be provisioned through the web interface
2. **An API key** -- generate one from the web UI under **SMTP Settings**
3. **Correct scopes** -- the API key must have the appropriate scopes enabled:
   - `smtp` -- for sending email
   - `imap` -- for reading email via IMAP
   - `pop3` -- for reading email via POP3
4. **Server hostname** -- the hostname or IP address where Relate Mail is running

### Generating an API Key

1. Log into the Relate Mail web interface
2. Navigate to **SMTP Settings** (or **Settings > SMTP Credentials**)
3. Click **Generate New Key**
4. Select the scopes you need (`smtp`, `imap`, and/or `pop3`)
5. Copy the generated key -- it will only be shown once
6. Use this key as the password in your email client

## Connection Settings Summary

| Protocol | Port | Encryption | Authentication |
|----------|------|------------|----------------|
| SMTP (STARTTLS) | 587 | STARTTLS | Username + API key |
| SMTP (SSL/TLS) | 465 | Implicit TLS | Username + API key |
| IMAP (STARTTLS) | 143 | STARTTLS | Username + API key |
| IMAP (SSL/TLS) | 993 | Implicit TLS | Username + API key |
| POP3 (STARTTLS) | 110 | STARTTLS | Username + API key |
| POP3 (SSL/TLS) | 995 | Implicit TLS | Username + API key |

For all protocols:
- **Username** = your email address
- **Password** = your API key (not your account password)

## Mozilla Thunderbird

Thunderbird is a free, open-source email client available for Windows, macOS, and Linux.

### Setup Steps

1. Open Thunderbird and go to **Account Settings** (hamburger menu > Account Settings, or Edit > Account Settings)
2. Click **Account Actions > Add Mail Account**
3. Enter your name, email address, and API key as the password
4. Click **Configure manually** (do not use auto-configuration)

**Incoming Server (IMAP recommended):**

| Setting | Value |
|---------|-------|
| Protocol | IMAP |
| Hostname | `mail.example.com` (your server) |
| Port | `993` |
| Connection Security | SSL/TLS |
| Authentication Method | Normal password |
| Username | your email address |

**Outgoing Server (SMTP):**

| Setting | Value |
|---------|-------|
| Hostname | `mail.example.com` (your server) |
| Port | `465` |
| Connection Security | SSL/TLS |
| Authentication Method | Normal password |
| Username | your email address |

5. Click **Re-test** to verify the connection
6. Click **Done** to save

If you prefer STARTTLS over implicit TLS, use port 143 for IMAP and port 587 for SMTP, and select "STARTTLS" as the connection security.

::: info Screenshot
![Screenshot: Thunderbird account setup](./screenshots/email-client-thunderbird.png)

_TODO: Add screenshot of Thunderbird manual account configuration dialog_
:::

### Using POP3 Instead of IMAP

If you prefer POP3 (which downloads messages to your device and optionally removes them from the server):

| Setting | Value |
|---------|-------|
| Protocol | POP3 |
| Hostname | `mail.example.com` |
| Port | `995` |
| Connection Security | SSL/TLS |
| Authentication Method | Normal password |
| Username | your email address |

## Microsoft Outlook

### Outlook for Windows (New Outlook / Outlook 365)

1. Open Outlook and go to **File > Add Account**
2. Enter your email address and click **Advanced options > Let me set up my account manually**
3. Select **IMAP** (or **POP** if preferred)

**IMAP Settings:**

| Setting | Value |
|---------|-------|
| Incoming mail server | `mail.example.com` |
| Incoming port | `993` |
| Encryption method | SSL/TLS |
| Outgoing mail server | `mail.example.com` |
| Outgoing port | `465` |
| Encryption method | SSL/TLS |

4. Enter your API key when prompted for a password
5. Click **Connect**

### Outlook for Mac

1. Open Outlook and go to **Outlook > Preferences > Accounts**
2. Click the **+** button and select **New Account**
3. Enter your email address and click **Continue**
4. If auto-configuration fails, select **IMAP** and enter the settings manually

Use the same server settings as Outlook for Windows above.

## Apple Mail

### macOS

1. Open **System Settings > Internet Accounts** (or System Preferences > Internet Accounts on older macOS)
2. Click **Add Other Account > Mail Account**
3. Enter your name, email address, and API key as the password
4. Click **Sign In** -- if auto-configuration fails, you will be prompted for manual settings

**Incoming Mail Server:**

| Setting | Value |
|---------|-------|
| Account Type | IMAP |
| Mail Server | `mail.example.com` |
| Port | `993` |
| Use SSL | Yes |
| Authentication | Password |
| Username | your email address |

**Outgoing Mail Server:**

| Setting | Value |
|---------|-------|
| SMTP Server | `mail.example.com` |
| Port | `465` |
| Use SSL | Yes |
| Authentication | Password |
| Username | your email address |

5. Select which apps to use with this account (Mail, Notes, etc.)
6. Click **Done**

### iOS / iPadOS

1. Go to **Settings > Mail > Accounts > Add Account**
2. Select **Other > Add Mail Account**
3. Enter your name, email, API key (as password), and a description
4. Tap **Next**
5. Select **IMAP** at the top
6. Enter the incoming and outgoing server settings:

| Field | Value |
|-------|-------|
| Incoming Host Name | `mail.example.com` |
| Incoming Username | your email address |
| Incoming Password | your API key |
| Outgoing Host Name | `mail.example.com` |
| Outgoing Username | your email address |
| Outgoing Password | your API key |

7. Tap **Next** and then **Save**

To configure ports and SSL, go to **Settings > Mail > Accounts > your account > Account > Advanced**:
- Use SSL: On
- IMAP Port: 993
- SMTP Port: 465

## Other Email Clients

Any email client that supports IMAP/POP3 and SMTP will work with Relate Mail. Use these settings:

### Incoming Mail (IMAP -- recommended)

| Setting | Value |
|---------|-------|
| Server | Your Relate Mail server hostname |
| Port | 993 (SSL/TLS) or 143 (STARTTLS) |
| Encryption | SSL/TLS or STARTTLS |
| Username | Your email address |
| Password | Your API key |

### Incoming Mail (POP3 -- alternative)

| Setting | Value |
|---------|-------|
| Server | Your Relate Mail server hostname |
| Port | 995 (SSL/TLS) or 110 (STARTTLS) |
| Encryption | SSL/TLS or STARTTLS |
| Username | Your email address |
| Password | Your API key |

### Outgoing Mail (SMTP)

| Setting | Value |
|---------|-------|
| Server | Your Relate Mail server hostname |
| Port | 465 (SSL/TLS) or 587 (STARTTLS) |
| Encryption | SSL/TLS or STARTTLS |
| Username | Your email address |
| Password | Your API key |

## Troubleshooting

### "Authentication failed" or "Login failed"

- Verify you are using the **API key** as the password, not your account password
- Check that the API key has the correct scopes enabled (`smtp` for sending, `imap` or `pop3` for receiving)
- Ensure the API key has not been revoked in the web UI

### "Connection refused" or "Connection timed out"

- Verify the server hostname and port are correct
- Check that the relevant protocol is enabled on the server (`Smtp__Enabled`, `Imap__Enabled`, `Pop3__Enabled`)
- Ensure firewall rules allow traffic on the required ports
- If using a self-signed TLS certificate, your email client may reject the connection -- check client settings for certificate exceptions

### "Certificate error" or "SSL error"

- If using a self-signed certificate, add it as a trusted certificate in your email client
- If using Let's Encrypt or another CA, ensure the certificate is valid and not expired
- Verify the certificate's Common Name (CN) or Subject Alternative Name (SAN) matches the hostname you are connecting to

### "Cannot send mail" (SMTP errors)

- Verify the outgoing server settings (hostname, port, encryption)
- Check that the API key has the `smtp` scope
- If using port 587, ensure the encryption is set to STARTTLS (not SSL/TLS, which is for port 465)
- If using port 465, ensure the encryption is set to SSL/TLS (not STARTTLS, which is for port 587)
