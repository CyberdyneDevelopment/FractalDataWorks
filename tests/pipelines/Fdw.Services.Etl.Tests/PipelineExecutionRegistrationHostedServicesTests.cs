using System.Linq;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Thresholds;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Services.Pipelines;

namespace Fdw.Services.Etl.Tests;

/// <summary>
/// Tests for which hosts host the services that run pipelines (FDW-3975): the queue consumer with its run recovery, the components publisher, the wait monitor's loop, the
/// escalation timer and the holder of the runs that start at once are registered only by a host that names its operational store; a host that does not name it (the scheduler) registers none of them.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineExecutionRegistrationHostedServicesTests : IDisposable
{
    private readonly string? _before = PipelineTypes.OperationalConnection;

    public void Dispose() => PipelineTypes.OperationalConnection = _before;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AHostThatNamesNoOperationalStoreRegistersNoHostedServiceThatRunsPipelines()
    {
        PipelineTypes.OperationalConnection = null;
        var services = new ServiceCollection();

        PipelineExecutionRegistration.RegisterPipelineExecutionQueue(services);

        services.Count(descriptor => descriptor.ServiceType == typeof(IHostedService)).ShouldBe(0);
        services.Any(descriptor => descriptor.ServiceType == typeof(WaitThresholdMonitor)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AHostThatNamesItsOperationalStoreRegistersTheSixHostedServices()
    {
        PipelineTypes.OperationalConnection = "OpsDb";
        var services = new ServiceCollection();

        PipelineExecutionRegistration.RegisterPipelineExecutionQueue(services);

        // Six since FDW-6986: the sixth is the recovery of at-once runs that lost their claim (PipelineAtOnceRecoveryService), beside the five this test was written for.
        services.Count(descriptor => descriptor.ServiceType == typeof(IHostedService)).ShouldBe(6);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AHostThatRunsPipelinesCarriesOutTheAtOnceRunModeBesideTheQueuedOne()
    {
        PipelineTypes.OperationalConnection = "OpsDb";
        var services = new ServiceCollection();

        PipelineExecutionRegistration.RegisterPipelineExecutionQueue(services);

        // A pipeline type and the runner of its run mode are never separated by a package boundary (FDW-6141): the Interactive type is in this assembly, so its runner is too.
        services.Where(descriptor => descriptor.ServiceType == typeof(IPipelineRunner)).Select(descriptor => descriptor.ImplementationType)
            .ShouldBe(new Type?[] { typeof(QueuedPipelineRunner), typeof(AtOnceRunner) }, ignoreOrder: true);
    }
}
