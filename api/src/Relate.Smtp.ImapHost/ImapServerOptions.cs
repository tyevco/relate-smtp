namespace Relate.Smtp.ImapHost;

public class ImapServerOptions
{
    public string ServerName { get; set; } = "localhost";
    public int Port { get; set; } = 143;           // Plain IMAP
    public int SecurePort { get; set; } = 993;     // IMAPS (SSL/TLS)
    public bool RequireAuthentication { get; set; } = true;
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public bool CheckCertificateRevocation { get; set; } = true;
    public int MaxConnectionsPerUser { get; set; } = 5;
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxMessagesPerSession { get; set; } = 2000;
}
