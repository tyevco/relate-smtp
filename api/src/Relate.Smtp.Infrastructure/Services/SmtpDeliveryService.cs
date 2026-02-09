using System.Diagnostics;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Services;

public class SmtpDeliveryService
{
    private readonly MxResolverService _mxResolver;
    private readonly IOptions<OutboundMailOptions> _options;
    private readonly ILogger<SmtpDeliveryService> _logger;

    public SmtpDeliveryService(
        MxResolverService mxResolver,
        IOptions<OutboundMailOptions> options,
        ILogger<SmtpDeliveryService> logger)
    {
        _mxResolver = mxResolver;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Delivers an outbound email to all recipients. Returns delivery results per recipient.
    /// </summary>
    public async Task<List<RecipientDeliveryResult>> DeliverAsync(
        OutboundEmail email,
        CancellationToken cancellationToken = default)
    {
        using var message = BuildMimeMessage(email);
        var results = new List<RecipientDeliveryResult>();
        var opts = _options.Value;

        if (!string.IsNullOrEmpty(opts.RelayHost))
        {
            // Relay mode: send everything through the configured smarthost
            results.AddRange(await DeliverViaRelayAsync(message, email, cancellationToken).ConfigureAwait(false));
        }
        else
        {
            // Direct MX delivery: group recipients by domain
            var recipientsByDomain = email.Recipients
                .Where(r => r.Status == OutboundRecipientStatus.Pending || r.Status == OutboundRecipientStatus.Deferred)
                .GroupBy(r => MxResolverService.GetDomainFromAddress(r.Address));

            foreach (var domainGroup in recipientsByDomain)
            {
                var domainResults = await DeliverToMxAsync(
                    message, email, domainGroup.Key, domainGroup.ToList(), cancellationToken).ConfigureAwait(false);
                results.AddRange(domainResults);
            }
        }

        return results;
    }

    private async Task<List<RecipientDeliveryResult>> DeliverViaRelayAsync(
        MimeMessage message,
        OutboundEmail email,
        CancellationToken cancellationToken)
    {
        var opts = _options.Value;
        var results = new List<RecipientDeliveryResult>();
        var sw = Stopwatch.StartNew();

        try
        {
            using var client = new SmtpClient();
            client.Timeout = opts.SmtpTimeoutSeconds * 1000;

            var secureSocketOptions = opts.RelayUseTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(opts.RelayHost, opts.RelayPort, secureSocketOptions, cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrEmpty(opts.RelayUsername))
            {
                await client.AuthenticateAsync(opts.RelayUsername, opts.RelayPassword, cancellationToken)
                    .ConfigureAwait(false);
            }

            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

            sw.Stop();

            foreach (var recipient in email.Recipients)
            {
                results.Add(new RecipientDeliveryResult
                {
                    RecipientId = recipient.Id,
                    Address = recipient.Address,
                    Success = true,
                    MxHost = opts.RelayHost!,
                    Duration = sw.Elapsed
                });
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types - Relay delivery must capture all failures for per-recipient status
        catch (Exception ex)
#pragma warning restore CA1031
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to deliver email {EmailId} via relay {RelayHost}",
                email.Id, opts.RelayHost);

            foreach (var recipient in email.Recipients)
            {
                results.Add(new RecipientDeliveryResult
                {
                    RecipientId = recipient.Id,
                    Address = recipient.Address,
                    Success = false,
                    MxHost = opts.RelayHost!,
                    ErrorMessage = ex.Message,
                    Duration = sw.Elapsed
                });
            }
        }

        return results;
    }

    private async Task<List<RecipientDeliveryResult>> DeliverToMxAsync(
        MimeMessage message,
        OutboundEmail email,
        string domain,
        List<OutboundRecipient> recipients,
        CancellationToken cancellationToken)
    {
        var opts = _options.Value;
        var results = new List<RecipientDeliveryResult>();
        var mxHosts = await _mxResolver.ResolveMxHostsAsync(domain, cancellationToken).ConfigureAwait(false);

        foreach (var mxHost in mxHosts)
        {
            var sw = Stopwatch.StartNew();

            try
            {
#pragma warning disable CA2000 // SmtpClient is disposed by the using declaration
                using var client = new SmtpClient();
#pragma warning restore CA2000
                client.Timeout = opts.SmtpTimeoutSeconds * 1000;

                // Try STARTTLS first, fall back to no encryption
                await client.ConnectAsync(mxHost, 25, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken)
                    .ConfigureAwait(false);

                // Build a message with only this domain's recipients
                using var domainMessage = CloneMessageForRecipients(message, recipients);
                await client.SendAsync(domainMessage, cancellationToken).ConfigureAwait(false);
                await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

                sw.Stop();

                foreach (var recipient in recipients)
                {
                    results.Add(new RecipientDeliveryResult
                    {
                        RecipientId = recipient.Id,
                        Address = recipient.Address,
                        Success = true,
                        MxHost = mxHost,
                        Duration = sw.Elapsed
                    });
                }

                // Successfully delivered to this MX host, no need to try others
                return results;
            }
#pragma warning disable CA1031 // Do not catch general exception types - MX delivery must try next host on any failure
            catch (Exception ex)
#pragma warning restore CA1031
            {
                sw.Stop();
                _logger.LogWarning(ex, "Failed to deliver to MX host {MxHost} for domain {Domain}, trying next MX",
                    mxHost, domain);

                // Continue to next MX host
            }
        }

        // All MX hosts failed
        foreach (var recipient in recipients)
        {
            results.Add(new RecipientDeliveryResult
            {
                RecipientId = recipient.Id,
                Address = recipient.Address,
                Success = false,
                MxHost = mxHosts.Count > 0 ? mxHosts[0] : domain,
                ErrorMessage = $"All MX hosts for {domain} failed"
            });
        }

        return results;
    }

    private MimeMessage BuildMimeMessage(OutboundEmail email)
    {
        var message = new MimeMessage();
        message.MessageId = email.MessageId ?? MimeUtils.GenerateMessageId(_options.Value.SenderDomain);
        message.From.Add(new MailboxAddress(email.FromDisplayName, email.FromAddress));

        foreach (var recipient in email.Recipients)
        {
            var address = new MailboxAddress(recipient.DisplayName, recipient.Address);
            switch (recipient.Type)
            {
                case RecipientType.To:
                    message.To.Add(address);
                    break;
                case RecipientType.Cc:
                    message.Cc.Add(address);
                    break;
                case RecipientType.Bcc:
                    message.Bcc.Add(address);
                    break;
            }
        }

        message.Subject = email.Subject;
        message.Date = email.QueuedAt ?? DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(email.InReplyTo))
        {
            message.InReplyTo = email.InReplyTo;
        }

        if (!string.IsNullOrEmpty(email.References))
        {
            foreach (var reference in email.References.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                message.References.Add(reference);
            }
        }

        var builder = new BodyBuilder();
        if (!string.IsNullOrEmpty(email.TextBody))
        {
            builder.TextBody = email.TextBody;
        }
        if (!string.IsNullOrEmpty(email.HtmlBody))
        {
            builder.HtmlBody = email.HtmlBody;
        }

        foreach (var attachment in email.Attachments)
        {
            builder.Attachments.Add(attachment.FileName, attachment.Content,
                ContentType.Parse(attachment.ContentType));
        }

        message.Body = builder.ToMessageBody();
        return message;
    }

    private static MimeMessage CloneMessageForRecipients(MimeMessage original, List<OutboundRecipient> recipients)
    {
        var message = new MimeMessage();
        message.MessageId = original.MessageId;
        foreach (var from in original.From)
            message.From.Add(from);
        message.Subject = original.Subject;
        message.Date = original.Date;

        if (!string.IsNullOrEmpty(original.InReplyTo))
            message.InReplyTo = original.InReplyTo;
        foreach (var reference in original.References)
            message.References.Add(reference);

        // Only add the specific recipients for this domain
        foreach (var recipient in recipients)
        {
            var address = new MailboxAddress(recipient.DisplayName, recipient.Address);
            switch (recipient.Type)
            {
                case RecipientType.To:
                    message.To.Add(address);
                    break;
                case RecipientType.Cc:
                    message.Cc.Add(address);
                    break;
                case RecipientType.Bcc:
                    message.Bcc.Add(address);
                    break;
            }
        }

        message.Body = original.Body;
        return message;
    }
}

public class RecipientDeliveryResult
{
    public Guid RecipientId { get; set; }
    public string Address { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? MxHost { get; set; }
    public int? SmtpStatusCode { get; set; }
    public string? SmtpResponse { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? Duration { get; set; }
}
