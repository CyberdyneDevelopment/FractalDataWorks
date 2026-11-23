using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Settings;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="QueuedPipelineRunner"/> (FDW-6131): a run whose execution its caller opened is queued as the caller prepared it, with no second
/// record; each reason the queue refuses it is its own code; a run no other run started is opened as a root.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class QueuedPipelineRunnerTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Opened = Guid.Parse("33333333-0000-0000-0000-000000000003");
    private static readonly Guid PipelineIdentity = Guid.Parse("44444444-0000-0000-0000-000000000004");

    private readonly Mock<IExecutionTracker> _tracker = new();
    private readonly Mock<IEffectiveSettingsProvider> _settings = new();
    private readonly Mock<IDataGateway> _gateway = new();
    private readonly Mock<IDataGatewayProvider> _gateways = new();
    private readonly Mock<IPipelineExecutionQueue> _queue = new();
    private readonly Mock<IPipelineStatusBroadcaster> _broadcaster = new();
    private readonly Mock<IExecutionCompletionSignaler> _signaler = new();
    private readonly List<PipelineExecutionRequest> _enqueued = [];
    private readonly List<IDataCommand> _written = [];
    private readonly GraphPipelineConfiguration _pipeline = new() { Id = PipelineIdentity, Name = "Consolidate", Implementation = "Graph" };

    public QueuedPipelineRunnerTests()
    {
        PipelineTypes.OperationalConnection = "OpsDb";
        _settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxPipelineChainDepth, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(10);

        var state = new Mock<IExecutionStateType>();
        state.SetupGet(s => s.Name).Returns("Created");
        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.Id).Returns(Opened);
        item.SetupGet(i => i.State).Returns(state.Object);
        _tracker
            .Setup(t => t.CreateItem(
                It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExecutionItem>.Success(item.Object));
        _tracker
            .Setup(t => t.Complete(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        _gateways.Setup(g => g.ByName("Main")).Returns(_gateway.Object);
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, _, _) => _written.Add(command))
            .ReturnsAsync(GenericResult<int>.Success(1));
        Accepting(PipelineEnqueueResult.Accepted);
        _broadcaster
            .Setup(b => b.BroadcastStatusChange(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
    }

    public void Dispose()
    {
        PipelineTypes.OperationalConnection = null;
    }

    private void Accepting(PipelineEnqueueResult answer) =>
        _queue
            .Setup(q => q.Enqueue(It.IsAny<PipelineExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PipelineExecutionRequest, CancellationToken>((request, _) => _enqueued.Add(request))
            .Returns(new ValueTask<PipelineEnqueueResult>(answer));

    private QueuedPipelineRunner Sut() => new(
        _tracker.Object, _gateways.Object, _queue.Object, new PipelineRunScheduler(TimeProvider.System), _broadcaster.Object, TestRunOrigins.Create(), _settings.Object, _signaler.Object,
        NullLogger<QueuedPipelineRunner>.Instance);

    private static PipelineStart Start(PipelineStartOpened? opened = null) => new()
    {
        PipelineId = PipelineIdentity,
        TriggerSource = "Api",
        TenantId = Tenant,
        RunRoles = ["etl-runner"],
        Parameters = [],
        ScheduleName = "Nightly",
        TimeoutSeconds = 600,
        AwaitCompletion = false,
        Opened = opened
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task AStartOnAHostThatNamesNoOperationalStoreIsRefusedWithItsCodeAndNothingIsOpenedOrQueued()
    {
        // FDW-7033: the same rule as the at-once runner's, from the one opening code both use.
        PipelineTypes.OperationalConnection = null;

        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(message => message.Code).ShouldContain("PIPELINERUN-60805");
        _enqueued.ShouldBeEmpty();
        _written.ShouldBeEmpty();
        _tracker.Verify(t => t.CreateItem(
            It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnExecutionTheCallerOpenedIsQueuedWithoutASecondRecord()
    {
        var values = new Dictionary<string, object?> { ["Region"] = "ERCOT" };

        var result = await Sut().Start(Start(new PipelineStartOpened(Opened, values)), _pipeline, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(Opened);
        _tracker.Verify(t => t.CreateItem(
            It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Never);
        _tracker.Verify(t => t.RecordEvent(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _written.ShouldBeEmpty();
        var request = _enqueued.ShouldHaveSingleItem();
        request.ExecutionId.ShouldBe(Opened);
        request.PipelineId.ShouldBe(PipelineIdentity);
        request.TenantId.ShouldBe(Tenant);
        request.ScheduleName.ShouldBe("Nightly");
        request.TimeoutSeconds.ShouldBe(600);
        request.Parameters.ShouldNotBeNull()["Region"].ShouldBe("ERCOT");
    }

    public static TheoryData<string, string, string> QueueRefusals => new()
    {
        { PipelineEnqueueResult.QueueFullCode, "the queue holds 1", "PIPELINERUN-150830" },
        { PipelineEnqueueResult.ServerBusyCode, "too many runs wait", "PIPELINERUN-150831" },
        { PipelineEnqueueResult.RefusalNotRecordedCode, "the refusal could not be written", "PIPELINERUN-90830" },
    };

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [MemberData(nameof(QueueRefusals))]
    public async Task AnOpenedRunTheQueueRefusesIsRefusedWithTheCodeOfItsReasonAndReadsBackAsTheQueuesOwnRefusal(string queueCode, string reason, string code)
    {
        Accepting(new PipelineEnqueueResult(false, queueCode, reason));

        var result = await Sut().Start(Start(new PipelineStartOpened(Opened, new Dictionary<string, object?>())), _pipeline, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe(code);
        QueuedPipelineRunner.IsQueueRefusal(result, out var refusal).ShouldBeTrue();
        refusal.ShouldNotBeNull().Code.ShouldBe(queueCode);
        refusal.Reason.ShouldBe(reason);
        _tracker.Verify(
            t => t.Complete(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AFailureTheQueueDidNotGiveIsNotReadAsAQueueRefusal()
    {
        QueuedPipelineRunner.IsQueueRefusal(GenericResult.Failure(new GenericMessage("something else")), out var refusal).ShouldBeFalse();
        refusal.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunNoOtherRunStartedIsOpenedAsARootWithNoParentAndNoBorrowedCorrelation()
    {
        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _tracker.Verify(t => t.CreateItem(
            It.IsAny<IExecutionItemType>(), It.IsAny<string>(), Tenant, null, null, "Api",
            It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Once);
        _enqueued.ShouldHaveSingleItem().ExecutionId.ShouldBe(Opened);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TheQueuedRequestCarriesTheStartsPersonOrNone(bool opened)
    {
        var person = Guid.Parse("66666666-0000-0000-0000-000000000006");
        var start = Start(opened ? new PipelineStartOpened(Opened, new Dictionary<string, object?>()) : null) with { PersonId = person };

        var result = await Sut().Start(start, _pipeline, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _enqueued.ShouldHaveSingleItem().PersonId.ShouldBe(person);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheRunnerCarriesOutTheQueuedRunMode()
    {
        Sut().RunMode.Name.ShouldBe("Queued");
    }
}
