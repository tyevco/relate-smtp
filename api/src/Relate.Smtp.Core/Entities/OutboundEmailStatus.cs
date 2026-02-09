namespace Relate.Smtp.Core.Entities;

public enum OutboundEmailStatus
{
    Draft,
    Queued,
    Sending,
    Sent,
    PartialFailure,
    Failed
}
