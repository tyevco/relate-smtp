using Relate.Smtp.ImapHost.Protocol;
using Shouldly;

namespace Relate.Smtp.Tests.Unit.ImapHost;

[Trait("Category", "Unit")]
[Trait("Protocol", "IMAP")]
public class ImapCommandTests
{
    [Fact]
    public void Parse_SimpleCommand_ParsesCorrectly()
    {
        // Arrange
        var line = "A001 CAPABILITY";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A001");
        command.Name.ShouldBe("CAPABILITY");
        command.Arguments.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_CommandWithArguments_ParsesCorrectly()
    {
        // Arrange
        var line = "A002 LOGIN user@example.com password123";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A002");
        command.Name.ShouldBe("LOGIN");
        command.Arguments.ShouldBe(new[] { "user@example.com", "password123" });
    }

    [Fact]
    public void Parse_QuotedStrings_HandlesQuotesCorrectly()
    {
        // Arrange
        var line = "A003 LOGIN \"user@example.com\" \"pass word\"";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A003");
        command.Name.ShouldBe("LOGIN");
        command.Arguments.ShouldBe(new[] { "user@example.com", "pass word" });
    }

    [Fact]
    public void Parse_SelectCommand_ParsesMailboxName()
    {
        // Arrange
        var line = "A004 SELECT INBOX";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A004");
        command.Name.ShouldBe("SELECT");
        command.Arguments.ShouldBe(new[] { "INBOX" });
    }

    [Fact]
    public void Parse_FetchCommand_ParsesSequenceAndItems()
    {
        // Arrange
        var line = "A005 FETCH 1:* (FLAGS)";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A005");
        command.Name.ShouldBe("FETCH");
        command.RawArguments.ShouldContain("1:*");
        command.RawArguments.ShouldContain("(FLAGS)");
    }

    [Fact]
    public void Parse_StoreCommand_ParsesCorrectly()
    {
        // Arrange
        var line = @"A006 STORE 1 +FLAGS (\Seen)";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A006");
        command.Name.ShouldBe("STORE");
        command.Arguments[0].ShouldBe("1");
    }

    [Fact]
    public void Parse_UidCommand_ParsesSubcommand()
    {
        // Arrange
        var line = "A007 UID FETCH 1:* FLAGS";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A007");
        command.Name.ShouldBe("UID");
        command.Arguments[0].ShouldBe("FETCH");
    }

    [Fact]
    public void Parse_LowercaseCommand_ConvertsToUppercase()
    {
        // Arrange
        var line = "a001 select inbox";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("a001"); // Tag preserves case
        command.Name.ShouldBe("SELECT"); // Command is uppercase
    }

    [Fact]
    public void Parse_EmptyLine_ReturnsNoop()
    {
        // Arrange
        var line = "";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("*");
        command.Name.ShouldBe("NOOP");
    }

    [Fact]
    public void Parse_WhitespaceOnlyLine_ReturnsNoop()
    {
        // Arrange
        var line = "   \t  ";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("*");
        command.Name.ShouldBe("NOOP");
    }

    [Fact]
    public void Parse_TagOnly_ReturnsEmptyCommand()
    {
        // Arrange
        var line = "A001";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A001");
        command.Name.ShouldBe(string.Empty);
    }

    [Fact]
    public void Parse_StatusCommand_PreservesRawArguments()
    {
        // Arrange
        var line = "A008 STATUS INBOX (MESSAGES UNSEEN)";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A008");
        command.Name.ShouldBe("STATUS");
        command.RawArguments.ShouldBe("INBOX (MESSAGES UNSEEN)");
    }

    [Fact]
    public void Parse_SearchCommand_PreservesRawArguments()
    {
        // Arrange
        var line = "A009 SEARCH ALL UNSEEN";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A009");
        command.Name.ShouldBe("SEARCH");
        command.RawArguments.ShouldBe("ALL UNSEEN");
    }

    [Fact]
    public void Parse_EscapedQuotes_HandlesBackslash()
    {
        // Arrange
        var line = @"A010 LOGIN ""user@example.com"" ""pass\\word""";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Arguments.ShouldContain(@"pass\word");
    }

    [Fact]
    public void Parse_ExamineCommand_ParsesCorrectly()
    {
        // Arrange
        var line = "A011 EXAMINE INBOX";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A011");
        command.Name.ShouldBe("EXAMINE");
        command.Arguments.ShouldBe(new[] { "INBOX" });
    }

    [Fact]
    public void Parse_ListCommand_ParsesCorrectly()
    {
        // Arrange
        var line = @"A012 LIST """" ""*""";

        // Act
        var command = ImapCommand.Parse(line);

        // Assert
        command.Tag.ShouldBe("A012");
        command.Name.ShouldBe("LIST");
        // Empty quoted string becomes empty string
    }
}
