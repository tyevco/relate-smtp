namespace Relate.Smtp.Tests.Common.Helpers;

/// <summary>
/// Common test utilities for reliable async testing.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Polls a condition until it returns true or the timeout is reached.
    /// </summary>
    /// <param name="condition">The async condition to evaluate.</param>
    /// <param name="timeout">Maximum time to wait (default: 10 seconds).</param>
    /// <param name="pollInterval">Interval between condition checks (default: 100ms).</param>
    /// <param name="timeoutMessage">Custom message for timeout exception.</param>
    /// <exception cref="TimeoutException">Thrown when condition is not met within the timeout.</exception>
    public static async Task WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        string? timeoutMessage = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);
        pollInterval ??= TimeSpan.FromMilliseconds(100);

        using var cts = new CancellationTokenSource(timeout.Value);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (await condition())
                    return;

                await Task.Delay(pollInterval.Value, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }

        throw new TimeoutException(
            timeoutMessage ?? $"Condition not met within {timeout.Value.TotalSeconds}s");
    }

    /// <summary>
    /// Synchronous overload for condition polling.
    /// </summary>
    public static async Task WaitForConditionAsync(
        Func<bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        string? timeoutMessage = null)
    {
        await WaitForConditionAsync(
            () => Task.FromResult(condition()),
            timeout,
            pollInterval,
            timeoutMessage);
    }
}
