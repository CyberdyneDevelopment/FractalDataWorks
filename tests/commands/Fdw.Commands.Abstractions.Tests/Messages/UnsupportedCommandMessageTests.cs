using Fdw.Commands.Abstractions.Messages;
using Fdw.Messages;

namespace Fdw.Commands.Abstractions.Tests.Messages;

public sealed class UnsupportedCommandMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        var commandType = "CustomQuery";

        // Act
        var message = new UnsupportedCommandMessage(commandType);

        // Assert
        message.Id.ShouldBe(1003);
        message.Name.ShouldBe("UnsupportedCommand");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Command type 'CustomQuery' is not supported");
        message.Code.ShouldBe("CMD_UNSUP");
        message.CommandType.ShouldBe(commandType);
        message.Category.ShouldBe("Message"); // Category comes from MessageTemplate base
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesEmptyCommandType()
    {
        // Arrange
        var commandType = string.Empty;

        // Act
        var message = new UnsupportedCommandMessage(commandType);

        // Assert
        message.CommandType.ShouldBe(string.Empty);
        message.Message.ShouldBe("Command type '' is not supported");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorIncludesCommandTypeInMessage()
    {
        // Arrange
        var commandType = "ObsoleteQuery";

        // Act
        var message = new UnsupportedCommandMessage(commandType);

        // Assert
        message.Message.ShouldContain(commandType);
        message.Message.ShouldBe($"Command type '{commandType}' is not supported");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DifferentCommandTypesCreateDifferentMessages()
    {
        // Act
        var message1 = new UnsupportedCommandMessage("Type1");
        var message2 = new UnsupportedCommandMessage("Type2");

        // Assert
        message1.CommandType.ShouldBe("Type1");
        message2.CommandType.ShouldBe("Type2");
        message1.Message.ShouldNotBe(message2.Message);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void InheritsFromCommandMessage()
    {
        // Act
        var message = new UnsupportedCommandMessage("Test");

        // Assert
        message.ShouldBeAssignableTo<CommandMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIGenericMessage()
    {
        // Act
        var message = new UnsupportedCommandMessage("Test");

        // Assert
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CommandTypePropertyIsAccessible()
    {
        // Arrange
        var commandType = "CustomType";

        // Act
        var message = new UnsupportedCommandMessage(commandType);

        // Assert
        message.CommandType.ShouldBe(commandType);
    }
}
