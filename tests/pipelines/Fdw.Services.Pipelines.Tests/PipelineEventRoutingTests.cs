using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Configuration;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// The routing broadcaster every host registers (FDW-3773): a host whose Host row serves the hubs broadcasts through
/// SignalR, any other host relays through the pipeline event client, and a host that can do neither drops the event
/// with a logged failure and never throws into the run.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class PipelineEventRoutingTests
{
    private const string HostName = "FdwReference.Etl.Server";
    private static readonly Guid Execution = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Org = new("bbbbbbbb-0000-0000-0000-000000000001");

    private static (PipelineStatusBroadcaster Hub, List<string> Groups) SignalR()
    {
        var targeted = new List<string>();
        var client = new Mock<IPipelineStatusHubClient>();
        var clients = new Mock<IHubClients<IPipelineStatusHubClient>>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns((string g) => { targeted.Add(g); return client.Object; });
        var hubContext = new Mock<IHubContext<PipelineStatusHub, IPipelineStatusHubClient>>();
        hubContext.SetupGet(h => h.Clients).Returns(clients.Object);
        return (new PipelineStatusBroadcaster(hubContext.Object), targeted);
    }

    private static Mock<IHostConfigurationProvider> Hosts(bool? servesHubs)
    {
        var hosts = new Mock<IHostConfigurationProvider>();
        hosts.Setup(h => h.Get(HostName, It.IsAny<CancellationToken>())).ReturnsAsync(servesHubs is null
            ? GenericResult<IHostImplementationConfiguration>.Failure(new GenericMessage("no Host row"))
            : GenericResult<IHostImplementationConfiguration>.Success(new HostImplementationConfiguration { ServesRealTimeHubs = servesHubs.Value }));
        return hosts;
    }

    private static Mock<IHostEnvironment> Environment()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.ApplicationName).Returns(HostName);
        return environment;
    }

    private static PipelineEventRelaySwitch Switch(bool? on)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        var value = on ?? false;
        settings.Setup(x => x.TryGetEffectiveValue<bool>(SettingDefinitions.PipelineEventRelayEnabled, out value, It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(on is not null);
        return new PipelineEventRelaySwitch(settings.Object);
    }

    [Fact]
    public async Task AHostThatServesNoHubAndHasTheRelaySwitchedOffPublishesNothing()
    {
        var (hub, groups) = SignalR();
        var client = new Mock<IPipelineEventClient>(MockBehavior.Strict);
        var routing = new RoutingPipelineStatusBroadcaster(hub, Hosts(false).Object, Environment().Object, Switch(false), clientFactory: () => client.Object);

        await routing.BroadcastStatusChange("nfl", Execution, "Running");
        await routing.BroadcastExecutionPaused(Execution);

        groups.ShouldBeEmpty();
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AHostThatServesNoHubAndHasNoRelaySettingPublishesNothingAndDoesNotAssumeAValue()
    {
        var (hub, groups) = SignalR();
        var client = new Mock<IPipelineEventClient>(MockBehavior.Strict);
        var routing = new RoutingPipelineStatusBroadcaster(hub, Hosts(false).Object, Environment().Object, Switch(null), clientFactory: () => client.Object);

        await Should.NotThrowAsync(() => routing.BroadcastStatusChange("nfl", Execution, "Running"));

        groups.ShouldBeEmpty();
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AHostThatServesTheHubsIgnoresTheRelaySwitch()
    {
        var (hub, groups) = SignalR();
        var routing = new RoutingPipelineStatusBroadcaster(hub, Hosts(true).Object, Environment().Object, Switch(false));

        await routing.BroadcastStatusChange("nfl", Execution, "Running", orgId: Org);

        groups.ShouldContain("pipeline:nfl");
    }

    [Fact]
    public async Task AHostThatServesTheHubsBroadcastsThroughSignalRAndRelaysNothing()
    {
        var (hub, groups) = SignalR();
        var client = new Mock<IPipelineEventClient>(MockBehavior.Strict);
        var routing = new RoutingPipelineStatusBroadcaster(hub, Hosts(true).Object, Environment().Object, Switch(true), clientFactory: () => client.Object);

        await routing.BroadcastStatusChange("nfl", Execution, "Running", orgId: Org);

        groups.ShouldContain($"org:{Org}:pipeline-updates");
        groups.ShouldContain("pipeline:nfl");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AHostThatServesNoHubRelaysTheEventAndTouchesNoGroup()
    {
        var (hub, groups) = SignalR();
        PipelineEventRequest? sent = null;
        var client = new Mock<IPipelineEventClient>();
        client.Setup(c => c.Relay(It.IsAny<PipelineEventRequest>(), It.IsAny<CancellationToken>()))
            .Callback((PipelineEventRequest r, CancellationToken _) => sent = r)
            .ReturnsAsync(GenericResult<PipelineEventResponse>.Success(new PipelineEventResponse { Kind = "StatusChanged" }));
        var routing = new RoutingPipelineStatusBroadcaster(hub, Hosts(false).Object, Environment().Object, Switch(true), clientFactory: () => client.Object);

        await routing.BroadcastStatusChange("nfl", Execution, "Running", "started", Org);

        groups.ShouldBeEmpty();
        sent.ShouldNotBeNull();
        sent!.Kind.ShouldBe("StatusChanged");
        sent.OrgId.ShouldBe(Org);
        sent.Status.ShouldNotBeNull();
        sent.Status!.PipelineName.ShouldBe("nfl");
        sent.Status.ExecutionId.ShouldBe(Execution);
        sent.Status.Status.ShouldBe("Running");
        sent.Status.Message.ShouldBe("started");
    }

    [Fact]
    public async Task EveryBroadcastMethodRelaysItsOwnKind()
    {
        var (hub, _) = SignalR();
        var kinds = new List<string>();
        var client = new Mock<IPipelineEventClient>();
        client.Setup(c => c.Relay(It.IsAny<PipelineEventRequest>(), It.IsAny<CancellationToken>()))
            .Callback((PipelineEventRequest r, CancellationToken _) => kinds.Add(r.Kind))
            .ReturnsAsync(GenericResult<PipelineEventResponse>.Success(new PipelineEventResponse { Kind = "x" }));
        var routing = new RoutingPipelineStatusBroadcaster(hub, Hosts(false).Object, Environment().Object, Switch(true), clientFactory: () => client.Object);
        var task = Guid.NewGuid();

        await routing.BroadcastStatusChange("nfl", Execution, "Running");
        await routing.BroadcastProgress("nfl", Execution, 1, 1, 1, 0, 50);
        await routing.BroadcastCompletion(new PipelineExecutionComplete { PipelineName = "nfl", ExecutionId = Execution, Status = "Completed" });
        await routing.BroadcastTaskStatus(Execution, task, "Running", 1, 1, 0, 0, false);
        await routing.BroadcastEdgeFlow(Execution, task, Guid.NewGuid(), 5);
        await routing.BroadcastExecutionPaused(Execution);
        await routing.BroadcastExecutionResumed(Execution);
        await routing.BroadcastComponents(new PipelineComponentsUpdate { PipelineName = "nfl", ExecutionId = Execution });

        kinds.ShouldBe(["StatusChanged", "Progress", "Completed", "TaskStatus", "EdgeFlow", "Paused", "Resumed", "ComponentsChanged"]);
    }

    [Fact]
    public async Task ARelayTheHubHostRefusesDoesNotThrowIntoTheRun()
    {
        var (hub, _) = SignalR();
        var client = new Mock<IPipelineEventClient>();
        client.Setup(c => c.Relay(It.IsAny<PipelineEventRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineEventResponse>.Failure(new GenericMessage("403")));
        var routing = new RoutingPipelineStatusBroadcaster(hub, Hosts(false).Object, Environment().Object, Switch(true), clientFactory: () => client.Object);

        await Should.NotThrowAsync(() => routing.BroadcastStatusChange("nfl", Execution, "Running"));
        client.Verify(c => c.Relay(It.IsAny<PipelineEventRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ARelayThatThrowsDoesNotThrowIntoTheRun()
    {
        var (hub, _) = SignalR();
        var client = new Mock<IPipelineEventClient>();
        client.Setup(c => c.Relay(It.IsAny<PipelineEventRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("down"));
        var routing = new RoutingPipelineStatusBroadcaster(hub, Hosts(false).Object, Environment().Object, Switch(true), clientFactory: () => client.Object);

        await Should.NotThrowAsync(() => routing.BroadcastExecutionPaused(Execution));
    }

    [Fact]
    public async Task AHostWithNoUsableHostRowSendsNothingAnywhere()
    {
        var (hub, groups) = SignalR();
        var client = new Mock<IPipelineEventClient>(MockBehavior.Strict);
        var routing = new RoutingPipelineStatusBroadcaster(hub, Hosts(null).Object, Environment().Object, Switch(true), clientFactory: () => client.Object);

        await routing.BroadcastStatusChange("nfl", Execution, "Running");

        groups.ShouldBeEmpty();
        client.VerifyNoOtherCalls();
    }

    // FDW-4064: building the client resolves its endpoint and logs an error when none is declared, so it is built only to send.
    [Fact]
    public async Task TheClientIsNotBuiltWhileTheRelayIsOff()
    {
        var (hub, _) = SignalR();
        var built = 0;
        var routing = new RoutingPipelineStatusBroadcaster(
            hub, Hosts(false).Object, Environment().Object, Switch(false), clientFactory: () => { built++; return new Mock<IPipelineEventClient>().Object; });

        await routing.BroadcastStatusChange("nfl", Execution, "Running");
        await routing.BroadcastExecutionPaused(Execution);

        built.ShouldBe(0);
    }

    [Fact]
    public async Task TheClientIsNotBuiltWhileTheRelaySettingIsMissing()
    {
        var (hub, _) = SignalR();
        var built = 0;
        var routing = new RoutingPipelineStatusBroadcaster(
            hub, Hosts(false).Object, Environment().Object, Switch(null), clientFactory: () => { built++; return new Mock<IPipelineEventClient>().Object; });

        await routing.BroadcastStatusChange("nfl", Execution, "Running");

        built.ShouldBe(0);
    }

    [Fact]
    public async Task TheClientIsNotBuiltOnAHostThatServesTheHubsWhateverTheSwitchSays()
    {
        var (hub, groups) = SignalR();
        var built = 0;
        var routing = new RoutingPipelineStatusBroadcaster(
            hub, Hosts(true).Object, Environment().Object, Switch(true), clientFactory: () => { built++; return new Mock<IPipelineEventClient>().Object; });

        await routing.BroadcastStatusChange("nfl", Execution, "Running", orgId: Org);

        groups.ShouldContain("pipeline:nfl");
        built.ShouldBe(0);
    }

    [Fact]
    public async Task TheClientIsNotBuiltWhenThereIsNoHostRow()
    {
        var (hub, _) = SignalR();
        var built = 0;
        var routing = new RoutingPipelineStatusBroadcaster(
            hub, Hosts(null).Object, Environment().Object, Switch(true), clientFactory: () => { built++; return new Mock<IPipelineEventClient>().Object; });

        await routing.BroadcastStatusChange("nfl", Execution, "Running");

        built.ShouldBe(0);
    }

    [Fact]
    public async Task TheClientIsBuiltOnceAndKeptWhileEventsAreRelayed()
    {
        var (hub, _) = SignalR();
        var built = 0;
        var client = new Mock<IPipelineEventClient>();
        client.Setup(c => c.Relay(It.IsAny<PipelineEventRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineEventResponse>.Success(new PipelineEventResponse { Kind = "x" }));
        var routing = new RoutingPipelineStatusBroadcaster(
            hub, Hosts(false).Object, Environment().Object, Switch(true), clientFactory: () => { built++; return client.Object; });

        await routing.BroadcastStatusChange("nfl", Execution, "Running");
        await routing.BroadcastExecutionPaused(Execution);

        built.ShouldBe(1);
        client.Verify(c => c.Relay(It.IsAny<PipelineEventRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ARelayThatIsOnWithNoClientAsksForOneAndLogsTheMissingClientAsAnError()
    {
        var (hub, groups) = SignalR();
        var asked = 0;
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<RoutingPipelineStatusBroadcaster>>();
        logger.Setup(l => l.IsEnabled(It.IsAny<Microsoft.Extensions.Logging.LogLevel>())).Returns(true);
        var routing = new RoutingPipelineStatusBroadcaster(
            hub, Hosts(false).Object, Environment().Object, Switch(true), logger.Object, clientFactory: () => { asked++; return null; });

        await Should.NotThrowAsync(() => routing.BroadcastStatusChange("nfl", Execution, "Running"));

        asked.ShouldBe(1);
        groups.ShouldBeEmpty();
        logger.Verify(
            l => l.Log(
                Microsoft.Extensions.Logging.LogLevel.Error,
                It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AHostThatServesNoHubAndHasNoClientSendsNothing()
    {
        var (hub, groups) = SignalR();
        var routing = new RoutingPipelineStatusBroadcaster(hub, Hosts(false).Object, Environment().Object, Switch(true));

        await Should.NotThrowAsync(() => routing.BroadcastStatusChange("nfl", Execution, "Running"));

        groups.ShouldBeEmpty();
    }
}
