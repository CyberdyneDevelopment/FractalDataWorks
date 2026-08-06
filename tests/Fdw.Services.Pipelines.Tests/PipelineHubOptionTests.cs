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

        // Assert
        option.Id.ShouldBe(1);
        option.Name.ShouldBe("Pipeline");
        option.Route.ShouldBe("/hubs/pipelines");
        option.HubType.ShouldBe(typeof(PipelineStatusHub));
        // Why: null means "require the default authenticated-principal policy" (see
        // RealTimeHubOptionBase.MapHubAt) - never anonymous access.
        option.AuthorizationPolicy.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterServicesRegistersPipelineStatusBroadcasterAsScoped()
    {
        // Arrange
        var option = new PipelineHubOption();
        var services = new ServiceCollection();

        // Act
        option.RegisterServices(services, loggerFactory: null);

        // Assert
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPipelineStatusBroadcaster));
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(PipelineStatusBroadcaster));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
}
