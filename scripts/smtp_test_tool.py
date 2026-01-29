#!/usr/bin/env python3
"""
SMTP Test Tool - Send test emails to your mock SMTP server
"""

import argparse
import smtplib
import sys
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from datetime import datetime


def send_test_email(
    host='localhost',
    port=1025,
    from_addr='test@example.com',
    to_addr='recipient@example.com',
    subject='Test Email',
    body='This is a test email.',
    html=False,
    use_tls=False,
    use_ssl=False,
    username=None,
    password=None,
    cc=None,
    bcc=None
):
    """Send a test email through the specified SMTP server."""
    
    try:
        # Create message
        if html:
            msg = MIMEMultipart('alternative')
            msg.attach(MIMEText(body, 'html'))
        else:
            msg = MIMEText(body, 'plain')
        
        msg['Subject'] = subject
        msg['From'] = from_addr
        msg['To'] = to_addr
        msg['Date'] = datetime.now().strftime('%a, %d %b %Y %H:%M:%S %z')
        
        if cc:
            msg['Cc'] = cc
        if bcc:
            msg['Bcc'] = bcc
        
        # Determine recipients
        recipients = [to_addr]
        if cc:
            recipients.extend([addr.strip() for addr in cc.split(',')])
        if bcc:
            recipients.extend([addr.strip() for addr in bcc.split(',')])
        
        # Connect to SMTP server
        print(f"Connecting to SMTP server at {host}:{port}...")
        
        if use_ssl:
            server = smtplib.SMTP_SSL(host, port)
        else:
            server = smtplib.SMTP(host, port)
        
        server.set_debuglevel(0)
        
        if use_tls and not use_ssl:
            print("Starting TLS...")
            server.starttls()
        
        # Authenticate if credentials provided
        if username and password:
            print(f"Authenticating as {username}...")
            server.login(username, password)
        
        # Send email
        print(f"Sending email from {from_addr} to {to_addr}...")
        server.send_message(msg)
        
        server.quit()
        
        print("✓ Email sent successfully!")
        print(f"\nDetails:")
        print(f"  From: {from_addr}")
        print(f"  To: {to_addr}")
        if cc:
            print(f"  Cc: {cc}")
        if bcc:
            print(f"  Bcc: {bcc}")
        print(f"  Subject: {subject}")
        print(f"  Body length: {len(body)} characters")
        
        return True
        
    except Exception as e:
        print(f"✗ Error sending email: {e}", file=sys.stderr)
        return False


def main():
    parser = argparse.ArgumentParser(
        description='Send test emails to your mock SMTP server',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog='''
Examples:
  # Basic test email to localhost:1025
  %(prog)s
  
  # Custom SMTP server
  %(prog)s --host smtp.example.com --port 587
  
  # Send HTML email
  %(prog)s --html --body "<h1>Hello</h1><p>This is a test.</p>"
  
  # With authentication
  %(prog)s --username user@example.com --password secret123
  
  # Multiple recipients
  %(prog)s --to user1@example.com --cc "user2@example.com,user3@example.com"
        '''
    )
    
    # SMTP server settings
    parser.add_argument('--host', default='localhost',
                       help='SMTP server host (default: localhost)')
    parser.add_argument('--port', type=int, default=1025,
                       help='SMTP server port (default: 1025)')
    parser.add_argument('--tls', action='store_true',
                       help='Use STARTTLS')
    parser.add_argument('--ssl', action='store_true',
                       help='Use SSL/TLS connection')
    
    # Authentication
    parser.add_argument('--username', help='SMTP username')
    parser.add_argument('--password', help='SMTP password')
    
    # Email content
    parser.add_argument('--from', dest='from_addr', default='test@example.com',
                       help='From address (default: test@example.com)')
    parser.add_argument('--to', dest='to_addr', default='recipient@example.com',
                       help='To address (default: recipient@example.com)')
    parser.add_argument('--cc', help='Cc addresses (comma-separated)')
    parser.add_argument('--bcc', help='Bcc addresses (comma-separated)')
    parser.add_argument('--subject', default='Test Email',
                       help='Email subject (default: "Test Email")')
    parser.add_argument('--body', default='This is a test email.',
                       help='Email body (default: "This is a test email.")')
    parser.add_argument('--html', action='store_true',
                       help='Send as HTML email')
    
    # Batch mode
    parser.add_argument('--count', type=int, default=1,
                       help='Number of emails to send (default: 1)')
    
    args = parser.parse_args()
    
    # Send emails
    success_count = 0
    for i in range(args.count):
        if args.count > 1:
            print(f"\n{'='*60}")
            print(f"Sending email {i+1}/{args.count}")
            print('='*60)
        
        if send_test_email(
            host=args.host,
            port=args.port,
            from_addr=args.from_addr,
            to_addr=args.to_addr,
            subject=args.subject,
            body=args.body,
            html=args.html,
            use_tls=args.tls,
            use_ssl=args.ssl,
            username=args.username,
            password=args.password,
            cc=args.cc,
            bcc=args.bcc
        ):
            success_count += 1
    
    if args.count > 1:
        print(f"\n{'='*60}")
        print(f"Summary: {success_count}/{args.count} emails sent successfully")
        print('='*60)
    
    return 0 if success_count == args.count else 1


if __name__ == '__main__':
    sys.exit(main())