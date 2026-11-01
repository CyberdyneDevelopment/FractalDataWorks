using Microsoft.Extensions.Logging;
using Fdw.Services.Authorization.Abstractions.Logging;

namespace Fdw.Services.Authorization.Abstractions.Tests.Logging;

/// <summary>
/// Tests for AuthorizationLogger MessageLogging methods.
/// </summary>
public class AuthorizationLoggerTests
{
    private readonly Mock<ILogger> _mockLogger;

    public AuthorizationLoggerTests()
    {
        _mockLogger = new Mock<ILogger>();
        // Setup IsEnabled to return true for all levels by default
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AuthorizationDeniedLogsCorrectly()
    {
        // Act
        var result = AuthorizationLogger.AuthorizationDenied(
            _mockLogger.Object,
            "user123",
            "orders",
            "create");

        // Assert
        result.ShouldNotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                new EventId(51000),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AuthorizationGrantedLogsCorrectly()
    {
        // Act
        var result = AuthorizationLogger.AuthorizationGranted(
            _mockLogger.Object,
            "user456",
            "products",
            "read");

        // Assert
        result.ShouldNotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                new EventId(11000),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void InsufficientPermissionsLogsCorrectly()
    {
        // Act
        var result = AuthorizationLogger.InsufficientPermissions(
            _mockLogger.Object,
            "user789",
            "orders:delete");

        // Assert
        result.ShouldNotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                new EventId(51001),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RoleRequiredLogsCorrectly()
    {
        // Act
        var result = AuthorizationLogger.RoleRequired(
            _mockLogger.Object,
            "user101",
            "Admin");

        // Assert
        result.ShouldNotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                new EventId(51002),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TenantAccessDeniedLogsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = AuthorizationLogger.TenantAccessDenied(
            _mockLogger.Object,
            "user202",
            tenantId);

        // Assert
        result.ShouldNotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                new EventId(51003),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TenantContextSetLogsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = AuthorizationLogger.TenantContextSet(
            _mockLogger.Object,
            tenantId,
            "acme-corp");

        // Assert
        result.ShouldNotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                new EventId(11001),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SecurityAuditLogsCorrectly()
    {
        // Act
        var result = AuthorizationLogger.SecurityAudit(
            _mockLogger.Object,
            "LoginAttempt",
            "user303",
            "authentication");

        // Assert
        result.ShouldNotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                new EventId(11002),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SecurityAuditWithTenantLogsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = AuthorizationLogger.SecurityAuditWithTenant(
            _mockLogger.Object,
            "DataAccess",
            "user404",
            "database",
            tenantId);

        // Assert
        result.ShouldNotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                new EventId(11003),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CheckingPermissionLogsCorrectly()
    {
        // Act
        var result = AuthorizationLogger.CheckingPermission(
            _mockLogger.Object,
            "orders:create",
            "user505");

        // Assert
        result.ShouldNotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                new EventId(11004),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CheckingRoleLogsCorrectly()
    {
        // Act
        var result = AuthorizationLogger.CheckingRole(
            _mockLogger.Object,
            "Manager",
            "user606");

        // Assert
        result.ShouldNotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                new EventId(11005),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
