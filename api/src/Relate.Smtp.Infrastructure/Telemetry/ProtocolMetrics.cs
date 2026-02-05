using System.Diagnostics.Metrics;

namespace Relate.Smtp.Infrastructure.Telemetry;

public static class ProtocolMetrics
{
    public const string SmtpMeterName = "Relate.Smtp";
    public const string Pop3MeterName = "Relate.Pop3";
    public const string ImapMeterName = "Relate.Imap";

    private static readonly Meter SmtpMeter = new(SmtpMeterName, "1.0.0");
    private static readonly Meter Pop3Meter = new(Pop3MeterName, "1.0.0");
    private static readonly Meter ImapMeter = new(ImapMeterName, "1.0.0");

    // SMTP Metrics
    public static readonly Counter<long> SmtpMessagesReceived =
        SmtpMeter.CreateCounter<long>(
            "smtp.messages.received",
            unit: "messages",
            description: "Total messages received via SMTP");

    public static readonly Counter<long> SmtpBytesReceived =
        SmtpMeter.CreateCounter<long>(
            "smtp.bytes.received",
            unit: "bytes",
            description: "Total bytes received via SMTP");

    public static readonly Counter<long> SmtpAuthAttempts =
        SmtpMeter.CreateCounter<long>(
            "smtp.auth.attempts",
            unit: "attempts",
            description: "SMTP authentication attempts");

    public static readonly Counter<long> SmtpAuthFailures =
        SmtpMeter.CreateCounter<long>(
            "smtp.auth.failures",
            unit: "failures",
            description: "SMTP authentication failures");

    public static readonly Histogram<double> SmtpMessageProcessingDuration =
        SmtpMeter.CreateHistogram<double>(
            "smtp.message.processing.duration",
            unit: "ms",
            description: "Time to process SMTP message");

    public static readonly UpDownCounter<int> SmtpActiveConnections =
        SmtpMeter.CreateUpDownCounter<int>(
            "smtp.connections.active",
            unit: "connections",
            description: "Active SMTP connections");

    // POP3 Metrics
    public static readonly Counter<long> Pop3MessagesRetrieved =
        Pop3Meter.CreateCounter<long>(
            "pop3.messages.retrieved",
            unit: "messages",
            description: "Total messages retrieved via POP3");

    public static readonly Counter<long> Pop3BytesSent =
        Pop3Meter.CreateCounter<long>(
            "pop3.bytes.sent",
            unit: "bytes",
            description: "Total bytes sent via POP3");

    public static readonly Counter<long> Pop3AuthAttempts =
        Pop3Meter.CreateCounter<long>(
            "pop3.auth.attempts",
            unit: "attempts",
            description: "POP3 authentication attempts");

    public static readonly Counter<long> Pop3AuthFailures =
        Pop3Meter.CreateCounter<long>(
            "pop3.auth.failures",
            unit: "failures",
            description: "POP3 authentication failures");

    public static readonly Counter<long> Pop3Commands =
        Pop3Meter.CreateCounter<long>(
            "pop3.commands",
            unit: "commands",
            description: "POP3 commands processed");

    public static readonly UpDownCounter<int> Pop3ActiveSessions =
        Pop3Meter.CreateUpDownCounter<int>(
            "pop3.sessions.active",
            unit: "sessions",
            description: "Active POP3 sessions");

    // IMAP Metrics
    public static readonly Counter<long> ImapMessagesRetrieved =
        ImapMeter.CreateCounter<long>(
            "imap.messages.retrieved",
            unit: "messages",
            description: "Total messages retrieved via IMAP");

    public static readonly Counter<long> ImapBytesSent =
        ImapMeter.CreateCounter<long>(
            "imap.bytes.sent",
            unit: "bytes",
            description: "Total bytes sent via IMAP");

    public static readonly Counter<long> ImapAuthAttempts =
        ImapMeter.CreateCounter<long>(
            "imap.auth.attempts",
            unit: "attempts",
            description: "IMAP authentication attempts");

    public static readonly Counter<long> ImapAuthFailures =
        ImapMeter.CreateCounter<long>(
            "imap.auth.failures",
            unit: "failures",
            description: "IMAP authentication failures");

    public static readonly Counter<long> ImapCommands =
        ImapMeter.CreateCounter<long>(
            "imap.commands",
            unit: "commands",
            description: "IMAP commands processed");

    public static readonly UpDownCounter<int> ImapActiveSessions =
        ImapMeter.CreateUpDownCounter<int>(
            "imap.sessions.active",
            unit: "sessions",
            description: "Active IMAP sessions");
}
