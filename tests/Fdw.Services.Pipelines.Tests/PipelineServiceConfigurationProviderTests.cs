using Fdw.Configuration;
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
/// Covers the registration of <see cref="PipelineServiceConfigurationProvider"/>: one singleton is
/// published under the domain interface and every forward, and registering succeeds when no
/// operational connection is named.
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
        // A domain provider since the domain/implementation split: it is published under the domain
        // contract, not the implementation-provider names it carried before.
        var asDomain = provider.GetRequiredService<IDomainConfigurationProvider<IPipelineImplementationConfiguration>>();

        concrete.ShouldBeSameAs(byInterface);
        asDomain.ShouldBeSameAs(concrete);
        concrete.DataStoreName.ShouldBe(PipelineServiceTypes.ConfigurationConnection);
        concrete.PathName.ShouldBe("pipe");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void RegisterSucceedsWhenNoOperationalConnectionIsNamed()
    {
        // Every host registers this domain as part of the platform bootstrap, including hosts that never
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
