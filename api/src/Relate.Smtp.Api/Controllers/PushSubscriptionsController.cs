using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Relate.Smtp.Api.Models;
using Relate.Smtp.Api.Services;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;

namespace Relate.Smtp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public class PushSubscriptionsController : ControllerBase
{
    private readonly IPushSubscriptionRepository _subscriptionRepository;
    private readonly IOptions<PushOptions> _pushOptions;

    public PushSubscriptionsController(
        IPushSubscriptionRepository subscriptionRepository,
        IOptions<PushOptions> pushOptions)
    {
        _subscriptionRepository = subscriptionRepository;
        _pushOptions = pushOptions;
    }

    [HttpGet("vapid-public-key")]
    public ActionResult<VapidPublicKeyResponse> GetVapidPublicKey()
    {
        var publicKey = _pushOptions.Value.VapidPublicKey;

        if (string.IsNullOrEmpty(publicKey))
        {
            return BadRequest(new { message = "Push notifications are not configured on this server" });
        }

        return Ok(new VapidPublicKeyResponse { PublicKey = publicKey });
    }

    [HttpPost]
    public async Task<ActionResult<PushSubscriptionDto>> Subscribe(
        [FromBody] CreatePushSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();

        // Check if subscription already exists for this user
        var existing = await _subscriptionRepository.GetByEndpointAsync(request.Endpoint, userId, cancellationToken);
        if (existing != null)
        {
            return Ok(MapToDto(existing));
        }

        var subscription = new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = request.Endpoint,
            P256dhKey = request.P256dhKey,
            AuthKey = request.AuthKey,
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await _subscriptionRepository.AddAsync(subscription, cancellationToken);

        return CreatedAtAction(nameof(Subscribe), new { id = created.Id }, MapToDto(created));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Unsubscribe(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();

        // Verify subscription belongs to user before deleting
        var subscriptions = await _subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);
        var subscription = subscriptions.FirstOrDefault(s => s.Id == id);

        if (subscription == null)
        {
            return NotFound();
        }

        await _subscriptionRepository.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }

    private static PushSubscriptionDto MapToDto(PushSubscription subscription)
    {
        return new PushSubscriptionDto
        {
            Id = subscription.Id,
            Endpoint = subscription.Endpoint,
            CreatedAt = subscription.CreatedAt,
            LastUsedAt = subscription.LastUsedAt
        };
    }
}
