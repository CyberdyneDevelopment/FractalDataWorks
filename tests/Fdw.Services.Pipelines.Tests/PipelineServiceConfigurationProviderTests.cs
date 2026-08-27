using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// Covers <see cref="PipelineServiceConfigurationProvider.RegisterDomainConfiguration"/>: the header
/// provider must be resolvable under all three contracts it registers itself against (concrete type,
/// the open <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/> base, and
/// <see cref="IServiceConfigurationProvider{T}"/>) as the very same singleton instance, targeting this
/// domain's own default DataStore/path (no arguments — <see cref="RegisterDomainConfiguration"/> is
/// parameterless; overriding the location is <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}.SetConfiguration"/>
/// on the resolved instance, not a registration-time argument).
/// </summary>
[Trait("Category", "Configuration")]
public sealed class PipelineServiceConfigurationProviderTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void RegisterPublishesOneSingletonUnderTheDomainInterfaceAndEveryForward()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddLogging();
        // Why: the gateway is never dereferenced by this test - PipelineServiceConfigurationProvider's
        // constructor only stores the Lazy<T>, it does not resolve .Value.
        builder.Services.AddSingleton(new Lazy<IConfigurationGateway>(() =>
            throw new InvalidOperationException("gateway should not be dereferenced by DI resolution alone.")));

        // Act
        PipelineServiceTypes.Register(builder, NullLoggerFactory.Instance, force: true);

        // Assert
        using var provider = builder.Services.BuildServiceProvider();
        // Why the domain interface is asserted first: it is what the collection resolves to attach the
        // provider, so a registration that publishes only the concrete type leaves the domain unable to
        // resolve any configuration by name.
        var byInterface = provider.GetRequiredService<IPipelineConfigurationProvider>();
        var concrete = provider.GetRequiredService<PipelineServiceConfigurationProvider>();
        var asBase = provider.GetRequiredService<ImplementationConfigurationProviderBase<PipelineConfiguration, PipelineConfigurationCommand>>();
        var asServiceConfiguration = provider.GetRequiredService<IServiceConfigurationProvider<PipelineConfiguration>>();

        concrete.ShouldBeSameAs(byInterface);
        asBase.ShouldBeSameAs(concrete);
        asServiceConfiguration.ShouldBeSameAs(concrete);
        concrete.DataStoreName.ShouldBe(PipelineServiceTypes.ConfigurationConnection);
        concrete.PathName.ShouldBe("pipe");
    }
}
