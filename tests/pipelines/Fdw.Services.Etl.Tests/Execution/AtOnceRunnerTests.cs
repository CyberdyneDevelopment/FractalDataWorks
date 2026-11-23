using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="AtOnceRunner"/> (FDW-6141, slice 2): a request starts the stored graph at once, in the host that took it, under the limits its pipeline states and the
/// ceilings in force. The run is opened as every run is (<see cref="PipelineRunOpening"/>), its row states the AtOnce run mode and the host and slot that ran it, it is held by
/// run control so a stop reaches it, and the shared run code (<see cref="IPipelineRequestProcessor"/>) runs it without the queue or the scheduler.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class AtOnceRunnerTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Person = Guid.Parse("55555555-0000-0000-0000-000000000005");
    private static readonly Guid Opened = Guid.Parse("33333333-0000-0000-0000-000000000003");
    private static readonly Guid PipelineIdentity = Guid.Parse("44444444-0000-0000-0000-000000000004");
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    private readonly Mock<IExecutionTracker> _tracker = new();
    private readonly Mock<IDataGateway> _gateway = new();
    private readonly Mock<IDataGatewayProvider> _gateways = new();
    private readonly Mock<IPipelineStatusBroadcaster> _broadcaster = new();
    private readonly Mock<IEffectiveSettingsProvider> _settings = new();
    private readonly List<IDataCommand> _written = [];
    private readonly RecordingProcessor _processor = new();
    private readonly PipelineRunControl _control = new();
    private readonly PipelineAtOnceRuns _runs = new();
    private readonly GraphPipelineConfiguration _pipeline = new()
    {
        Id = PipelineIdentity, Name = "Chat", Implementation = "Interactive", MaxRunSeconds = 120, MaxSteps = 40, ClaimKeyTemplate = "{{conversation}}", HalfDoneRule = "End",
    };

    public AtOnceRunnerTests()
    {
        PipelineTypes.OperationalConnection = "OpsDb";

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
        _broadcaster
            .Setup(b => b.BroadcastStatusChange(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
        Ceilings(runSeconds: 600, steps: 200);
        _settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxPipelineChainDepth, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(10);
    }

    public void Dispose()
    {
        _processor.Release();
        _processor.Dispose();
        _runs.Dispose();
        PipelineTypes.OperationalConnection = null;
    }

    private void Ceilings(int runSeconds, int steps)
    {
        _settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.InteractiveMaxRunSecondsCeiling, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(runSeconds);
        _settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.InteractiveMaxStepsCeiling, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(steps);
    }

    private AtOnceRunner Sut() => new(
        _tracker.Object, _gateways.Object, TestRunOrigins.Create("Reference.Api", "b"), _broadcaster.Object, _processor, _control, _runs, _settings.Object,
        signaler: null, NullLogger<AtOnceRunner>.Instance);

    private static PipelineStart Start(int? timeoutSeconds = null, PipelineStartOpened? opened = null) => new()
    {
        PipelineId = PipelineIdentity,
        TriggerSource = "Api",
        TenantId = Tenant,
        RunRoles = ["etl-runner"],
        Parameters = [],
        TimeoutSeconds = timeoutSeconds,
        AwaitCompletion = false,
        Opened = opened,
        PersonId = Person,
    };

    private void NothingWasOpened()
    {
        _tracker.Verify(t => t.CreateItem(
            It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Never);
        _written.ShouldBeEmpty();
        _processor.Calls.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task AStartOnAHostThatNamesNoOperationalStoreIsRefusedWithItsCodeAndNothingIsOpenedOrAwaited()
    {
        // FDW-7033: a host that names no operational store can keep neither the start nor the end of a run, so no run is taken: the start is refused before one exists.
        PipelineTypes.OperationalConnection = null;

        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(message => message.Code).ShouldContain("PIPELINERUN-60805");
        NothingWasOpened();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheRunnerCarriesOutTheAtOnceRunMode() =>
        Sut().RunMode.Name.ShouldBe("AtOnce");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStartAnswersTheExecutionAtOnceAndTheRunIsRunByTheSharedRunCodeWithoutTheQueue()
    {
        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(Opened);
        var call = await _processor.NextCall(Patience);
        _processor.Finished.ShouldBeFalse("the start answers while the run is still running");
        call.Request.ExecutionId.ShouldBe(Opened);
        call.Request.PipelineId.ShouldBe(PipelineIdentity);
        call.Request.PipelineName.ShouldBe("Chat");
        call.Request.TenantId.ShouldBe(Tenant);
        call.Request.PersonId.ShouldBe(Person);
        call.Request.TriggerSource.ShouldBe("Api");
        call.Refusal.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheRunRowStatesTheAtOnceRunModeThePersonAndTheHostAndSlotThatRanIt()
    {
        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);

        var row = _written.OfType<InsertCommand<EtlPipelineExecutionRecord>>().ShouldHaveSingleItem().Data.Single();
        row.Id.ShouldBe(Opened);
        row.RunMode.ShouldBe("AtOnce");
        row.ActingPersonId.ShouldBe(Person);
        row.ExecutingHost.ShouldBe("Reference.Api");
        row.ExecutingSlot.ShouldBe("b");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheRunsTimeLimitIsTheOneItsPipelineStates()
    {
        _ = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        (await _processor.NextCall(Patience)).Request.TimeoutSeconds.ShouldBe(120);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheRunsStepLimitIsTheOneItsPipelineStatesAndTravelsWithTheRequest()
    {
        _ = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        (await _processor.NextCall(Patience)).Request.MaxSteps.ShouldBe(40);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(30, 30)]
    [InlineData(900, 120)]
    public async Task ACallerCanOnlyShortenTheTimeLimitItsPipelineStates(int callerSeconds, int expected)
    {
        _ = await Sut().Start(Start(callerSeconds), _pipeline, TestContext.Current.CancellationToken);

        (await _processor.NextCall(Patience)).Request.TimeoutSeconds.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunIsHeldByRunControlWhileItRunsSoAStopReachesIt()
    {
        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);
        var call = await _processor.NextCall(Patience);

        _control.Cancel(result.Value).IsSuccess.ShouldBeTrue();

        call.Run.IsCancelled.ShouldBeTrue();
        call.Run.Token.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task OnceTheRunHasEndedRunControlHoldsNothingOfIt()
    {
        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);
        _ = await _processor.NextCall(Patience);

        _processor.Release();
        await _runs.Drain(Patience, TestContext.Current.CancellationToken);

        _control.Cancel(result.Value).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnExecutionTheCallerOpenedIsRunWithoutASecondRecord()
    {
        var values = new Dictionary<string, object?> { ["Region"] = "ERCOT" };

        var result = await Sut().Start(Start(opened: new PipelineStartOpened(Opened, values)), _pipeline, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(Opened);
        _tracker.Verify(t => t.CreateItem(
            It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Never);
        _written.ShouldBeEmpty();
        var call = await _processor.NextCall(Patience);
        call.Request.ExecutionId.ShouldBe(Opened);
        call.Request.Parameters.ShouldNotBeNull()["Region"].ShouldBe("ERCOT");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task APipelineThatStatesNoTimeLimitIsRefusedAndNothingIsOpened()
    {
        _pipeline.MaxRunSeconds = null;

        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(m => m.Code).ShouldContain("PIPELINERUN-20845");
        NothingWasOpened();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task APipelineThatStatesNoStepLimitIsRefusedAndNothingIsOpened()
    {
        _pipeline.MaxSteps = null;

        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(m => m.Code).ShouldContain("PIPELINERUN-20845");
        NothingWasOpened();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task ATimeLimitAboveTheCeilingInForceRefusesTheRunAndNothingIsOpenedOrLowered()
    {
        Ceilings(runSeconds: 60, steps: 200);

        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(m => m.Code).ShouldContain("PIPELINERUN-20841");
        NothingWasOpened();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task AStepLimitAboveTheCeilingInForceRefusesTheRunAndNothingIsOpenedOrLowered()
    {
        Ceilings(runSeconds: 600, steps: 10);

        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(m => m.Code).ShouldContain("PIPELINERUN-20841");
        NothingWasOpened();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData(nameof(SettingDefinitions.InteractiveMaxRunSecondsCeiling))]
    [InlineData(nameof(SettingDefinitions.InteractiveMaxStepsCeiling))]
    public async Task ACeilingThatIsNotSetRefusesTheRunNamingTheSettingAndNothingIsOpened(string setting)
    {
        _settings.Setup(s => s.GetEffectiveValue<int>(setting, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(0);

        var result = await Sut().Start(Start(), _pipeline, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(m => m.Code).ShouldContain("PIPELINERUN-20844");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(setting);
        NothingWasOpened();
    }

    // FDW-6701 (slice 2d): a pipeline an AtOnce run starts is carried out at once under what is left of its parent's time and steps.
    private static readonly GraphPipelineConfiguration ChildWithNoLimitsOfItsOwn = new()
    {
        Id = Guid.Parse("66666666-0000-0000-0000-000000000006"), Name = "Report", Implementation = "Graph",
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AChildWithNoLimitsOfItsOwnIsBoundedByItsParentsAndItsStepsCountAgainstThem()
    {
        var parent = PipelineRunTimeAndStepLimits.Start(DateTimeOffset.UtcNow, maxRunSeconds: 100, maxSteps: 3);
        var start = Start() with { ParentLimits = parent };

        var result = await Sut().Start(start, ChildWithNoLimitsOfItsOwn, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var limits = (await _processor.NextCall(Patience)).Request.Limits.ShouldNotBeNull();
        limits.Deadline.ShouldBe(parent.Deadline);
        limits.TryTakeStep().ShouldBeTrue();
        limits.TryTakeStep().ShouldBeTrue();
        limits.TryTakeStep().ShouldBeTrue();
        parent.StepsLeft.ShouldBe(0);
        limits.TryTakeStep().ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AChildsOwnShorterLimitsNarrowTheParentsAndNeverExtendThem()
    {
        var parent = PipelineRunTimeAndStepLimits.Start(DateTimeOffset.UtcNow, maxRunSeconds: 100, maxSteps: 50);
        var child = new GraphPipelineConfiguration
        {
            Id = ChildWithNoLimitsOfItsOwn.Id, Name = "Report", Implementation = "Graph", MaxRunSeconds = 10, MaxSteps = 2,
        };

        var result = await Sut().Start(Start() with { ParentLimits = parent }, child, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var limits = (await _processor.NextCall(Patience)).Request.Limits.ShouldNotBeNull();
        limits.Deadline.ShouldBeLessThan(parent.Deadline);
        limits.StepsLeft.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheRowOfAChildOfAnAtOnceRunStatesAtOnceWhateverItsPipelineTypeSays()
    {
        var parent = PipelineRunTimeAndStepLimits.Start(DateTimeOffset.UtcNow, maxRunSeconds: 100, maxSteps: 3);

        var result = await Sut().Start(Start() with { ParentLimits = parent }, ChildWithNoLimitsOfItsOwn, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _written.OfType<InsertCommand<EtlPipelineExecutionRecord>>().ShouldHaveSingleItem().Data.Single().RunMode.ShouldBe("AtOnce");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task APipelineThatStatesNoLimitsAndHasNoParentIsStillRefusedNamingTheLimit()
    {
        var result = await Sut().Start(Start(), ChildWithNoLimitsOfItsOwn, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(m => m.Code).ShouldContain("PIPELINERUN-20845");
        NothingWasOpened();
    }

    /// <summary>Stands in for the shared run code: records each call and holds the run until the test lets it end.</summary>
    private sealed class RecordingProcessor : IPipelineRequestProcessor, IDisposable
    {
        private readonly List<Call> _calls = [];
        private readonly SemaphoreSlim _arrived = new(0);
        private readonly TaskCompletionSource _end = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReadOnlyList<Call> Calls => _calls;

        public bool Finished { get; private set; }

        public void Release() => _end.TrySetResult();

        public void Dispose() => _arrived.Dispose();

        public async Task<Call> NextCall(TimeSpan patience)
        {
            if (!await _arrived.WaitAsync(patience).ConfigureAwait(false))
                throw new TimeoutException("the run code was not called");

            return _calls[^1];
        }

        public async Task ProcessRequest(PipelineExecutionRequest request, PipelineRunRefusal? refusal, PipelineRunHandle run, CancellationToken stoppingToken)
        {
            _calls.Add(new Call(request, refusal, run, stoppingToken));
            _ = _arrived.Release();
            await _end.Task.ConfigureAwait(false);
            Finished = true;
        }
    }

    private sealed record Call(PipelineExecutionRequest Request, PipelineRunRefusal? Refusal, PipelineRunHandle Run, CancellationToken StoppingToken);
}
