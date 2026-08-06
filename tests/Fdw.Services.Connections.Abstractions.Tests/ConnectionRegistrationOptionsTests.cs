using Fdw.ServiceTypes;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests for ConnectionRegistrationOptions.
/// </summary>
public class ConnectionRegistrationOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultConstructorSetsLifetimeToScoped()
    {
        // Act
        var options = new ConnectionRegistrationOptions();

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void InheritsFromRegistrationOptions()
    {
        // Act
        var options = new ConnectionRegistrationOptions();

        // Assert
        options.ShouldBeAssignableTo<RegistrationOptions>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TypeIsSealed()
    {
        // Act
        var type = typeof(ConnectionRegistrationOptions);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TypeIsPublic()
    {
        // Act
        var type = typeof(ConnectionRegistrationOptions);

        // Assert
        type.IsPublic.ShouldBeTrue();
    }
}
