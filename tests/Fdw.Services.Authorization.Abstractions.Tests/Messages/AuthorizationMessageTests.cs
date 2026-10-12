using Fdw.Messages;
using Fdw.Services.Authorization.Abstractions.Messages;

namespace Fdw.Services.Authorization.Abstractions.Tests.Messages;

/// <summary>
/// Tests for AuthorizationMessage base class.
/// </summary>
public class AuthorizationMessageTests
{
    private class TestAuthorizationMessage : AuthorizationMessage
    {
        public TestAuthorizationMessage(int id, string name, MessageSeverity severity, string message, string? code = null)
            : base(id, name, severity, message, code)
        {
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorSetsPropertiesCorrectly()
    {
        // Arrange & Act
        var message = new TestAuthorizationMessage(
            3001,
            "TestMessage",
            MessageSeverity.Warning,
            "Test message text",
            "TEST_CODE");

        // Assert
        message.Id.ShouldBe(3001);
        message.Name.ShouldBe("TestMessage");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("Test message text");
        message.Code.ShouldBe("TEST_CODE");
        message.Category.ShouldBe("Message");
        message.OriginatedIn.ShouldBe("Authorization");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CodeCanBeOmitted()
    {
        // Arrange & Act
        var message = new TestAuthorizationMessage(
            3001,
            "TestMessage",
            MessageSeverity.Error,
            "Test message",
            null);

        // Assert
        message.Code.ShouldBeNull();
    }
}
