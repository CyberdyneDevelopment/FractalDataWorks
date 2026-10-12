using Fdw.Messages;
using Fdw.Services.Authorization.Abstractions.Messages;

namespace Fdw.Services.Authorization.Abstractions.Tests.Messages;

/// <summary>
/// Tests for InsufficientPermissionsMessage.
/// </summary>
public class InsufficientPermissionsMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DefaultConstructorSetsPropertiesCorrectly()
    {
        // Arrange & Act
        var message = new InsufficientPermissionsMessage();

        // Assert
        message.Id.ShouldBe(3002);
        message.Name.ShouldBe("InsufficientPermissions");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("Insufficient permissions");
        message.Code.ShouldBe("INSUFFICIENT_PERMS");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContextConstructorSetsPropertiesCorrectly()
    {
        // Arrange & Act
        var message = new InsufficientPermissionsMessage("user456", "orders:delete");

        // Assert
        message.Id.ShouldBe(3002);
        message.Name.ShouldBe("InsufficientPermissions");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("User 'user456' lacks permission 'orders:delete'");
        message.Code.ShouldBe("INSUFFICIENT_PERMS");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContextConstructorFormatsMessageCorrectly()
    {
        // Arrange & Act
        var message = new InsufficientPermissionsMessage("admin@example.com", "products:manage");

        // Assert
        message.Message.ShouldContain("admin@example.com");
        message.Message.ShouldContain("products:manage");
    }
}
