using System;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.Http.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Tests.Security;

/// <summary>
/// Tests for <see cref="WorkAuthenticationContext"/> — the non-HTTP, work-scoped
/// <see cref="IAuthenticationContext"/> used by background executions.
/// </summary>
public class WorkAuthenticationContextTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ActiveTenantIdIsSetFromConstructor()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var context = new WorkAuthenticationContext(tenantId);

        // Assert
        context.ActiveTenantId.ShouldBe(tenantId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsCrossTenantIsAlwaysFalse()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid());

        // Assert
        context.IsCrossTenant.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsAuthenticatedIsAlwaysTrue()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid());

        // Assert
        context.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UserIdDefaultsToSystemWhenNotSupplied()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid());

        // Assert
        context.UserId.ShouldBe("system");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UserIdUsesSuppliedValueWhenPresent()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid(), "schedule-owner-id");

        // Assert
        context.UserId.ShouldBe("schedule-owner-id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UsernameMirrorsUserId()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid(), "schedule-owner-id");

        // Assert
        context.Username.ShouldBe(context.UserId);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ActiveOrgIdIsAlwaysNull()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid());

        // Assert
        context.ActiveOrgId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ExpiresAtIsAlwaysNull()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid());

        // Assert
        context.ExpiresAt.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ClaimsIsEmptyNotNull()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid());

        // Assert
        context.Claims.ShouldNotBeNull();
        context.Claims.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void RolesIsEmptyNotNull()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid());

        // Assert
        context.Roles.ShouldNotBeNull();
        context.Roles.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void PermissionsIsEmptyNotNull()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid());

        // Assert
        context.Permissions.ShouldNotBeNull();
        context.Permissions.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AuthenticationMethodIsNone()
    {
        // Arrange
        var context = new WorkAuthenticationContext(Guid.NewGuid());

        // Assert
        context.AuthenticationMethod.ShouldBe((SecurityMethodBase)SecurityMethods.ByName("None"));
    }
}
