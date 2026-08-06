using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Tests.Security;

/// <summary>
/// Tests for authentication security interfaces.
/// </summary>
public class SecurityInterfaceTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IAuthenticationContextInterfaceExists()
    {
        // Arrange
        var type = typeof(IAuthenticationContext);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IAuthenticationContextHasUserIdProperty()
    {
        // Arrange
        var type = typeof(IAuthenticationContext);

        // Assert
        type.GetProperty("UserId").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IAuthenticationContextHasUsernameProperty()
    {
        // Arrange
        var type = typeof(IAuthenticationContext);

        // Assert
        type.GetProperty("Username").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IAuthenticationContextHasIsAuthenticatedProperty()
    {
        // Arrange
        var type = typeof(IAuthenticationContext);

        // Assert
        type.GetProperty("IsAuthenticated").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IFrameworkAuthorizationServiceInterfaceExists()
    {
        // Arrange
        var type = typeof(IFrameworkAuthorizationService);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IFrameworkAuthorizationServiceHasAuthorizeMethod()
    {
        // Arrange
        var type = typeof(IFrameworkAuthorizationService);

        // Assert
        type.GetMethod("Authorize").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IRoleProviderInterfaceExists()
    {
        // Arrange
        var type = typeof(IRoleProvider);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IRoleProviderHasGetUserRolesMethod()
    {
        // Arrange
        var type = typeof(IRoleProvider);

        // Assert
        type.GetMethod("GetUserRoles").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ISecurityTokenServiceInterfaceExists()
    {
        // Arrange
        var type = typeof(ISecurityTokenService);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ISecurityTokenServiceHasGenerateTokenMethod()
    {
        // Arrange
        var type = typeof(ISecurityTokenService);

        // Assert
        type.GetMethod("GenerateToken").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ISecurityTokenServiceHasValidateTokenMethod()
    {
        // Arrange
        var type = typeof(ISecurityTokenService);

        // Assert
        type.GetMethod("ValidateToken").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ITokenTypeInterfaceExists()
    {
        // Arrange
        var type = typeof(ITokenType);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ITokenTypeHasFormatProperty()
    {
        // Arrange
        var type = typeof(ITokenType);

        // Assert
        type.GetProperty("Format").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ITokenTypeHasCanBeRefreshedProperty()
    {
        // Arrange
        var type = typeof(ITokenType);

        // Assert
        type.GetProperty("CanBeRefreshed").ShouldNotBeNull();
    }
}
