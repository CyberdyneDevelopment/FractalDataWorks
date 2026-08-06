using Fdw.Commands.Abstractions.Messages;
using Fdw.Messages;

namespace Fdw.Commands.Abstractions.Tests.Messages;

public sealed class CommandNullMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Act
        var message = new CommandNullMessage();

        // Assert
        message.Id.ShouldBe(1001);
        message.Name.ShouldBe("CommandNull");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Command cannot be null");
        message.Code.ShouldBe("CMD_NULL");
        message.Category.ShouldBe("Message"); // Category comes from MessageTemplate base
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MultipleInstancesHaveSameValues()
    {
        // Act
        var message1 = new CommandNullMessage();
        var message2 = new CommandNullMessage();

        // Assert
        message1.Id.ShouldBe(message2.Id);
        message1.Name.ShouldBe(message2.Name);
        message1.Severity.ShouldBe(message2.Severity);
        message1.Message.ShouldBe(message2.Message);
        message1.Code.ShouldBe(message2.Code);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void InheritsFromCommandMessage()
    {
        // Act
        var message = new CommandNullMessage();

        // Assert
        message.ShouldBeAssignableTo<CommandMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIGenericMessage()
    {
        // Act
        var message = new CommandNullMessage();

        // Assert
        message.ShouldBeAssignableTo<IGenericMessage>();
    }
}
