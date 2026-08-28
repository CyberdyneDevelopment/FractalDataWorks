using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.Http.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Tests.Security;

/// <summary>
/// Tests for <see cref="SystemAuthenticationContext"/> — the ONLY <see cref="IAuthenticationContext"/>
/// implementation that reports <see cref="IAuthenticationContext.IsSystemContext"/> = <c>true</c>.
/// </summary>
public class SystemAuthenticationContextTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsSystemContextIsAlwaysTrue()
    {
        // Arrange
        var context = new SystemAuthenticationContext();

        // Assert
        context.IsSystemContext.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsAuthenticatedIsAlwaysTrue()
    {
        // Arrange
        var context = new SystemAuthenticationContext();

        // Assert
        context.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ActiveTenantIdIsAlwaysNull()
    {
        // Arrange
        var context = new SystemAuthenticationContext();

        // Assert
        context.ActiveTenantId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void UserIdIsTheLiteralSystemString()
    {
        // Arrange
        var context = new SystemAuthenticationContext();

        // Assert
        context.UserId.ShouldBe("system");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UsernameMirrorsUserId()
    {
        // Arrange
        var context = new SystemAuthenticationContext();

        // Assert
        context.Username.ShouldBe(context.UserId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsCrossTenantIsAlwaysFalse()
    {
        // Arrange
        var context = new SystemAuthenticationContext();

        // Assert
        context.IsCrossTenant.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ActiveOrgIdIsAlwaysNull()
    {
        // Arrange
        var context = new SystemAuthenticationContext();

        // Assert
        context.ActiveOrgId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ExpiresAtIsAlwaysNull()
    {
        // Arrange
        var context = new SystemAuthenticationContext();

        // Assert
        context.ExpiresAt.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ClaimsIsEmptyNotNull()
    {
        // Arrange
        var context = new SystemAuthenticationContext();

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
        var context = new SystemAuthenticationContext();

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
        var context = new SystemAuthenticationContext();

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
        var context = new SystemAuthenticationContext();

        // Assert
        context.AuthenticationMethod.ShouldBe((SecurityMethodBase)SecurityMethods.ByName("None"));
    }
}
