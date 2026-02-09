using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;

namespace Relate.Smtp.Infrastructure.Services;

public class MxResolverService
{
    private readonly ILookupClient _lookupClient;
    private readonly ILogger<MxResolverService> _logger;

    public MxResolverService(ILookupClient lookupClient, ILogger<MxResolverService> logger)
    {
        _lookupClient = lookupClient;
        _logger = logger;
    }

    /// <summary>
    /// Resolves MX records for the given domain, ordered by preference (lowest first).
    /// Falls back to the domain itself (A/AAAA record) if no MX records are found.
    /// </summary>
    public async Task<IReadOnlyList<string>> ResolveMxHostsAsync(string domain, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _lookupClient.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var mxRecords = result.Answers
                .OfType<MxRecord>()
                .OrderBy(mx => mx.Preference)
                .Select(mx => mx.Exchange.Value.TrimEnd('.'))
                .Where(host => !string.IsNullOrEmpty(host))
                .ToList();

            if (mxRecords.Count > 0)
            {
                _logger.LogDebug("Resolved {Count} MX records for {Domain}: {Hosts}",
                    mxRecords.Count, domain, string.Join(", ", mxRecords));
                return mxRecords;
            }

            // RFC 5321 Section 5.1: If no MX records, fall back to domain itself
            _logger.LogDebug("No MX records found for {Domain}, falling back to domain as host", domain);
            return [domain];
        }
        catch (DnsResponseException ex)
        {
            _logger.LogWarning(ex, "DNS query failed for domain {Domain}", domain);
            return [domain];
        }
    }

    /// <summary>
    /// Extracts the domain from an email address.
    /// </summary>
    public static string GetDomainFromAddress(string emailAddress)
    {
        var atIndex = emailAddress.IndexOf('@', StringComparison.Ordinal);
        return atIndex >= 0 ? emailAddress[(atIndex + 1)..] : emailAddress;
    }
}
