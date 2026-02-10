namespace Relate.Smtp.Pop3Host;

public class Pop3ServerOptions
{
    public string ServerName { get; set; } = "localhost";
    public int Port { get; set; } = 110;           // Plain POP3
    public int SecurePort { get; set; } = 995;     // POP3S (SSL/TLS)
    public bool RequireAuthentication { get; set; } = true;
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public bool CheckCertificateRevocation { get; set; } = true;
    public int MaxConnectionsPerUser { get; set; } = 5;
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public int MaxMessagesPerSession { get; set; } = 1000;
}
