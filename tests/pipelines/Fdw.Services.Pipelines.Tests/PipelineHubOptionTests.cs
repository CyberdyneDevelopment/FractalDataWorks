using System.Linq;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// Covers <see cref="PipelineHubOption"/>: the fixed identity/route/hub-type/policy declared on
/// construction (mirrors <see cref="RealTimeHubs"/> TypeOption discovery contract), and that
/// <see cref="PipelineHubOption.RegisterServices"/> wires <see cref="IPipelineStatusBroadcaster"/> to
/// <see cref="PipelineStatusBroadcaster"/> as a scoped service.
/// </summary>
[Trait("Category", "CoreFramework")]
public sealed class PipelineHubOptionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorDeclaresRouteHubTypeAndDefaultAuthorizationPolicy()
    {
        // Act
        var option = new PipelineHubOption();
        option.Name.ShouldBe("Pipeline");
        option.Route.ShouldBe("/hubs/pipelines");
        option.HubType.ShouldBe(typeof(PipelineStatusHub));
        option.AuthorizationPolicy.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterServicesRegistersTheRoutingBroadcasterAsTheBroadcasterAndTheSignalRBroadcasterAsItself()
    {
        // Arrange
        var option = new PipelineHubOption();
        var services = new ServiceCollection();

        // Act
        option.RegisterServices(services, loggerFactory: null);

        // Assert
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPipelineStatusBroadcaster));
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationFactory.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        var hub = services.SingleOrDefault(d => d.ServiceType == typeof(PipelineStatusBroadcaster));
        hub.ShouldNotBeNull();
        hub!.ImplementationType.ShouldBe(typeof(PipelineStatusBroadcaster));
        hub.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
}
