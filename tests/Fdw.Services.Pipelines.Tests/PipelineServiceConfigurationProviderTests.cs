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
using Fdw.Services.Data;

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
        builder.Services.AddSingleton<IConfigurationGatewayProvider>(new ConfigurationGatewayProvider());
        // A test is a host: the operational connection has no default, so it has to be named here for
        // the same reason a real host has to name it.
        PipelineServiceTypes.OperationalConnection = "OpsDb";

        // Act
        PipelineServiceTypes.Register(builder, NullLoggerFactory.Instance, force: true);

        // Assert
        using var provider = builder.Services.BuildServiceProvider();
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

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void RegisterSucceedsWhenNoOperationalConnectionIsNamed()
    {
        // Every host registers this domain as part of the platform sweep, including hosts that never
        // read its operational data. Refusing to register them took every reference host down at boot
        // once already; this is the regression guard for that, not a preference.
        var previous = PipelineServiceTypes.OperationalConnection;
        try
        {
            PipelineServiceTypes.OperationalConnection = null;
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddLogging();
            builder.Services.AddSingleton<IConfigurationGatewayProvider>(new ConfigurationGatewayProvider());

            // Act
            var result = PipelineServiceTypes.Register(builder, NullLoggerFactory.Instance, force: true);

            // Assert -- registration completes; an unnamed store is reported by the sites that read it.
            result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        }
        finally
        {
            // Static state on a collection outlives the test that set it.
            PipelineServiceTypes.OperationalConnection = previous;
        }
    }
}
