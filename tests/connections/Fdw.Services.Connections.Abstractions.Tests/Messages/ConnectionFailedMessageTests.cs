using Fdw.Messages;
using Fdw.Services.Connections.Abstractions.Messages;

namespace Fdw.Services.Connections.Abstractions.Tests.Messages;

/// <summary>
/// Tests for ConnectionFailedMessage.
/// </summary>
public class ConnectionFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultConstructorCreatesMessageWithCorrectProperties()
    {
        // Act
        var message = new ConnectionFailedMessage();

        // Assert
        message.Id.ShouldBe(3001);
        message.Name.ShouldBe("ConnectionFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to establish connection");
        message.Code.ShouldBe("CONN_FAILED");
        message.Category.ShouldBe("Message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorWithReasonCreatesMessageWithCustomMessage()
    {
        // Arrange
        var reason = "Timeout occurred";

        // Act
        var message = new ConnectionFailedMessage(reason);

        // Assert
        message.Id.ShouldBe(3001);
        message.Name.ShouldBe("ConnectionFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe($"Failed to establish connection: {reason}");
        message.Code.ShouldBe("CONN_FAILED");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void MessageWithEmptyReasonStillFormatsCorrectly()
    {
        // Act
        var message = new ConnectionFailedMessage(string.Empty);

        // Assert
        message.Message.ShouldBe("Failed to establish connection: ");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void MessageImplementsIGenericMessage()
    {
        // Act
        var message = new ConnectionFailedMessage();

        // Assert
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void MessageInheritsFromConnectionMessage()
    {
        // Act
        var message = new ConnectionFailedMessage();

        // Assert
        message.ShouldBeAssignableTo<ConnectionMessage>();
    }
}
