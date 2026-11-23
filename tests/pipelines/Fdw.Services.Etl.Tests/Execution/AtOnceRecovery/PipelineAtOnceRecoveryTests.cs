using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Services.Etl.Execution.AtOnceRecovery;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.AtOnceRecovery;

/// <summary>
/// FDW-6986, slice 4 of FDW-6141: a run carried out at once whose durable claim lapsed is half-done, and ends as its pipeline's half-done rule says (End: Interrupted, never runs again). The claim
/// lease is the only test: a run another live instance is carrying on renews its claim, so its row never lapses and is never seen here; a queued run, a run already ended and a run with no record are
/// left as they are; a run whose pipeline or rule cannot be judged is left open and named at every sweep, and no rule is assumed for it.
/// </summary>
public sealed class PipelineAtOnceRecoveryTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Pipeline = Guid.Parse("44444444-0000-0000-0000-000000000004");
    private static readonly Guid Run = Guid.Parse("11111111-0000-0000-0000-000000000001");

    private sealed class Records : IHalfDoneRunRecords
    {
        public Dictionary<Guid, HalfDoneRunRow> Rows { get; } = [];

        public bool ReadFails { get; set; }

        public bool CloseFails { get; set; }

        public List<(Guid Id, string Reason)> Closed { get; } = [];

        public Task<IGenericResult<HalfDoneRunRow?>> Read(Guid tenantId, Guid executionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<HalfDoneRunRow?>>(ReadFails
                ? GenericResult<HalfDoneRunRow?>.Failure(PipelineAtOnceRecoveryLog.RunRecordNotReadable(NullLogger.Instance, executionId, "the store is away"))
                : GenericResult<HalfDoneRunRow?>.Success(Rows.TryGetValue(executionId, out var row) ? row : null));

        public Task<IGenericResult> Close(Guid tenantId, Guid executionId, string reason, CancellationToken cancellationToken = default)
        {
            if (CloseFails)
                return Task.FromResult<IGenericResult>(GenericResult.Failure(PipelineAtOnceRecoveryLog.RunNotClosed(NullLogger.Instance, executionId, "the store is away")));

            Closed.Add((executionId, reason));
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }
    }

    private sealed class Pipelines : IHalfDonePipelines
    {
        public string? Rule { get; set; } = "End";

        public bool Unreadable { get; set; }

        public Task<IGenericResult<string?>> RuleOf(Guid tenantId, string pipelineName, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<string?>>(Unreadable
                ? GenericResult<string?>.Failure(PipelineAtOnceRecoveryLog.PipelineNotReadable(NullLogger.Instance, Run, pipelineName, "no such pipeline"))
                : GenericResult<string?>.Success(Rule));
    }

    private static PipelineRunLapsedClaim Lapsed(Guid id) => new(id, Tenant, Pipeline, "FdwReference.Etl.Server", null, "Claimed");

    private static HalfDoneRunRow OpenAtOnce(Guid id) => new(id, "Chat", "AtOnce", true);

    private static PipelineAtOnceRecovery Recovery(Records records, Pipelines pipelines) => new(records, pipelines, NullLogger<PipelineAtOnceRecovery>.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALapsedClaimOfAnOpenAtOnceRunEndsItInterruptedWithTheRuleNamedAndOnlyOnce()
    {
        var records = new Records();
        records.Rows[Run] = OpenAtOnce(Run);
        var sut = Recovery(records, new Pipelines());

        sut.Seen([Lapsed(Run)]);
        await sut.Settle(TestContext.Current.CancellationToken);
        await sut.Settle(TestContext.Current.CancellationToken);

        var closed = records.Closed.ShouldHaveSingleItem();
        closed.Id.ShouldBe(Run);
        closed.Reason.ShouldContain("End");
        closed.Reason.ShouldContain("Chat");
        sut.Pending.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheReasonIsTheSameTextWhicheverTimeItIsWritten()
    {
        var first = new Records();
        first.Rows[Run] = OpenAtOnce(Run);
        var second = new Records();
        second.Rows[Run] = OpenAtOnce(Run);
        var one = Recovery(first, new Pipelines());
        var two = Recovery(second, new Pipelines());

        one.Seen([Lapsed(Run)]);
        await one.Settle(TestContext.Current.CancellationToken);
        await Task.Delay(5, TestContext.Current.CancellationToken);
        two.Seen([Lapsed(Run)]);
        await two.Settle(TestContext.Current.CancellationToken);

        first.Closed.Single().Reason.ShouldBe(second.Closed.Single().Reason);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task NothingIsClosedWhenNoClaimLapsedWhateverRunsAreOpen()
    {
        // A run another live instance carries on (the same application name and slot) renews its claim inside the lease, so its row never lapses and the recovery is never told of it.
        var records = new Records();
        records.Rows[Run] = OpenAtOnce(Run);
        var sut = Recovery(records, new Pipelines());

        await sut.Settle(TestContext.Current.CancellationToken);

        records.Closed.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Queued", true)]
    [InlineData("AtOnce", false)]
    [InlineData(null, true)]
    public async Task AQueuedRunARunAlreadyEndedAndARunOfNoModeAreLeftAsTheyAre(string? runMode, bool open)
    {
        var records = new Records();
        records.Rows[Run] = new HalfDoneRunRow(Run, "Chat", runMode, open);
        var sut = Recovery(records, new Pipelines());

        sut.Seen([Lapsed(Run)]);
        await sut.Settle(TestContext.Current.CancellationToken);

        records.Closed.ShouldBeEmpty();
        sut.Pending.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunWithNoRecordIsLeftAsItIs()
    {
        var records = new Records();
        var sut = Recovery(records, new Pipelines());

        sut.Seen([Lapsed(Run)]);
        await sut.Settle(TestContext.Current.CancellationToken);

        records.Closed.ShouldBeEmpty();
        sut.Pending.ShouldBe(0);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("StartAgain")]
    public async Task AnUnusableRuleLeavesTheRunOpenNamedAtEverySweepAndNoRuleIsAssumed(string? rule)
    {
        var records = new Records();
        records.Rows[Run] = OpenAtOnce(Run);
        var pipelines = new Pipelines { Rule = rule };
        var sut = Recovery(records, pipelines);

        sut.Seen([Lapsed(Run)]);
        await sut.Settle(TestContext.Current.CancellationToken);
        await sut.Settle(TestContext.Current.CancellationToken);

        records.Closed.ShouldBeEmpty();
        sut.Pending.ShouldBe(1);

        pipelines.Rule = "End";
        await sut.Settle(TestContext.Current.CancellationToken);

        records.Closed.ShouldHaveSingleItem().Id.ShouldBe(Run);
        sut.Pending.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatCannotBeReadLeavesTheRunOpenAndItIsJudgedAtTheNextSweep()
    {
        var records = new Records();
        records.Rows[Run] = OpenAtOnce(Run);
        var pipelines = new Pipelines { Unreadable = true };
        var sut = Recovery(records, pipelines);

        sut.Seen([Lapsed(Run)]);
        await sut.Settle(TestContext.Current.CancellationToken);

        records.Closed.ShouldBeEmpty();
        sut.Pending.ShouldBe(1);

        pipelines.Unreadable = false;
        await sut.Settle(TestContext.Current.CancellationToken);

        records.Closed.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARecordThatCannotBeReadOrClosedIsTriedAgainAndNothingIsAssumed()
    {
        var records = new Records { ReadFails = true };
        records.Rows[Run] = OpenAtOnce(Run);
        var sut = Recovery(records, new Pipelines());

        sut.Seen([Lapsed(Run)]);
        await sut.Settle(TestContext.Current.CancellationToken);
        sut.Pending.ShouldBe(1);

        records.ReadFails = false;
        records.CloseFails = true;
        await sut.Settle(TestContext.Current.CancellationToken);
        sut.Pending.ShouldBe(1);

        records.CloseFails = false;
        await sut.Settle(TestContext.Current.CancellationToken);

        records.Closed.Count.ShouldBe(1);
        sut.Pending.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task EveryLapsedClaimOfOneAnswerIsJudged()
    {
        var other = Guid.Parse("11111111-0000-0000-0000-000000000009");
        var records = new Records();
        records.Rows[Run] = OpenAtOnce(Run);
        records.Rows[other] = OpenAtOnce(other);
        var sut = Recovery(records, new Pipelines());

        sut.Seen([Lapsed(Run), Lapsed(other)]);
        await sut.Settle(TestContext.Current.CancellationToken);

        records.Closed.Select(closed => closed.Id).ShouldBe([Run, other], ignoreOrder: true);
    }

    // The host's stop and a lost claim are one case, a run that ended half done (FDW-7403): the stop asks the same judgment, by the run mode the run's own record states.

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheStopOfAnOpenAtOnceRunEndsItThroughTheSameJudgmentAndNamesTheHost()
    {
        var records = new Records();
        records.Rows[Run] = OpenAtOnce(Run);
        var sut = Recovery(records, new Pipelines());

        var ended = await sut.EndStoppedRun(Tenant, Run, TestContext.Current.CancellationToken);

        ended.Name.ShouldBe("Ended");
        var closed = records.Closed.ShouldHaveSingleItem();
        closed.Id.ShouldBe(Run);
        closed.Reason.ShouldContain("End");
        closed.Reason.ShouldContain("host");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Queued", true)]
    [InlineData("AtOnce", false)]
    public async Task TheStopOfAQueuedRunOrOfARunAlreadyEndedClosesNothingHere(string runMode, bool open)
    {
        var records = new Records();
        records.Rows[Run] = new HalfDoneRunRow(Run, "Chat", runMode, open);
        var sut = Recovery(records, new Pipelines());

        var ended = await sut.EndStoppedRun(Tenant, Run, TestContext.Current.CancellationToken);

        ended.Name.ShouldBe("NotOpenAtOnce");
        records.Closed.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheStopOfARunWithNoRecordIsNotOneEither()
    {
        var sut = Recovery(new Records(), new Pipelines());

        (await sut.EndStoppedRun(Tenant, Run, TestContext.Current.CancellationToken)).Name.ShouldBe("NotOpenAtOnce");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("record")]
    [InlineData("pipeline")]
    [InlineData("rule")]
    [InlineData("close")]
    public async Task ARunTheStopCannotJudgeIsLeftOpenAndNoRuleIsAssumed(string whatFails)
    {
        var records = new Records { ReadFails = whatFails == "record", CloseFails = whatFails == "close" };
        records.Rows[Run] = OpenAtOnce(Run);
        var pipelines = new Pipelines { Unreadable = whatFails == "pipeline", Rule = whatFails == "rule" ? "Nothing" : "End" };
        var sut = Recovery(records, pipelines);

        var ended = await sut.EndStoppedRun(Tenant, Run, TestContext.Current.CancellationToken);

        ended.Name.ShouldBe("Unjudged");
        records.Closed.ShouldBeEmpty();
    }

    // The processor's side of the stop: the run mode decides what it does, the judgment closes an at-once run's row and records, and a queued run keeps what it had.
    private sealed class StopWorld
    {
        public Records Records { get; } = new();

        public Pipelines Pipelines { get; } = new();

        public Mock<IExecutionTracker> Tracker { get; } = new();

        public Mock<IPipelineStatusBroadcaster> Broadcaster { get; } = new();

        public Mock<IExecutionCompletionSignaler> Signaler { get; } = new();

        public ServiceProvider Services { get; }

        public StopWorld()
        {
            var item = new Mock<IExecutionItem>();
            item.SetupGet(i => i.Id).Returns(Run);
            item.SetupGet(i => i.State).Returns(ExecutionStateTypes.Running);
            Tracker.Setup(t => t.GetItem(Run, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Success(item.Object));
            Tracker.Setup(t => t.GetChildren(Run, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IExecutionItem>>.Success([]));
            Tracker.Setup(t => t.Close(It.IsAny<Guid>(), It.IsAny<IExecutionStateType>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            Broadcaster.Setup(b => b.BroadcastStatusChange(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>())).Returns(Task.CompletedTask);
            Services = new ServiceCollection()
                .AddSingleton(new PipelineAtOnceRecovery(Records, Pipelines, NullLogger<PipelineAtOnceRecovery>.Instance))
                .BuildServiceProvider();
        }

        public Task<IAtOnceRunEnd> Stop(bool withTenant = true) =>
            new PipelineRequestProcessor(Mock.Of<IPipelineRunRequestStore>(), Mock.Of<IServiceScopeFactory>(), NullLogger<PipelineRequestProcessor>.Instance, Signaler.Object)
                .InterruptRun(Services, Tracker.Object, Broadcaster.Object, Request(withTenant), null);

        private static PipelineExecutionRequest Request(bool withTenant) => new()
        {
            ExecutionId = Run,
            PipelineId = Pipeline,
            PipelineName = "Chat",
            TriggerSource = "Api",
            RunRoles = ["DataWriter"],
            TenantId = withTenant ? Tenant : null,
        };
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AtTheStopAnAtOnceRunIsEndedByTheJudgmentAndNothingIsClosedASecondWay()
    {
        var world = new StopWorld();
        world.Records.Rows[Run] = OpenAtOnce(Run);

        var ended = await world.Stop();

        ended.Name.ShouldBe("Ended");
        world.Records.Closed.ShouldHaveSingleItem().Id.ShouldBe(Run);
        world.Tracker.Verify(t => t.Close(It.IsAny<Guid>(), It.IsAny<IExecutionStateType>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        world.Broadcaster.Verify(b => b.BroadcastStatusChange("Chat", Run, "Interrupted", It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Once);
        world.Signaler.Verify(s => s.Signal(Run, false, "Interrupted"), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AtTheStopAQueuedRunKeepsWhatItHadItsRecordsAreClosedAndItsRowIsLeftToTheNextStart()
    {
        var world = new StopWorld();
        world.Records.Rows[Run] = new HalfDoneRunRow(Run, "Chat", "Queued", true);

        var ended = await world.Stop();

        ended.Name.ShouldBe("NotOpenAtOnce");
        world.Records.Closed.ShouldBeEmpty();
        world.Tracker.Verify(t => t.Close(Run, ExecutionStateTypes.Interrupted, "ServerStopped", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        world.Broadcaster.Verify(b => b.BroadcastStatusChange("Chat", Run, "Interrupted", It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Once);
        world.Signaler.Verify(s => s.Signal(Run, false, "Interrupted"), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AtTheStopARunWithNoTenantIsAQueuedRunAndItsRecordsAreClosed()
    {
        var world = new StopWorld();

        var ended = await world.Stop(withTenant: false);

        ended.Name.ShouldBe("NotOpenAtOnce");
        world.Tracker.Verify(t => t.Close(Run, ExecutionStateTypes.Interrupted, "ServerStopped", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AtTheStopARunThatCannotBeJudgedIsLeftOpenUntoldAndItsClaimIsLeftToLapse()
    {
        var world = new StopWorld();
        world.Records.Rows[Run] = OpenAtOnce(Run);
        world.Pipelines.Unreadable = true;

        var ended = await world.Stop();

        ended.Name.ShouldBe("Unjudged");
        world.Records.Closed.ShouldBeEmpty();
        world.Tracker.Verify(t => t.Close(It.IsAny<Guid>(), It.IsAny<IExecutionStateType>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        world.Broadcaster.Verify(b => b.BroadcastStatusChange(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
        world.Signaler.Verify(s => s.Signal(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>()), Times.Never);
    }
}
