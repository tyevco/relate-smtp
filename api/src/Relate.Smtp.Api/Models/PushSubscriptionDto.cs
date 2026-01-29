namespace Relate.Smtp.Api.Models;

public class VapidPublicKeyResponse
{
    public string PublicKey { get; set; } = string.Empty;
}

public class PushSubscriptionDto
{
    public Guid Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}

public class CreatePushSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dhKey { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
}
