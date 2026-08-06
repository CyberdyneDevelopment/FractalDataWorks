using Fdw.Commands.Abstractions.Messages;
using Fdw.Messages;

namespace Fdw.Commands.Abstractions.Tests.Messages;

public sealed class TranslationFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        var reason = "Unsupported feature";

        // Act
        var message = new TranslationFailedMessage(reason);

        // Assert
        message.Id.ShouldBe(1002);
        message.Name.ShouldBe("TranslationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to translate command: Unsupported feature");
        message.Code.ShouldBe("CMD_TRANS_001");
        message.Reason.ShouldBe(reason);
        message.Category.ShouldBe("Message"); // Category comes from MessageTemplate base
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesEmptyReason()
    {
        // Arrange
        var reason = string.Empty;

        // Act
        var message = new TranslationFailedMessage(reason);

        // Assert
        message.Reason.ShouldBe(string.Empty);
        message.Message.ShouldBe("Failed to translate command: ");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorIncludesReasonInMessage()
    {
        // Arrange
        var reason = "Complex query not supported";

        // Act
        var message = new TranslationFailedMessage(reason);

        // Assert
        message.Message.ShouldContain(reason);
        message.Message.ShouldBe($"Failed to translate command: {reason}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DifferentReasonsCreateDifferentMessages()
    {
        // Act
        var message1 = new TranslationFailedMessage("Reason 1");
        var message2 = new TranslationFailedMessage("Reason 2");

        // Assert
        message1.Reason.ShouldBe("Reason 1");
        message2.Reason.ShouldBe("Reason 2");
        message1.Message.ShouldNotBe(message2.Message);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void InheritsFromCommandMessage()
    {
        // Act
        var message = new TranslationFailedMessage("Test");

        // Assert
        message.ShouldBeAssignableTo<CommandMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIGenericMessage()
    {
        // Act
        var message = new TranslationFailedMessage("Test");

        // Assert
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ReasonPropertyIsAccessible()
    {
        // Arrange
        var reason = "Custom reason";

        // Act
        var message = new TranslationFailedMessage(reason);

        // Assert
        message.Reason.ShouldBe(reason);
    }
}
