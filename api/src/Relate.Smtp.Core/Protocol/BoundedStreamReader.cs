using System.Text;

namespace Relate.Smtp.Core.Protocol;

public static class BoundedStreamReader
{
    public static async Task<string?> ReadLineBoundedAsync(
        StreamReader reader, int maxLength = 8192, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        var buffer = new char[1];
        while (!ct.IsCancellationRequested)
        {
            var read = await reader.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (read == 0) return sb.Length == 0 ? null : sb.ToString();
            if (buffer[0] == '\n') return sb.ToString();
            if (buffer[0] == '\r') continue;
            if (sb.Length >= maxLength)
                throw new InvalidOperationException($"Line exceeds maximum length of {maxLength}");
            sb.Append(buffer[0]);
        }
        return null;
    }
}
