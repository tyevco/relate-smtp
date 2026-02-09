namespace Relate.Smtp.Infrastructure.Services;

public class OutboundMailOptions
{
    public const string SectionName = "OutboundMail";

    /// <summary>
    /// Whether outbound mail delivery is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Optional relay/smarthost address. If set, all mail is sent through this relay
    /// instead of direct MX delivery.
    /// </summary>
    public string? RelayHost { get; set; }

    /// <summary>
    /// Relay host port. Defaults to 587 (submission).
    /// </summary>
    public int RelayPort { get; set; } = 587;

    /// <summary>
    /// Username for relay authentication (if required).
    /// </summary>
    public string? RelayUsername { get; set; }

    /// <summary>
    /// Password for relay authentication (if required).
    /// </summary>
    public string? RelayPassword { get; set; }

    /// <summary>
    /// Whether to use TLS when connecting to the relay.
    /// </summary>
    public bool RelayUseTls { get; set; } = true;

    /// <summary>
    /// Maximum concurrent delivery tasks.
    /// </summary>
    public int MaxConcurrency { get; set; } = 5;

    /// <summary>
    /// Maximum number of retry attempts per email (RFC 5321 recommends at least 4-5 days of retries).
    /// </summary>
    public int MaxRetries { get; set; } = 10;

    /// <summary>
    /// Base delay in seconds for exponential backoff retry.
    /// </summary>
    public int RetryBaseDelaySeconds { get; set; } = 60;

    /// <summary>
    /// How frequently the queue processor polls for new messages (in seconds).
    /// </summary>
    public int QueuePollingIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// SMTP connection timeout in seconds.
    /// </summary>
    public int SmtpTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// The sender domain used for HELO/EHLO and Message-ID generation.
    /// </summary>
    public string SenderDomain { get; set; } = "localhost";
}
