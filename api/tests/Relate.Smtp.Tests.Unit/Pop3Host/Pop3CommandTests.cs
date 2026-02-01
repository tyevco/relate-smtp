using Relate.Smtp.Pop3Host.Protocol;
using Shouldly;

namespace Relate.Smtp.Tests.Unit.Pop3Host;

[Trait("Category", "Unit")]
[Trait("Protocol", "POP3")]
public class Pop3CommandTests
{
    [Theory]
    [InlineData("USER john@example.com", "USER", new[] { "john@example.com" })]
    [InlineData("PASS secret123", "PASS", new[] { "secret123" })]
    [InlineData("STAT", "STAT", new string[0])]
    [InlineData("LIST", "LIST", new string[0])]
    [InlineData("LIST 1", "LIST", new[] { "1" })]
    [InlineData("RETR 1", "RETR", new[] { "1" })]
    [InlineData("DELE 1", "DELE", new[] { "1" })]
    [InlineData("NOOP", "NOOP", new string[0])]
    [InlineData("RSET", "RSET", new string[0])]
    [InlineData("QUIT", "QUIT", new string[0])]
    [InlineData("UIDL", "UIDL", new string[0])]
    [InlineData("UIDL 1", "UIDL", new[] { "1" })]
    [InlineData("TOP 1 10", "TOP", new[] { "1", "10" })]
    public void Parse_ValidCommand_ParsesCorrectly(string input, string expectedName, string[] expectedArgs)
    {
        // Act
        var command = Pop3Command.Parse(input);

        // Assert
        command.Name.ShouldBe(expectedName);
        command.Arguments.ShouldBe(expectedArgs);
    }

    [Fact]
    public void Parse_LowercaseCommand_ConvertsToUppercase()
    {
        // Act
        var command = Pop3Command.Parse("user test@example.com");

        // Assert
        command.Name.ShouldBe("USER");
    }

    [Fact]
    public void Parse_MixedCaseCommand_ConvertsToUppercase()
    {
        // Act
        var command = Pop3Command.Parse("StAt");

        // Assert
        command.Name.ShouldBe("STAT");
    }

    [Fact]
    public void Parse_EmptyLine_ReturnsEmptyCommand()
    {
        // Act
        var command = Pop3Command.Parse("");

        // Assert
        command.Name.ShouldBe(string.Empty);
        command.Arguments.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_WhitespaceOnlyLine_ReturnsEmptyCommand()
    {
        // Act
        var command = Pop3Command.Parse("   \t  ");

        // Assert
        command.Name.ShouldBe(string.Empty);
        command.Arguments.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_ExtraWhitespace_TrimsCorrectly()
    {
        // Act
        var command = Pop3Command.Parse("  USER   test@example.com  ");

        // Assert
        command.Name.ShouldBe("USER");
        command.Arguments.ShouldBe(new[] { "test@example.com" });
    }

    [Fact]
    public void Parse_MultipleSpacesBetweenArgs_HandlesCorrectly()
    {
        // Act
        var command = Pop3Command.Parse("TOP    1     10");

        // Assert
        command.Name.ShouldBe("TOP");
        command.Arguments.ShouldBe(new[] { "1", "10" });
    }
}
