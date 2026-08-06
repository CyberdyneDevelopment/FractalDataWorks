using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Commands;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// Covers <see cref="PipelineServiceConfigurationProvider.RegisterDomainConfiguration"/>: the header
/// provider must be resolvable under all three contracts it registers itself against (concrete type,
/// the open <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/> base, and
/// <see cref="IServiceConfigurationProvider{T}"/>) as the very same singleton instance, targeting this
/// domain's own default DataStore/path (no arguments — <see cref="RegisterDomainConfiguration"/> is
/// parameterless; overriding the location is <see cref="DefaultConfigurationProvider{TConfig,TCommand}.SetConfiguration"/>
/// on the resolved instance, not a registration-time argument).
/// </summary>
[Trait("Category", "Configuration")]
public sealed class PipelineServiceConfigurationProviderTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void RegisterDomainConfigurationRegistersTheSameSingletonUnderAllThreeContracts()
    {
        // Arrange
        var services = new ServiceCollection();
        // Why: the gateway is never dereferenced by this test - PipelineServiceConfigurationProvider's
        // constructor only stores the Lazy<T>, it does not resolve .Value.
        services.AddSingleton(new Lazy<IConfigurationGateway>(() =>
            throw new InvalidOperationException("gateway should not be dereferenced by DI resolution alone.")));

        // Act
        PipelineServiceConfigurationProvider.RegisterDomainConfiguration(services);

        // Assert
        using var provider = services.BuildServiceProvider();
        var concrete = provider.GetRequiredService<PipelineServiceConfigurationProvider>();
        var asBase = provider.GetRequiredService<DefaultConfigurationProvider<PipelineConfiguration, PipelineConfigurationCommand>>();
        var asInterface = provider.GetRequiredService<IServiceConfigurationProvider<PipelineConfiguration>>();

        asBase.ShouldBeSameAs(concrete);
        asInterface.ShouldBeSameAs(concrete);
        concrete.DataStoreName.ShouldBe("ConfigurationDb");
        concrete.PathName.ShouldBe("pipe");
    }
}
