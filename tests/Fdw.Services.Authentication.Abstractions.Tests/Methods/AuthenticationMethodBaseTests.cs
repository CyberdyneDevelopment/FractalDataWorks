using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;

namespace Fdw.Services.Authentication.Abstractions.Tests.Methods;

/// <summary>
/// Tests for AuthenticationMethodBase class.
/// </summary>
public class AuthenticationMethodBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesAllProperties()
    {
        // Arrange & Act
        var method = new TestAuthenticationMethod(
            id: 1,
            name: "TestMethod",
            requiresUserInteraction: true,
            supportsTokenRefresh: true,
            supportsMultiTenant: false,
            authenticationScheme: "Bearer",
            priority: 10);

        // Assert
        method.Id.ShouldBe(1);
        method.Name.ShouldBe("TestMethod");
        method.RequiresUserInteraction.ShouldBeTrue();
        method.SupportsTokenRefresh.ShouldBeTrue();
        method.SupportsMultiTenant.ShouldBeFalse();
        method.AuthenticationScheme.ShouldBe("Bearer");
        method.Priority.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesWithNullAuthenticationScheme()
    {
        // Arrange & Act
        var method = new TestAuthenticationMethod(
            id: 2,
            name: "TestMethod2",
            requiresUserInteraction: false,
            supportsTokenRefresh: false,
            supportsMultiTenant: true,
            authenticationScheme: null,
            priority: 5);

        // Assert
        method.AuthenticationScheme.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void PropertiesAreReadOnly()
    {
        // Arrange
        var method = new TestAuthenticationMethod(
            id: 1,
            name: "Test",
            requiresUserInteraction: true,
            supportsTokenRefresh: true,
            supportsMultiTenant: true,
            authenticationScheme: "Test",
            priority: 1);

        // Assert - Properties should only have getters
        var type = typeof(IAuthenticationMethod);
        type.GetProperty(nameof(IAuthenticationMethod.RequiresUserInteraction))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(IAuthenticationMethod.SupportsTokenRefresh))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(IAuthenticationMethod.SupportsMultiTenant))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(IAuthenticationMethod.AuthenticationScheme))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(IAuthenticationMethod.Priority))!.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void MethodImplementsIAuthenticationMethod()
    {
        // Arrange
        var method = new TestAuthenticationMethod(1, "Test", true, true, true, "Test", 1);

        // Assert
        method.ShouldBeAssignableTo<IAuthenticationMethod>();
    }

    /// <summary>
    /// Testable implementation of AuthenticationMethodBase.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticationMethod : AuthenticationMethodBase
    {
        public TestAuthenticationMethod(
            int id,
            string name,
            bool requiresUserInteraction,
            bool supportsTokenRefresh,
            bool supportsMultiTenant,
            string? authenticationScheme,
            int priority)
            : base(id, name, requiresUserInteraction, supportsTokenRefresh, supportsMultiTenant, authenticationScheme, priority)
        {
        }
    }
}
