using Fdw.Messages;
using Fdw.Services.Authorization.Abstractions.Messages;

namespace Fdw.Services.Authorization.Abstractions.Tests.Messages;

/// <summary>
/// Tests for RoleRequiredMessage.
/// </summary>
public class RoleRequiredMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DefaultConstructorSetsPropertiesCorrectly()
    {
        // Arrange & Act
        var message = new RoleRequiredMessage();

        // Assert
        message.Id.ShouldBe(3003);
        message.Name.ShouldBe("RoleRequired");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("Required role not found");
        message.Code.ShouldBe("ROLE_REQUIRED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContextConstructorSetsPropertiesCorrectly()
    {
        // Arrange & Act
        var message = new RoleRequiredMessage("user789", "Admin");

        // Assert
        message.Id.ShouldBe(3003);
        message.Name.ShouldBe("RoleRequired");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("User 'user789' requires role 'Admin'");
        message.Code.ShouldBe("ROLE_REQUIRED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContextConstructorFormatsMessageCorrectly()
    {
        // Arrange & Act
        var message = new RoleRequiredMessage("guest@example.com", "Manager");

        // Assert
        message.Message.ShouldContain("guest@example.com");
        message.Message.ShouldContain("Manager");
    }
}
