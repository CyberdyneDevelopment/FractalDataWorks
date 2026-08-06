using Fdw.Services.Scheduling.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Configuration;

public class SchedulerRegistrationOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ConstructorSetsSingletonLifetime()
    {
        // Act
        var options = new SchedulerRegistrationOptions();

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void DefaultConstructorCreatesInstance()
    {
        // Act
        var options = new SchedulerRegistrationOptions();

        // Assert
        options.ShouldNotBeNull();
    }
}
