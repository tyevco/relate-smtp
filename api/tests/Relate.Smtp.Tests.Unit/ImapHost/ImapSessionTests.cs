using Relate.Smtp.ImapHost.Protocol;
using Shouldly;

namespace Relate.Smtp.Tests.Unit.ImapHost;

[Trait("Category", "Unit")]
[Trait("Protocol", "IMAP")]
public class ImapSessionTests
{
    [Fact]
    public void NewSession_HasNotAuthenticatedState()
    {
        // Act
        var session = new ImapSession();

        // Assert
        session.State.ShouldBe(ImapState.NotAuthenticated);
    }

    [Fact]
    public void NewSession_HasUniqueConnectionId()
    {
        // Act
        var session1 = new ImapSession();
        var session2 = new ImapSession();

        // Assert
        session1.ConnectionId.ShouldNotBe(session2.ConnectionId);
        Guid.TryParse(session1.ConnectionId, out _).ShouldBeTrue();
    }

    [Fact]
    public void NewSession_HasEmptyMessageList()
    {
        // Act
        var session = new ImapSession();

        // Assert
        session.Messages.ShouldBeEmpty();
        session.DeletedUids.ShouldBeEmpty();
    }

    [Fact]
    public void NewSession_HasNoUser()
    {
        // Act
        var session = new ImapSession();

        // Assert
        session.Username.ShouldBeNull();
        session.UserId.ShouldBeNull();
    }

    [Fact]
    public void NewSession_HasNoSelectedMailbox()
    {
        // Act
        var session = new ImapSession();

        // Assert
        session.SelectedMailbox.ShouldBeNull();
        session.SelectedReadOnly.ShouldBeFalse();
    }

    [Fact]
    public void NewSession_HasNonZeroUidValidity()
    {
        // Act
        var session = new ImapSession();

        // Assert - UIDVALIDITY should be a non-zero timestamp-based value
        session.UidValidity.ShouldBeGreaterThan(0u);
    }

    [Fact]
    public void MultipleSessions_HaveSameUidValidity()
    {
        // Act - Sessions created in the same app instance should share UIDVALIDITY
        var session1 = new ImapSession();
        var session2 = new ImapSession();

        // Assert
        session1.UidValidity.ShouldBe(session2.UidValidity);
    }

    [Fact]
    public void IsTimedOut_RecentActivity_ReturnsFalse()
    {
        // Arrange
        var session = new ImapSession
        {
            LastActivityAt = DateTime.UtcNow
        };

        // Act
        var result = session.IsTimedOut(TimeSpan.FromMinutes(30));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsTimedOut_OldActivity_ReturnsTrue()
    {
        // Arrange
        var session = new ImapSession
        {
            LastActivityAt = DateTime.UtcNow.AddMinutes(-35)
        };

        // Act
        var result = session.IsTimedOut(TimeSpan.FromMinutes(30));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DeletedUids_TracksMultipleUids()
    {
        // Arrange
        var session = new ImapSession();

        // Act
        session.DeletedUids.Add(1);
        session.DeletedUids.Add(5);
        session.DeletedUids.Add(10);

        // Assert
        session.DeletedUids.Count.ShouldBe(3);
        session.DeletedUids.ShouldContain(1u);
        session.DeletedUids.ShouldContain(5u);
        session.DeletedUids.ShouldContain(10u);
    }

    [Fact]
    public void DeletedUids_ClearRemovesAll()
    {
        // Arrange
        var session = new ImapSession();
        session.DeletedUids.Add(1);
        session.DeletedUids.Add(2);

        // Act
        session.DeletedUids.Clear();

        // Assert
        session.DeletedUids.ShouldBeEmpty();
    }

    [Fact]
    public void EnabledCapabilities_TracksEnabled()
    {
        // Arrange
        var session = new ImapSession();

        // Act
        session.EnabledCapabilities.Add("UTF8=ACCEPT");

        // Assert
        session.EnabledCapabilities.ShouldContain("UTF8=ACCEPT");
    }
}

[Trait("Category", "Unit")]
[Trait("Protocol", "IMAP")]
public class ImapMessageTests
{
    [Fact]
    public void NewMessage_HasDefaultFlags()
    {
        // Act
        var message = new ImapMessage();

        // Assert
        message.Flags.ShouldBe(ImapFlags.None);
    }

    [Fact]
    public void Flags_CanBeSet()
    {
        // Arrange
        var message = new ImapMessage();

        // Act
        message.Flags = ImapFlags.Seen | ImapFlags.Flagged;

        // Assert
        message.Flags.HasFlag(ImapFlags.Seen).ShouldBeTrue();
        message.Flags.HasFlag(ImapFlags.Flagged).ShouldBeTrue();
        message.Flags.HasFlag(ImapFlags.Deleted).ShouldBeFalse();
    }
}

[Trait("Category", "Unit")]
[Trait("Protocol", "IMAP")]
public class ImapFlagsExtensionsTests
{
    [Fact]
    public void ToImapString_NoFlags_ReturnsEmpty()
    {
        // Arrange
        var flags = ImapFlags.None;

        // Act
        var result = flags.ToImapString();

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToImapString_SingleFlag_ReturnsCorrectFormat()
    {
        // Arrange
        var flags = ImapFlags.Seen;

        // Act
        var result = flags.ToImapString();

        // Assert
        result.ShouldBe(@"\Seen");
    }

    [Fact]
    public void ToImapString_MultipleFlags_ReturnsSpaceSeparated()
    {
        // Arrange
        var flags = ImapFlags.Seen | ImapFlags.Answered | ImapFlags.Flagged;

        // Act
        var result = flags.ToImapString();

        // Assert
        result.ShouldContain(@"\Seen");
        result.ShouldContain(@"\Answered");
        result.ShouldContain(@"\Flagged");
    }

    [Fact]
    public void ToImapString_AllFlags_ReturnsAllFlagStrings()
    {
        // Arrange
        var flags = ImapFlags.Seen | ImapFlags.Answered | ImapFlags.Flagged | ImapFlags.Deleted | ImapFlags.Draft;

        // Act
        var result = flags.ToImapString();

        // Assert
        result.ShouldContain(@"\Seen");
        result.ShouldContain(@"\Answered");
        result.ShouldContain(@"\Flagged");
        result.ShouldContain(@"\Deleted");
        result.ShouldContain(@"\Draft");
    }

    [Theory]
    [InlineData(new[] { @"\SEEN" }, ImapFlags.Seen)]
    [InlineData(new[] { @"\ANSWERED" }, ImapFlags.Answered)]
    [InlineData(new[] { @"\FLAGGED" }, ImapFlags.Flagged)]
    [InlineData(new[] { @"\DELETED" }, ImapFlags.Deleted)]
    [InlineData(new[] { @"\DRAFT" }, ImapFlags.Draft)]
    public void ParseFlags_SingleFlag_ParsesCorrectly(string[] flagStrings, ImapFlags expected)
    {
        // Act
        var result = ImapFlagsExtensions.ParseFlags(flagStrings);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void ParseFlags_MultipleFlags_ParsesAll()
    {
        // Arrange
        var flagStrings = new[] { @"\Seen", @"\Flagged", @"\Answered" };

        // Act
        var result = ImapFlagsExtensions.ParseFlags(flagStrings);

        // Assert
        result.HasFlag(ImapFlags.Seen).ShouldBeTrue();
        result.HasFlag(ImapFlags.Flagged).ShouldBeTrue();
        result.HasFlag(ImapFlags.Answered).ShouldBeTrue();
        result.HasFlag(ImapFlags.Deleted).ShouldBeFalse();
    }

    [Fact]
    public void ParseFlags_CaseInsensitive()
    {
        // Arrange
        var flagStrings = new[] { @"\seen", @"\FLAGGED", @"\Answered" };

        // Act
        var result = ImapFlagsExtensions.ParseFlags(flagStrings);

        // Assert
        result.HasFlag(ImapFlags.Seen).ShouldBeTrue();
        result.HasFlag(ImapFlags.Flagged).ShouldBeTrue();
        result.HasFlag(ImapFlags.Answered).ShouldBeTrue();
    }

    [Fact]
    public void ParseFlags_EmptyList_ReturnsNone()
    {
        // Arrange
        var flagStrings = Array.Empty<string>();

        // Act
        var result = ImapFlagsExtensions.ParseFlags(flagStrings);

        // Assert
        result.ShouldBe(ImapFlags.None);
    }

    [Fact]
    public void ParseFlags_UnknownFlags_Ignored()
    {
        // Arrange
        var flagStrings = new[] { @"\Seen", @"\Custom", @"\Unknown" };

        // Act
        var result = ImapFlagsExtensions.ParseFlags(flagStrings);

        // Assert
        result.ShouldBe(ImapFlags.Seen);
    }
}
