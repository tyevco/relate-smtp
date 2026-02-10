using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relate.Smtp.Core.Interfaces;

namespace Relate.Smtp.Infrastructure.Services;

/// <summary>
/// Represents a work item to update API key LastUsedAt timestamp.
/// </summary>
public readonly record struct LastUsedAtUpdate(Guid KeyId, DateTimeOffset Timestamp);

/// <summary>
/// Interface for queueing background tasks.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - This is a legitimate queue interface
public interface IBackgroundTaskQueue
#pragma warning restore CA1711
{
    /// <summary>
    /// Queue an API key LastUsedAt update for background processing.
    /// </summary>
    void QueueLastUsedAtUpdate(Guid keyId, DateTimeOffset timestamp);
}

/// <summary>
/// Channel-based background task queue for processing non-critical updates.
/// Ensures tasks are processed on shutdown rather than being lost.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - This is a legitimate queue implementation
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
#pragma warning restore CA1711
{
    private readonly Channel<LastUsedAtUpdate> _channel;

    public BackgroundTaskQueue()
    {
        // Bounded channel to prevent unbounded memory growth
        var options = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<LastUsedAtUpdate>(options);
    }

    public void QueueLastUsedAtUpdate(Guid keyId, DateTimeOffset timestamp)
    {
        if (keyId == Guid.Empty)
            return;

        // TryWrite is non-blocking and drops if channel is full
        _channel.Writer.TryWrite(new LastUsedAtUpdate(keyId, timestamp));
    }

    internal ChannelReader<LastUsedAtUpdate> Reader => _channel.Reader;

    internal void Complete() => _channel.Writer.Complete();
}

/// <summary>
/// Hosted service that processes queued background tasks.
/// Drains the queue on shutdown to ensure pending updates are saved.
/// </summary>
public sealed class BackgroundTaskQueueHostedService : BackgroundService
{
    private readonly BackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundTaskQueueHostedService> _logger;

    public BackgroundTaskQueueHostedService(
        BackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundTaskQueueHostedService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background task queue started");

        try
        {
            await foreach (var update in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessUpdateAsync(update, CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Draining background task queue...");

        // Signal no more items will be written
        _queue.Complete();

        // Process remaining items
        var count = 0;
        while (_queue.Reader.TryRead(out var update))
        {
            await ProcessUpdateAsync(update, cancellationToken);
            count++;
        }

        if (count > 0)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Processed {Count} pending background tasks before shutdown", count);
            }
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessUpdateAsync(LastUsedAtUpdate update, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ISmtpApiKeyRepository>();
            await repository.UpdateLastUsedAsync(update.KeyId, update.Timestamp, ct);
        }
#pragma warning disable CA1031 // Do not catch general exception types - Intentionally catching all exceptions to prevent background task crashes
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Failed to update LastUsedAt for API key {KeyId}", update.KeyId);
        }
    }
}
