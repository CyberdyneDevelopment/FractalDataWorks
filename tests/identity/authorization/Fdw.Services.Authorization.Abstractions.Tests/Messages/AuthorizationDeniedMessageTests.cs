using Fdw.Messages;
using Fdw.Services.Authorization.Abstractions.Messages;

namespace Fdw.Services.Authorization.Abstractions.Tests.Messages;

/// <summary>
/// Tests for AuthorizationDeniedMessage.
/// </summary>
public class AuthorizationDeniedMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DefaultConstructorSetsPropertiesCorrectly()
    {
        // Arrange & Act
        var message = new AuthorizationDeniedMessage();

        // Assert
        message.Id.ShouldBe(3001);
        message.Name.ShouldBe("AuthorizationDenied");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("Authorization denied");
        message.Code.ShouldBe("AUTH_DENIED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContextConstructorSetsPropertiesCorrectly()
    {
        // Arrange & Act
        var message = new AuthorizationDeniedMessage("user123", "orders", "create");

        // Assert
        message.Id.ShouldBe(3001);
        message.Name.ShouldBe("AuthorizationDenied");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("User 'user123' denied access to orders:create");
        message.Code.ShouldBe("AUTH_DENIED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContextConstructorFormatsMessageCorrectly()
    {
        // Arrange & Act
        var message = new AuthorizationDeniedMessage("admin@example.com", "products", "delete");

        // Assert
        message.Message.ShouldContain("admin@example.com");
        message.Message.ShouldContain("products:delete");
    }
}
