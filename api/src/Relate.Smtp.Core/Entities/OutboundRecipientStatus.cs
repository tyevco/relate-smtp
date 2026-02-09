namespace Relate.Smtp.Core.Entities;

public enum OutboundRecipientStatus
{
    Pending,
    Sending,
    Sent,
    Failed,
    Deferred
}
