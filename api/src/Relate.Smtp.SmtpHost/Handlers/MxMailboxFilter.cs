using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Telemetry;

namespace Relate.Smtp.SmtpHost.Handlers;

/// <summary>
/// Mailbox filter that enforces open relay prevention for the MX endpoint (port 25).
/// On unauthenticated connections, only accepts mail for recipients at configured hosted domains.
/// On authenticated connections (ports 587/465), accepts all recipients.
/// </summary>
public class MxMailboxFilter : IMailboxFilter, IMailboxFilterFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MxMailboxFilter> _logger;
    private readonly SmtpServerOptions _options;
    private readonly HashSet<string> _hostedDomains;

    public MxMailboxFilter(
        IServiceProvider serviceProvider,
        ILogger<MxMailboxFilter> logger,
        SmtpServerOptions options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        _hostedDomains = new HashSet<string>(
            options.Mx.HostedDomains,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates the sender address (MAIL FROM command).
    /// On the MX endpoint, accepts any sender since external servers deliver on behalf of their users.
    /// On authenticated endpoints, accepts any sender (authenticated users are trusted).
    /// </summary>
    public Task<bool> CanAcceptFromAsync(
        ISessionContext context,
        IMailbox @from,
        int size,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryConfiguration.SmtpActivitySource.StartActivity("smtp.filter.mail_from");
        activity?.SetTag("smtp.from", @from.AsAddress());

        // If the MX endpoint is not enabled, all connections are authenticated — accept all senders
        if (!_options.Mx.Enabled)
        {
            return Task.FromResult(true);
        }

        var isAuthenticated = context.Properties.ContainsKey("AuthenticatedUserId");
        var endpointPort = GetEndpointPort(context);
        activity?.SetTag("smtp.endpoint_port", endpointPort);
        activity?.SetTag("smtp.is_authenticated", isAuthenticated);

        // Authenticated connections (submission ports) can send from any address
        if (isAuthenticated)
        {
            return Task.FromResult(true);
        }

        // On the MX port, accept any sender — external MTAs deliver on behalf of their users.
        // The critical check is on the recipient side (CanDeliverToAsync).
        if (endpointPort == _options.Mx.Port)
        {
            _logger.LogDebug("MX: Accepting MAIL FROM {From} on port {Port}",
                @from.AsAddress(), endpointPort);
            return Task.FromResult(true);
        }

        // Unauthenticated connection on a non-MX port — reject
        _logger.LogWarning("Rejected unauthenticated MAIL FROM {From} on port {Port}",
            @from.AsAddress(), endpointPort);
        return Task.FromResult(false);
    }

    /// <summary>
    /// Validates the recipient address (RCPT TO command).
    /// On the MX endpoint, only accepts recipients at configured hosted domains.
    /// Optionally validates that the recipient user exists in the database.
    /// On authenticated endpoints, accepts all recipients.
    /// </summary>
    public async Task<bool> CanDeliverToAsync(
        ISessionContext context,
        IMailbox to,
        IMailbox @from,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryConfiguration.SmtpActivitySource.StartActivity("smtp.filter.rcpt_to");
        activity?.SetTag("smtp.to", to.AsAddress());
        activity?.SetTag("smtp.from", @from.AsAddress());

        // If MX is not enabled, all connections are authenticated — accept all recipients
        if (!_options.Mx.Enabled)
        {
            return true;
        }

        var isAuthenticated = context.Properties.ContainsKey("AuthenticatedUserId");
        var endpointPort = GetEndpointPort(context);
        activity?.SetTag("smtp.endpoint_port", endpointPort);
        activity?.SetTag("smtp.is_authenticated", isAuthenticated);

        // Authenticated connections can deliver to any recipient
        if (isAuthenticated)
        {
            return true;
        }

        // On the MX port, enforce hosted domain restriction (open relay prevention)
        if (endpointPort == _options.Mx.Port)
        {
            var recipientDomain = to.Host;

            // CRITICAL: Only accept mail for hosted domains to prevent open relay
            if (!_hostedDomains.Contains(recipientDomain))
            {
                _logger.LogWarning(
                    "MX: Rejected relay attempt — recipient {To} is not at a hosted domain. From: {From}",
                    to.AsAddress(), @from.AsAddress());
                activity?.SetTag("smtp.relay_rejected", true);
                return false;
            }

            // Optionally validate that the recipient exists
            if (_options.Mx.ValidateRecipients)
            {
                using var scope = _serviceProvider.CreateScope();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var user = await userRepository.GetByEmailAsync(to.AsAddress(), cancellationToken);

                if (user == null)
                {
                    _logger.LogInformation(
                        "MX: Rejected mail to unknown user {To} at hosted domain. From: {From}",
                        to.AsAddress(), @from.AsAddress());
                    activity?.SetTag("smtp.unknown_recipient", true);
                    return false;
                }
            }

            _logger.LogDebug("MX: Accepted RCPT TO {To} from {From}", to.AsAddress(), @from.AsAddress());
            return true;
        }

        // Unauthenticated connection on a non-MX port — reject
        _logger.LogWarning("Rejected unauthenticated RCPT TO {To} on port {Port}",
            to.AsAddress(), endpointPort);
        return false;
    }

    /// <summary>
    /// Factory method required by SmtpServer to create filter instances per session.
    /// </summary>
    public IMailboxFilter CreateInstance(ISessionContext context)
    {
        return this;
    }

    /// <summary>
    /// Extracts the endpoint port from the session context.
    /// The SmtpServer library sets the EndpointDefinition on the session context.
    /// </summary>
    internal static int GetEndpointPort(ISessionContext context)
    {
        return context.EndpointDefinition.Endpoint.Port;
    }
}
