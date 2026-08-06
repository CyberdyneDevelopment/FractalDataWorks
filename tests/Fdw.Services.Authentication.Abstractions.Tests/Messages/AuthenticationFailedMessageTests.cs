using Fdw.Messages;
using Fdw.Services.Authentication.Abstractions.Messages;

namespace Fdw.Services.Authentication.Abstractions.Tests.Messages;

/// <summary>
/// Tests for AuthenticationFailedMessage class.
/// </summary>
public class AuthenticationFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DefaultConstructorInitializesCorrectly()
    {
        // Arrange & Act
        var message = new AuthenticationFailedMessage();

        // Assert
        message.Id.ShouldBe(2003);
        message.Name.ShouldBe("AuthenticationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Authentication failed");
        message.Code.ShouldBe("AUTH_FAILED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorWithReasonInitializesCorrectly()
    {
        // Arrange
        var reason = "Invalid credentials";

        // Act
        var message = new AuthenticationFailedMessage(reason);

        // Assert
        message.Id.ShouldBe(2003);
        message.Name.ShouldBe("AuthenticationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Authentication failed: Invalid credentials");
        message.Code.ShouldBe("AUTH_FAILED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void MessageIsSealedClass()
    {
        // Arrange
        var type = typeof(AuthenticationFailedMessage);

        // Assert
        type.IsSealed.ShouldBeTrue();
        type.IsClass.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void MessageInheritsFromAuthenticationMessage()
    {
        // Arrange
        var message = new AuthenticationFailedMessage();

        // Assert
        message.ShouldBeAssignableTo<AuthenticationMessage>();
    }
}
