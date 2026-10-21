using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="PipelineRunStarter"/> (FDW-1381): a started run is the child of the run that started it, belongs to its
/// tenant, acts with its roles, carries typed parameters and has a run row that holds its start and end.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineRunStarterTests : IDisposable
{
    private static readonly Guid Parent = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Child = Guid.Parse("33333333-0000-0000-0000-000000000003");
    private static readonly Guid PipelineIdentity = Guid.Parse("44444444-0000-0000-0000-000000000004");

    private readonly Mock<IExecutionTracker> _tracker = new();
    private readonly Mock<IDataGateway> _gateway = new();
    private readonly Mock<IDataGatewayProvider> _gateways = new();
    private readonly Mock<IPipelineConfigurationProvider> _pipelines = new();
    private readonly Mock<IPipelineExecutionQueue> _queue = new();
    private readonly PipelineRunSlot _slot = new();
    private readonly Mock<IPipelineStatusBroadcaster> _broadcaster = new();
    private readonly Mock<IExecutionCompletionSignaler> _signaler = new();
    private readonly List<PipelineExecutionRequest> _enqueued = [];
    private readonly List<IDataCommand> _written = [];
    private readonly List<(Guid Id, string Type, IReadOnlyDictionary<string, object?>? Data)> _events = [];

    public PipelineRunStarterTests()
    {
        EtlPipelineTypes.OperationalConnection = "OpsDb";

        var itemType = new Mock<IExecutionItemType>();
        itemType.SetupGet(t => t.Name).Returns("Job");
        var state = new Mock<IExecutionStateType>();
        state.SetupGet(s => s.Name).Returns("Created");
        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.Id).Returns(Child);
        item.SetupGet(i => i.State).Returns(state.Object);
        item.SetupGet(i => i.ItemType).Returns(itemType.Object);

        _tracker
            .Setup(t => t.CreateItem(
                It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExecutionItem>.Success(item.Object));
        var parent = ItemOf(Parent, null, "Orders");
        _tracker.Setup(t => t.GetItem(Parent, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Success(parent));
        _tracker
            .Setup(t => t.Complete(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        _tracker
            .Setup(t => t.RecordEvent(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string?, IReadOnlyDictionary<string, object?>?, string?, CancellationToken>(
                (id, type, _, data, _, _) => _events.Add((id, type, data)))
            .ReturnsAsync(GenericResult<IExecutionEvent>.Success(Mock.Of<IExecutionEvent>()));

        _gateways.Setup(g => g.ByName("Main")).Returns(_gateway.Object);
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, _, _) => _written.Add(command))
            .ReturnsAsync(GenericResult<int>.Success(1));

        _queue
            .Setup(q => q.Enqueue(It.IsAny<PipelineExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PipelineExecutionRequest, CancellationToken>((request, _) => _enqueued.Add(request))
            .Returns(new ValueTask<bool>(true));
        _broadcaster
            .Setup(b => b.BroadcastStatusChange(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
    }

    public void Dispose()
    {
        _slot.Dispose();
        EtlPipelineTypes.OperationalConnection = null;
    }

    private static IExecutionItem ItemOf(Guid id, Guid? parentId, string pipelineName)
    {
        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.Id).Returns(id);
        item.SetupGet(i => i.ParentId).Returns(parentId);
        item.SetupGet(i => i.Parameters).Returns(new Dictionary<string, object?> { ["PipelineName"] = pipelineName });
        return item.Object;
    }

    private PipelineRunStarter Sut(bool withSignaler = true) => new(
        _tracker.Object, _gateways.Object, _pipelines.Object, _queue.Object, _slot, _broadcaster.Object,
        withSignaler ? _signaler.Object : null, NullLogger<PipelineRunStarter>.Instance);

    private void Pipeline(params PipelineParameterConfiguration[] declared)
    {
        var configuration = new GraphPipelineConfiguration { Id = PipelineIdentity, Name = "Consolidate", Parameters = [.. declared] };
        _pipelines
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(configuration));
        _pipelines
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(configuration));
    }

    private static PipelineParameterConfiguration Declared(string name, string type, bool required = false) =>
        new() { Name = name, ParameterType = type, IsRequired = required };

    private static PipelineRunStart Start(
        bool awaitCompletion = false,
        Guid? id = null,
        string? name = "Consolidate",
        params KeyValuePair<string, string>[] parameters) => new()
    {
        ParentExecutionId = Parent,
        PipelineId = id,
        PipelineName = id is null ? name : null,
        TenantId = Tenant,
        RunRoles = ["etl-runner"],
        TriggerSource = "Pipeline:Orders",
        Parameters = parameters,
        AwaitCompletion = awaitCompletion
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStartedRunRecordsTheParametersItWasGivenOnItsOwnRecord()
    {
        Pipeline(Declared("Region", "String"));

        var result = await Sut().Start(
            Start(parameters: new KeyValuePair<string, string>("Region", "ERCOT")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var recorded = _events.ShouldHaveSingleItem();
        recorded.Id.ShouldBe(Child);
        recorded.Type.ShouldBe(RunParameterEntries.EventType);
        var entries = (List<IReadOnlyDictionary<string, object?>>)recorded.Data!["parameters"]!;
        entries.ShouldHaveSingleItem()["name"].ShouldBe("Region");
        entries[0]["given"].ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AParameterThatTookItsDeclaredDefaultIsRecordedAsNotGiven()
    {
        Pipeline(new PipelineParameterConfiguration { Name = "Region", ParameterType = "String", DefaultText = "ERCOT" });

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var entries = (List<IReadOnlyDictionary<string, object?>>)_events.ShouldHaveSingleItem().Data!["parameters"]!;
        entries.ShouldHaveSingleItem()["given"].ShouldBe(false);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task APipelineThatDeclaresNoParametersRecordsNoEvent()
    {
        Pipeline();

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStartedRunIsTheChildOfTheRunThatStartedItInItsTenantAndWithItsRoles()
    {
        Pipeline(Declared("Region", "String"));

        var result = await Sut().Start(
            Start(parameters: new KeyValuePair<string, string>("Region", "ERCOT")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(Child);
        _tracker.Verify(t => t.CreateItem(
            ExecutionItemTypes.Job, It.Is<string>(n => n.StartsWith("Consolidate_", StringComparison.Ordinal)), Tenant, Parent,
            Parent.ToString("N"), "Pipeline:Orders", It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Once);

        var request = _enqueued.ShouldHaveSingleItem();
        request.ExecutionId.ShouldBe(Child);
        request.PipelineId.ShouldBe(PipelineIdentity);
        request.PipelineName.ShouldBe("Consolidate");
        request.TenantId.ShouldBe(Tenant);
        request.RunRoles.ShouldBe(new[] { "etl-runner" });
        request.TriggerSource.ShouldBe("Pipeline:Orders");
        request.Parameters.ShouldNotBeNull()["Region"].ShouldBe("ERCOT");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheRunRowIsWrittenBeforeTheRunIsQueuedSoItsStartAndEndHaveARowToLandOn()
    {
        Pipeline();

        await Sut().Start(Start(), TestContext.Current.CancellationToken);

        _written.Count.ShouldBe(1);
        _written[0].ShouldBeAssignableTo<IDataCommand>();
        _enqueued.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunNamedByIdIsFoundByIdAndOneNamedByNameByName()
    {
        Pipeline();

        await Sut().Start(Start(id: PipelineIdentity), TestContext.Current.CancellationToken);
        await Sut().Start(Start(name: "Consolidate"), TestContext.Current.CancellationToken);

        _pipelines.Verify(p => p.Get(PipelineIdentity, It.IsAny<CancellationToken>()), Times.Once);
        _pipelines.Verify(p => p.Get("Consolidate", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(true, true, "both")]
    [InlineData(false, false, "neither")]
    public async Task AStartThatNamesThePipelineByBothOrNeitherIsRefused(bool byId, bool byName, string reason)
    {
        Pipeline();
        var start = new PipelineRunStart
        {
            ParentExecutionId = Parent,
            PipelineId = byId ? PipelineIdentity : null,
            PipelineName = byName ? "Consolidate" : null,
            TenantId = Tenant,
            RunRoles = ["etl-runner"],
            TriggerSource = "Pipeline:Orders",
            Parameters = [],
            AwaitCompletion = false
        };

        var result = await Sut().Start(start, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(reason);
        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatRequiresApprovalIsRefusedBecauseAStartFromInsideARunHasNoPersonToWaitFor()
    {
        var gated = new GraphPipelineConfiguration { Id = PipelineIdentity, Name = "Consolidate", RequireApprovalToRun = true };
        _pipelines
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(gated));

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("requires approval");
        _tracker.Verify(t => t.CreateItem(
            It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Never);
        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatCannotBeFoundIsRefusedBeforeAnythingIsWritten()
    {
        _pipelines
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Failure(new GenericMessage("not found")));

        var result = await Sut().Start(Start(name: "Gone"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Gone");
        _tracker.Verify(t => t.CreateItem(
            It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Never);
        _written.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatIsTheRunItWouldBeStartedUnderIsRefusedBecauseItWouldNeverEnd()
    {
        Pipeline();
        var self = ItemOf(Parent, null, "Consolidate");
        _tracker.Setup(t => t.GetItem(Parent, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Success(self));

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("never end");
        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatStartedAnAncestorFurtherUpTheChainIsRefusedToo()
    {
        Pipeline();
        var grandparent = Guid.Parse("55555555-0000-0000-0000-000000000005");
        _tracker.Setup(t => t.GetItem(Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExecutionItem>.Success(ItemOf(Parent, grandparent, "Orders")));
        _tracker.Setup(t => t.GetItem(grandparent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExecutionItem>.Success(ItemOf(grandparent, null, "Consolidate")));

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("never end");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineNotFoundWithNoMessageIsRefusedSayingWhatThatMeans()
    {
        _pipelines
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Failure(new GenericMessage(string.Empty)));

        var result = await Sut().Start(Start(name: "Gone"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("no pipeline of that name is visible");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AParameterThePipelineDoesNotDeclareIsRefusedNamingIt()
    {
        Pipeline(Declared("Region", "String"));

        var result = await Sut().Start(
            Start(parameters: new KeyValuePair<string, string>("Mood", "x")), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Mood");
        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARequiredParameterLeftOutIsRefusedNamingIt()
    {
        Pipeline(Declared("Region", "String", required: true));

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Region");
    }

    private void GraphPipeline(params PipelineParameterConfiguration[] declared)
    {
        var configuration = new GraphPipelineConfiguration { Id = PipelineIdentity, Name = "Consolidate" };
        foreach (var parameter in declared)
        {
            configuration.Parameters.Add(parameter);
        }

        _pipelines
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(configuration));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AGraphPipelinesParametersAreTypedAndQueuedWithTheRun()
    {
        GraphPipeline(new PipelineParameterConfiguration { Name = "Limit", ParameterType = "Int32", IsRequired = true });

        var result = await Sut().Start(
            Start(parameters: new KeyValuePair<string, string>("Limit", "42")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _enqueued.ShouldHaveSingleItem().Parameters.ShouldNotBeNull()["Limit"].ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AParameterAGraphPipelineDoesNotDeclareIsRefusedNamingIt()
    {
        GraphPipeline(new PipelineParameterConfiguration { Name = "Limit", ParameterType = "Int32" });

        var result = await Sut().Start(
            Start(parameters: new KeyValuePair<string, string>("Mood", "x")), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Mood");
        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACheckOfTheNamesAScheduleSuppliesReadsAGraphPipelinesParameters()
    {
        GraphPipeline(new PipelineParameterConfiguration { Name = "Limit", ParameterType = "Int32", IsRequired = true });

        var known = await Sut().Check("Consolidate", ["Limit"], TestContext.Current.CancellationToken);
        var unknown = await Sut().Check("Consolidate", ["Mood"], TestContext.Current.CancellationToken);

        known.IsSuccess.ShouldBeTrue(known.CurrentMessage);
        unknown.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStartToBeAwaitedOnAHostWithNoSignalerIsRefusedNotStartedUnawaitable()
    {
        Pipeline();

        var result = await Sut(withSignaler: false).Start(Start(awaitCompletion: true), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("signaler");
        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AHostThatNamesNoOperationalStoreCannotStartARun()
    {
        Pipeline();
        EtlPipelineTypes.OperationalConnection = null;

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("OperationalConnection");
        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFullQueueClosesTheItemItOpenedAsFailedAndReleasesTheCompletion()
    {
        Pipeline();
        _queue
            .Setup(q => q.Enqueue(It.IsAny<PipelineExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<bool>(false));

        var result = await Sut().Start(Start(awaitCompletion: true), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        _tracker.Verify(t => t.Complete(Child, false, "QueueFull", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _signaler.Verify(s => s.Register(Child), Times.Once);
        _signaler.Verify(s => s.Deregister(Child), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunRowThatCannotBeWrittenClosesTheItemAndQueuesNothing()
    {
        Pipeline();
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(new GenericMessage("store refused")));

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        _tracker.Verify(t => t.Complete(Child, false, "RunRowNotWritten", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task CheckJudgesTheNamesWithoutAnyValue()
    {
        Pipeline(Declared("Region", "String", required: true));

        (await Sut().Check("Consolidate", ["Region"], TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await Sut().Check("Consolidate", [], TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await Sut().Check("Consolidate", ["Region", "Mood"], TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await Sut().Check("Consolidate", ["Region", "region"], TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AwaitWaitsOnTheSignalerAndAlwaysReleasesIt()
    {
        _signaler.Setup(s => s.Await(Child, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var succeeded = await Sut().Await(Child, TestContext.Current.CancellationToken);

        succeeded.ShouldBeTrue();
        _signaler.Verify(s => s.Deregister(Child), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatWaitsForTheRunItStartedHandsItsSlotBackAndTakesItAgain()
    {
        await _slot.Acquire(TestContext.Current.CancellationToken);
        _slot.MarkHeld();
        var slotWasFreeWhileWaiting = false;
        _signaler.Setup(s => s.Await(Child, It.IsAny<CancellationToken>())).Returns(async () =>
        {
            slotWasFreeWhileWaiting = await Task.Run(async () =>
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
                try
                {
                    await _slot.Acquire(timeout.Token);
                    _slot.MarkHeld();
                    _slot.Release();
                    return true;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            });
            return true;
        });

        await Sut().Await(Child, TestContext.Current.CancellationToken);

        slotWasFreeWhileWaiting.ShouldBeTrue();
        using var afterwards = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Should.ThrowAsync<OperationCanceledException>(() => _slot.Acquire(afterwards.Token));
    }
}
