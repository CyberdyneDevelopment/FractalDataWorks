using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Fdw.SignalR.Tests;

/// <summary>
/// Tests for the <see cref="RealTimeHubs"/> collection and its <see cref="RealTimeHubs.Register"/>
/// phase, using the four migrated domain hubs registered via the module initializer.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class RealTimeHubsCollectionTests
{
    [Fact]
    public void AllReturnsTheFourMigratedHubRoutes()
    {
        var routes = RealTimeHubs.All().Select(h => h.Route).ToList();

        routes.ShouldContain("/hubs/pipelines");
        routes.ShouldContain("/hubs/calculations");
        routes.ShouldContain("/hubs/schema-discovery");
        routes.ShouldContain("/hubs/messages");
    }

    [Fact]
    public void RegisterWithNullServicesThrows()
    {
        Should.Throw<ArgumentNullException>(() => RealTimeHubs.Register(null!));
    }

    [Fact]
    public void RegisterAddsSignalRServices()
    {
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;

        RealTimeHubs.Register(builder);

        services.ShouldContain(d =>
            d.ServiceType.FullName != null &&
            d.ServiceType.FullName.Contains("Microsoft.AspNetCore.SignalR", StringComparison.Ordinal));
    }

    [Fact]
    public void RegisterRegistersEachHubBroadcaster()
    {
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;

        RealTimeHubs.Register(builder);

        var serviceNames = services.Select(d => d.ServiceType.Name).ToList();
        serviceNames.ShouldContain("IPipelineStatusBroadcaster");
        serviceNames.ShouldContain("ICalculationNotifier");
        serviceNames.ShouldContain("ISchemaDiscoveryNotifier");
    }

    [Fact]
    public void RegisterWithLoggerFactoryStillRegistersHubs()
    {
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;

        // Passing a non-null logger factory exercises the registration-logging branch.
        RealTimeHubs.Register(builder, NullLoggerFactory.Instance);

        services.Select(d => d.ServiceType.Name).ShouldContain("IPipelineStatusBroadcaster");
    }
}
