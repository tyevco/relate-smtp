using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;

namespace Relate.Smtp.Infrastructure.Services;

public class DeliveryQueueProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OutboundMailOptions> _options;
    private readonly ILogger<DeliveryQueueProcessor> _logger;

    public DeliveryQueueProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboundMailOptions> options,
        ILogger<DeliveryQueueProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;

        if (!opts.Enabled)
        {
            _logger.LogInformation("Outbound mail delivery is disabled");
            return;
        }

        _logger.LogInformation("Outbound mail delivery processor started with max concurrency {MaxConcurrency}",
            opts.MaxConcurrency);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing delivery queue");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(opts.QueuePollingIntervalSeconds), stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Outbound mail delivery processor stopped");
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        var opts = _options.Value;

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboundEmailRepository>();
        var deliveryService = scope.ServiceProvider.GetRequiredService<SmtpDeliveryService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IDeliveryNotificationService>();

        var queuedEmails = await repository.GetQueuedForDeliveryAsync(opts.MaxConcurrency, cancellationToken)
            .ConfigureAwait(false);

        if (queuedEmails.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} queued emails for delivery", queuedEmails.Count);

        var tasks = queuedEmails.Select(email =>
            DeliverEmailAsync(email, repository, deliveryService, notificationService, cancellationToken));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task DeliverEmailAsync(
        OutboundEmail email,
        IOutboundEmailRepository repository,
        SmtpDeliveryService deliveryService,
        IDeliveryNotificationService notificationService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Mark as sending
            email.Status = OutboundEmailStatus.Sending;
            await repository.UpdateAsync(email, cancellationToken).ConfigureAwait(false);
            await notificationService.NotifyDeliveryStatusChangedAsync(email.UserId, email.Id,
                OutboundEmailStatus.Sending.ToString(), cancellationToken).ConfigureAwait(false);

            // Attempt delivery
            var results = await deliveryService.DeliverAsync(email, cancellationToken).ConfigureAwait(false);

            // Log delivery attempts
            foreach (var result in results)
            {
                var log = new DeliveryLog
                {
                    Id = Guid.NewGuid(),
                    OutboundEmailId = email.Id,
                    RecipientId = result.RecipientId,
                    RecipientAddress = result.Address,
                    MxHost = result.MxHost,
                    SmtpStatusCode = result.SmtpStatusCode,
                    SmtpResponse = result.SmtpResponse,
                    Success = result.Success,
                    ErrorMessage = result.ErrorMessage,
                    AttemptNumber = email.RetryCount + 1,
                    AttemptedAt = DateTimeOffset.UtcNow,
                    Duration = result.Duration
                };
                await repository.AddDeliveryLogAsync(log, cancellationToken).ConfigureAwait(false);
            }

            // Update recipient statuses
            foreach (var result in results)
            {
                var recipient = email.Recipients.FirstOrDefault(r => r.Id == result.RecipientId);
                if (recipient != null)
                {
                    recipient.Status = result.Success
                        ? OutboundRecipientStatus.Sent
                        : OutboundRecipientStatus.Failed;
                    recipient.StatusMessage = result.ErrorMessage;
                    if (result.Success)
                    {
                        recipient.DeliveredAt = DateTimeOffset.UtcNow;
                    }
                }
            }

            // Determine overall email status
            var allSucceeded = results.All(r => r.Success);
            var allFailed = results.All(r => !r.Success);

            if (allSucceeded)
            {
                email.Status = OutboundEmailStatus.Sent;
                email.SentAt = DateTimeOffset.UtcNow;
                _logger.LogInformation("Email {EmailId} delivered successfully to all recipients", email.Id);
            }
            else if (allFailed)
            {
                HandleFailure(email);
            }
            else
            {
                // Some succeeded, some failed
                email.Status = OutboundEmailStatus.PartialFailure;
                email.SentAt = DateTimeOffset.UtcNow;
                email.LastError = string.Join("; ",
                    results.Where(r => !r.Success).Select(r => $"{r.Address}: {r.ErrorMessage}"));
                _logger.LogWarning("Email {EmailId} partially delivered. Failed recipients: {Errors}",
                    email.Id, email.LastError);
            }

            await repository.UpdateAsync(email, cancellationToken).ConfigureAwait(false);
            await notificationService.NotifyDeliveryStatusChangedAsync(email.UserId, email.Id,
                email.Status.ToString(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error delivering email {EmailId}", email.Id);
            email.LastError = ex.Message;
            HandleFailure(email);
            await repository.UpdateAsync(email, cancellationToken).ConfigureAwait(false);
            await notificationService.NotifyDeliveryStatusChangedAsync(email.UserId, email.Id,
                email.Status.ToString(), cancellationToken).ConfigureAwait(false);
        }
    }

    private void HandleFailure(OutboundEmail email)
    {
        var opts = _options.Value;
        email.RetryCount++;

        if (email.RetryCount >= opts.MaxRetries)
        {
            email.Status = OutboundEmailStatus.Failed;
            _logger.LogError("Email {EmailId} permanently failed after {RetryCount} attempts",
                email.Id, email.RetryCount);
        }
        else
        {
            // Exponential backoff: baseDelay * 2^(retryCount-1)
            var delaySeconds = opts.RetryBaseDelaySeconds * Math.Pow(2, email.RetryCount - 1);
            // Cap at 1 hour
            delaySeconds = Math.Min(delaySeconds, 3600);
            email.NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
            email.Status = OutboundEmailStatus.Queued;
            _logger.LogWarning("Email {EmailId} deferred, attempt {Attempt}/{MaxRetries}. Next retry at {NextRetry}",
                email.Id, email.RetryCount, opts.MaxRetries, email.NextRetryAt);
        }
    }
}
