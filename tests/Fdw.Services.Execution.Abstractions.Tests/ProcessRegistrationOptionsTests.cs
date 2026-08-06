using Fdw.Services.Execution.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests;

public class ProcessRegistrationOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsDefaultLifetime()
    {
        // Arrange & Act
        var options = new ProcessRegistrationOptions();

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void InheritsFromRegistrationOptions()
    {
        // Arrange & Act
        var options = new ProcessRegistrationOptions();

        // Assert
        options.ShouldBeAssignableTo<Fdw.ServiceTypes.RegistrationOptions>();
    }
}
