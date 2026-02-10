using System.Collections.Concurrent;

namespace Relate.Smtp.Core.Protocol;

public class ConnectionRegistry
{
    private readonly ConcurrentDictionary<Guid, int> _counts = new();

    public bool TryAddConnection(Guid userId, int maxConnections)
    {
        while (true)
        {
            var current = _counts.GetOrAdd(userId, 0);
            if (current >= maxConnections) return false;
            if (_counts.TryUpdate(userId, current + 1, current)) return true;
        }
    }

    public void RemoveConnection(Guid userId)
    {
        _counts.AddOrUpdate(userId, 0, (_, count) => Math.Max(0, count - 1));
    }
}
