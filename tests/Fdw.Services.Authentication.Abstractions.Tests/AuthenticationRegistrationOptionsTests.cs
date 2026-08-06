using Fdw.Services.Authentication.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Authentication.Abstractions.Tests;

/// <summary>
/// Tests for AuthenticationRegistrationOptions class.
/// </summary>
public class AuthenticationRegistrationOptionsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesWithScopedLifetime()
    {
        // Act
        var options = new AuthenticationRegistrationOptions();

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void OptionsInheritsFromRegistrationOptions()
    {
        // Arrange
        var type = typeof(AuthenticationRegistrationOptions);

        // Assert
        type.BaseType.ShouldNotBeNull();
        type.BaseType!.Name.ShouldBe("RegistrationOptions");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void OptionsIsSealedClass()
    {
        // Arrange
        var type = typeof(AuthenticationRegistrationOptions);

        // Assert
        type.IsSealed.ShouldBeTrue();
        type.IsClass.ShouldBeTrue();
    }
}
