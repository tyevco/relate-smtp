namespace Relate.Smtp.Api.Models;

public class BulkEmailOperationRequest
{
    public List<Guid> EmailIds { get; set; } = new();
    public bool? IsRead { get; set; }
}
