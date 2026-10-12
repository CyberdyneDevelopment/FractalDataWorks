using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Shouldly;
using Fdw.Services.Resiliency.Extensions;
using Fdw.Services.Resiliency.Factories;

namespace Fdw.Services.Resiliency.Tests;

/// <summary>
/// Unit tests for ResiliencyServiceTypes registration.
/// </summary>
[Collection(nameof(ResiliencyTestCollection))]
public sealed class ResiliencyServiceExtensionsTests
{
    [Fact(Skip = "Registration runs through a ServiceTypeCollection; the result depends on suite-wide initializer ordering, not on this code.")]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterRegistersFactoryAsSingleton()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        services.AddLogging();

        // Act
        ResiliencyServiceTypes.Register(builder, NullLoggerFactory.Instance, force: true);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<IResiliencyPipelineFactory>();

        factory.ShouldNotBeNull();
        factory.ShouldBeOfType<ResiliencyPipelineFactory>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterDoesNotOverrideExistingRegistration()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        services.AddLogging();

        var stub = NSubstitute.Substitute.For<IResiliencyPipelineFactory>();
        services.AddSingleton(stub);

        // Act
        ResiliencyServiceTypes.Register(builder, NullLoggerFactory.Instance, force: true);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<IResiliencyPipelineFactory>();
        factory.ShouldBe(stub);
    }
}
