using Fdw.Messages;
using Fdw.Services.Authorization.Abstractions.Messages;

namespace Fdw.Services.Authorization.Abstractions.Tests.Messages;

/// <summary>
/// Tests for TenantAccessDeniedMessage.
/// </summary>
public class TenantAccessDeniedMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DefaultConstructorSetsPropertiesCorrectly()
    {
        // Arrange & Act
        var message = new TenantAccessDeniedMessage();

        // Assert
        message.Id.ShouldBe(3004);
        message.Name.ShouldBe("TenantAccessDenied");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("Tenant access denied");
        message.Code.ShouldBe("TENANT_DENIED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContextConstructorSetsPropertiesCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var message = new TenantAccessDeniedMessage("user101", tenantId);

        // Assert
        message.Id.ShouldBe(3004);
        message.Name.ShouldBe("TenantAccessDenied");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe($"User 'user101' denied access to tenant '{tenantId}'");
        message.Code.ShouldBe("TENANT_DENIED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContextConstructorFormatsMessageCorrectly()
    {
        // Arrange
        var tenantId = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var message = new TenantAccessDeniedMessage("admin@example.com", tenantId);

        // Assert
        message.Message.ShouldContain("admin@example.com");
        message.Message.ShouldContain(tenantId.ToString());
    }
}
