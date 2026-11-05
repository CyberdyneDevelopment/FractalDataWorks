using System.Linq;
using Fdw.Services.Etl.Thresholds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Etl.Tests;

/// <summary>
/// Tests for which hosts host the services that run pipelines (FDW-3975): the queue consumer with its run recovery, the components publisher, the wait monitor's loop and the
/// escalation timer are registered only by a host that names its operational store; a host that does not name it (the scheduler) registers none of them.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class EtlPipelineTypesHostedServicesTests : IDisposable
{
    private readonly string? _before = EtlPipelineTypes.OperationalConnection;

    public void Dispose() => EtlPipelineTypes.OperationalConnection = _before;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AHostThatNamesNoOperationalStoreRegistersNoHostedServiceThatRunsPipelines()
    {
        EtlPipelineTypes.OperationalConnection = null;
        var services = new ServiceCollection();

        EtlPipelineTypes.RegisterPipelineExecutionQueue(services);

        services.Count(descriptor => descriptor.ServiceType == typeof(IHostedService)).ShouldBe(0);
        services.Any(descriptor => descriptor.ServiceType == typeof(WaitThresholdMonitor)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AHostThatNamesItsOperationalStoreRegistersTheFourHostedServices()
    {
        EtlPipelineTypes.OperationalConnection = "OpsDb";
        var services = new ServiceCollection();

        EtlPipelineTypes.RegisterPipelineExecutionQueue(services);

        services.Count(descriptor => descriptor.ServiceType == typeof(IHostedService)).ShouldBe(4);
    }
}
