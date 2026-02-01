using Relate.Smtp.Pop3Host.Protocol;
using Shouldly;

namespace Relate.Smtp.Tests.Unit.Pop3Host;

[Trait("Category", "Unit")]
[Trait("Protocol", "POP3")]
public class Pop3SessionTests
{
    [Fact]
    public void NewSession_HasAuthorizationState()
    {
        // Act
        var session = new Pop3Session();

        // Assert
        session.State.ShouldBe(Pop3State.Authorization);
    }

    [Fact]
    public void NewSession_HasUniqueConnectionId()
    {
        // Act
        var session1 = new Pop3Session();
        var session2 = new Pop3Session();

        // Assert
        session1.ConnectionId.ShouldNotBe(session2.ConnectionId);
        Guid.TryParse(session1.ConnectionId, out _).ShouldBeTrue();
    }

    [Fact]
    public void NewSession_HasEmptyMessageList()
    {
        // Act
        var session = new Pop3Session();

        // Assert
        session.Messages.ShouldBeEmpty();
        session.DeletedMessages.ShouldBeEmpty();
    }

    [Fact]
    public void NewSession_HasNoUser()
    {
        // Act
        var session = new Pop3Session();

        // Assert
        session.Username.ShouldBeNull();
        session.UserId.ShouldBeNull();
    }

    [Fact]
    public void IsTimedOut_RecentActivity_ReturnsFalse()
    {
        // Arrange
        var session = new Pop3Session
        {
            LastActivityAt = DateTime.UtcNow
        };

        // Act
        var result = session.IsTimedOut(TimeSpan.FromMinutes(10));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsTimedOut_OldActivity_ReturnsTrue()
    {
        // Arrange
        var session = new Pop3Session
        {
            LastActivityAt = DateTime.UtcNow.AddMinutes(-15)
        };

        // Act
        var result = session.IsTimedOut(TimeSpan.FromMinutes(10));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsTimedOut_JustBeforeTimeout_ReturnsFalse()
    {
        // Arrange - Use a small buffer to avoid timing race conditions
        var timeout = TimeSpan.FromMinutes(10);
        var justBeforeTimeout = timeout - TimeSpan.FromSeconds(1);
        var session = new Pop3Session
        {
            LastActivityAt = DateTime.UtcNow.Add(-justBeforeTimeout)
        };

        // Act
        var result = session.IsTimedOut(timeout);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DeletedMessages_CanTrackMultipleDeletions()
    {
        // Arrange
        var session = new Pop3Session();
        session.Messages.Add(new Pop3Message { MessageNumber = 1 });
        session.Messages.Add(new Pop3Message { MessageNumber = 2 });
        session.Messages.Add(new Pop3Message { MessageNumber = 3 });

        // Act
        session.DeletedMessages.Add(1);
        session.DeletedMessages.Add(3);

        // Assert
        session.DeletedMessages.Count.ShouldBe(2);
        session.DeletedMessages.ShouldContain(1);
        session.DeletedMessages.ShouldContain(3);
        session.DeletedMessages.ShouldNotContain(2);
    }

    [Fact]
    public void DeletedMessages_ClearRemovesAllDeletions()
    {
        // Arrange
        var session = new Pop3Session();
        session.DeletedMessages.Add(1);
        session.DeletedMessages.Add(2);

        // Act
        session.DeletedMessages.Clear();

        // Assert
        session.DeletedMessages.ShouldBeEmpty();
    }
}
