using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// The publisher of the components of runs in flight (FDW-3778): it sends a run's list only when it changed, names the tenant, stops
/// sending for a run that ended, and sends nothing for a run that waits or that the host does not follow.
/// </summary>
public sealed class PipelineComponentsPublisherTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly PipelineRunLimitValues Limits = new(8, 8);
    private static readonly FlowLink SrcToDst = new("src", "output", "dst", "input");

    private static PipelineRunIdentity Run() => new(Guid.NewGuid(), Guid.NewGuid(), "Orders", Tenant);

    private sealed class Counted
    {
        public long Rows { get; set; }
    }

    private static (PipelineComponentsPublisher Publisher, Mock<IPipelineStatusBroadcaster> Broadcaster) Publisher(PipelineRunScheduler scheduler, RunningFlows flows)
    {
        var broadcaster = new Mock<IPipelineStatusBroadcaster>();
        broadcaster.Setup(b => b.BroadcastComponents(It.IsAny<PipelineComponentsUpdate>(), It.IsAny<Guid?>())).Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddScoped(_ => broadcaster.Object);
        var scopes = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        return (new PipelineComponentsPublisher(scheduler, flows, Mock.Of<IEffectiveSettingsProvider>(), scopes), broadcaster);
    }

    private static async Task<(PipelineRunScheduler Scheduler, PipelineRunIdentity Run)> Running()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var run = Run();
        await scheduler.Enter(PipelineRunClaim.Writing(run, ["store//t"], Limits, null), TestContext.Current.CancellationToken);
        return (scheduler, run);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AChangedListIsBroadcastOnceWithTheTenantAndAnUnchangedOneIsNot()
    {
        var (scheduler, run) = await Running();
        var flows = new RunningFlows();
        var counted = new Counted { Rows = 5 };
        using var live = flows.Begin(run.ExecutionId);
        using var graph = live.Open([new("src", 0, true), new("dst", 1, false)], () => [new FlowLinkCount(SrcToDst, counted.Rows, 1, 0)]);
        graph.ComponentStarted("src");
        graph.ComponentStarted("dst");
        var (publisher, broadcaster) = Publisher(scheduler, flows);
        var sent = new Dictionary<Guid, string>();

        await publisher.PublishOnce(sent);
        await publisher.PublishOnce(sent);
        counted.Rows = 9;
        await publisher.PublishOnce(sent);

        broadcaster.Verify(b => b.BroadcastComponents(It.Is<PipelineComponentsUpdate>(u => u.ExecutionId == run.ExecutionId && u.PipelineName == "Orders"), Tenant), Times.Exactly(2));
        broadcaster.Verify(b => b.BroadcastComponents(It.Is<PipelineComponentsUpdate>(u => u.Links.Single().Rows == 9 && u.Components.Single(c => c.Name == "dst").RowsIn == 9), Tenant), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ARunTheHostDoesNotFollowSendsNothing()
    {
        var (scheduler, _) = await Running();
        var (publisher, broadcaster) = Publisher(scheduler, new RunningFlows());

        await publisher.PublishOnce([]);

        broadcaster.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ARunThatEndedIsDroppedSoALaterRunOfTheSameIdSendsAgain()
    {
        var (scheduler, run) = await Running();
        var flows = new RunningFlows();
        var (publisher, _) = Publisher(scheduler, flows);
        var sent = new Dictionary<Guid, string>();
        using (var live = flows.Begin(run.ExecutionId))
        using (var graph = live.Open([new("src", 0, true)], () => []))
        {
            graph.ComponentStarted("src");
            await publisher.PublishOnce(sent);
        }

        sent.ShouldContainKey(run.ExecutionId);
        var (idle, _) = Publisher(new PipelineRunScheduler(TimeProvider.System), flows);
        await idle.PublishOnce(sent);

        sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ABroadcastThatThrowsIsLoggedAndTheNextTickTriesAgain()
    {
        var (scheduler, run) = await Running();
        var flows = new RunningFlows();
        using var live = flows.Begin(run.ExecutionId);
        using var graph = live.Open([new("src", 0, true)], () => []);
        graph.ComponentStarted("src");
        var (publisher, broadcaster) = Publisher(scheduler, flows);
        broadcaster.Setup(b => b.BroadcastComponents(It.IsAny<PipelineComponentsUpdate>(), It.IsAny<Guid?>())).ThrowsAsync(new InvalidOperationException("down"));
        var sent = new Dictionary<Guid, string>();

        await Should.NotThrowAsync(() => publisher.PublishOnce(sent));
        await publisher.PublishOnce(sent);

        sent.ShouldBeEmpty();
        broadcaster.Verify(b => b.BroadcastComponents(It.IsAny<PipelineComponentsUpdate>(), It.IsAny<Guid?>()), Times.Exactly(2));
    }
}
