using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Data.Abstractions.Tests;

public class DataRegistrationOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsLifetimeToScoped()
    {
        // Arrange & Act
        var result = new DataRegistrationOptions();

        // Assert
        result.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromRegistrationOptions()
    {
        // Arrange & Act
        var result = new DataRegistrationOptions();

        // Assert
        result.ShouldBeAssignableTo<ServiceTypes.RegistrationOptions>();
    }
}
