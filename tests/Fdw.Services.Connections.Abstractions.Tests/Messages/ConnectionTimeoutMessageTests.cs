using Fdw.Messages;
using Fdw.Services.Connections.Abstractions.Messages;

namespace Fdw.Services.Connections.Abstractions.Tests.Messages;

/// <summary>
/// Tests for ConnectionTimeoutMessage.
/// </summary>
public class ConnectionTimeoutMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultConstructorCreatesMessageWithCorrectProperties()
    {
        // Act
        var message = new ConnectionTimeoutMessage();

        // Assert
        message.Id.ShouldBe(3002);
        message.Name.ShouldBe("ConnectionTimeout");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("Connection attempt timed out");
        message.Code.ShouldBe("CONN_TIMEOUT");
        message.Category.ShouldBe("Message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorWithTimeoutCreatesMessageWithCustomMessage()
    {
        // Arrange
        var timeoutSeconds = 30;

        // Act
        var message = new ConnectionTimeoutMessage(timeoutSeconds);

        // Assert
        message.Id.ShouldBe(3002);
        message.Name.ShouldBe("ConnectionTimeout");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe($"Connection attempt timed out after {timeoutSeconds} seconds");
        message.Code.ShouldBe("CONN_TIMEOUT");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(300)]
    public void MessageFormatsTimeoutCorrectly(int timeoutSeconds)
    {
        // Act
        var message = new ConnectionTimeoutMessage(timeoutSeconds);

        // Assert
        message.Message.ShouldContain(timeoutSeconds.ToString());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void MessageImplementsIGenericMessage()
    {
        // Act
        var message = new ConnectionTimeoutMessage();

        // Assert
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void MessageInheritsFromConnectionMessage()
    {
        // Act
        var message = new ConnectionTimeoutMessage();

        // Assert
        message.ShouldBeAssignableTo<ConnectionMessage>();
    }
}
