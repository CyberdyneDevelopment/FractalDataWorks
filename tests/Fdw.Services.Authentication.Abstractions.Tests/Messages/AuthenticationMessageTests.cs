using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Messages;

namespace Fdw.Services.Authentication.Abstractions.Tests.Messages;

/// <summary>
/// Tests for AuthenticationMessage base class.
/// </summary>
public class AuthenticationMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesAllProperties()
    {
        // Arrange & Act
        var message = new TestAuthenticationMessage(
            id: 1000,
            name: "TestMessage",
            severity: MessageSeverity.Error,
            message: "Test error message",
            code: "TEST_ERROR");

        // Assert
        message.Id.ShouldBe(1000);
        message.Name.ShouldBe("TestMessage");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Test error message");
        message.Code.ShouldBe("TEST_ERROR");
        message.Source.ShouldBe("Authentication");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesWithNullCode()
    {
        // Arrange & Act
        var message = new TestAuthenticationMessage(
            id: 1001,
            name: "TestMessage2",
            severity: MessageSeverity.Warning,
            message: "Test warning",
            code: null);

        // Assert
        message.Code.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(MessageSeverity.Debug)]
    [InlineData(MessageSeverity.Information)]
    [InlineData(MessageSeverity.Warning)]
    [InlineData(MessageSeverity.Error)]
    [InlineData(MessageSeverity.Critical)]
    public void ConstructorInitializesWithVariousSeverities(MessageSeverity severity)
    {
        // Arrange & Act
        var message = new TestAuthenticationMessage(1, "Test", severity, "Test", null);

        // Assert
        message.Severity.ShouldBe(severity);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void MessageImplementsIServiceMessage()
    {
        // Arrange
        var message = new TestAuthenticationMessage(1, "Test", MessageSeverity.Information, "Test", null);

        // Assert
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void MessageIsAbstractClass()
    {
        // Arrange
        var type = typeof(AuthenticationMessage);

        // Assert
        type.IsAbstract.ShouldBeTrue();
        type.IsClass.ShouldBeTrue();
    }

    /// <summary>
    /// Testable implementation of AuthenticationMessage.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticationMessage : AuthenticationMessage
    {
        public TestAuthenticationMessage(int id, string name, MessageSeverity severity, string message, string? code)
            : base(id, name, severity, message, code)
        {
        }
    }
}
