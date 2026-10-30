using Fdw.Abstractions;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for keeping the request of a pipeline run that has not started in etl.PipelineRunRequest and reading the runs back after a restart (FDW-3216):
/// what is written, a run accepted a second time, the hold, what is removed, and how a restart sorts the stored requests by what their run is now.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineRunRequestStoreTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000003");
    private static readonly DateTimeOffset Created = new(2026, 10, 29, 20, 0, 0, TimeSpan.Zero);

    private readonly string? _before = EtlPipelineTypes.OperationalConnection;
    private readonly Mock<IDataGateway> _gateway = new();
    private readonly Mock<IExecutionTracker> _tracker = new();
    private readonly List<(IDataCommand Command, DataStoreTarget Target)> _calls = [];
    private readonly PipelineRunRequestStore _store;

    public PipelineRunRequestStoreTests()
    {
        EtlPipelineTypes.OperationalConnection = "Ops";
        var gateways = new Mock<IDataGatewayProvider>();
        gateways.Setup(provider => provider.ByName("Main")).Returns(_gateway.Object);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupProperty(a => a.Current);
        var services = new ServiceCollection().AddSingleton(gateways.Object).AddSingleton(_tracker.Object).AddSingleton(accessor.Object).BuildServiceProvider();
        _store = new PipelineRunRequestStore(services.GetRequiredService<IServiceScopeFactory>());
    }

    public void Dispose() => EtlPipelineTypes.OperationalConnection = _before;

    private static PipelineExecutionRequest Request(Guid id, string pipeline = "monthly-sales", Guid? tenant = null, object? value = null) => new()
    {
        ExecutionId = id,
        PipelineId = Guid.NewGuid(),
        PipelineName = pipeline,
        TriggerSource = "Api",
        TenantId = tenant ?? Tenant,
        RunRoles = ["DataWriter"],
        Parameters = new Dictionary<string, object?> { ["Size"] = value ?? 5 }
    };

    // The writes (insert, update, delete) answer in turn, and every call is noted.
    private void WritesReturn(params IGenericResult<int>[] results)
    {
        var next = new Queue<IGenericResult<int>>(results);
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataStoreTarget target, CancellationToken token) =>
            {
                _calls.Add((command, target));
                return Task.FromResult(next.Count > 0 ? next.Dequeue() : GenericResult<int>.Success(1));
            });
    }

    private void RowsAre(params EtlPipelineRunRequestRecord[] rows) =>
        _gateway
            .Setup(g => g.Execute<IEnumerable<EtlPipelineRunRequestRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataStoreTarget target, CancellationToken token) =>
            {
                _calls.Add((command, target));
                return Task.FromResult<IGenericResult<IEnumerable<EtlPipelineRunRequestRecord>>>(GenericResult<IEnumerable<EtlPipelineRunRequestRecord>>.Success(rows));
            });

    private static EtlPipelineRunRequestRecord Row(PipelineExecutionRequest request, DateTimeOffset? heldUntil = null) => new()
    {
        ExecutionId = request.ExecutionId,
        TenantId = Tenant,
        Request = PipelineRunRequestJson.Write(request, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance).Value!,
        HeldUntil = heldUntil
    };

    private void ItemIs(Guid id, IExecutionStateType state, int minutesOld)
    {
        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.Id).Returns(id);
        item.SetupGet(i => i.State).Returns(state);
        item.SetupGet(i => i.CreatedAt).Returns(Created.AddMinutes(-minutesOld));
        _tracker.Setup(t => t.GetItem(id, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Success(item.Object));
    }

    private void NoUnstartedRecordsWithoutRow() => _tracker
        .Setup(t => t.ListExecutions(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IExecutionItemType?>(), It.IsAny<IExecutionStateType?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(GenericResult<IPagedResponse<IExecutionItem>>.Success(new PagedResponse<IExecutionItem>([], 1, 100, 0)));

    private static List<(string Property, string Operator, object? Value)> Conditions(IFilterExpression? filter) => filter?.Root switch
    {
        FilterCondition condition => [(condition.PropertyName, condition.Operator.Name, condition.Value)],
        FilterGroup group => [.. group.Nodes.Cast<FilterCondition>().Select(c => (c.PropertyName, c.Operator.Name, c.Value))],
        _ => [],
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheRequestIsInsertedAsARowOfTheOperationalStoreAndReadsBackAsTheSameRequest()
    {
        WritesReturn(GenericResult<int>.Success(1));
        var id = Guid.NewGuid();

        var result = await _store.Record(Request(id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var (command, target) = _calls.Single();
        target.ShouldBe(new DataStoreTarget("Ops", "etl", "PipelineRunRequest"));
        var row = command.ShouldBeOfType<InsertCommand<EtlPipelineRunRequestRecord>>().Data.Single();
        row.ExecutionId.ShouldBe(id);
        row.TenantId.ShouldBe(Tenant);
        row.HeldUntil.ShouldBeNull();
        var back = PipelineRunRequestJson.Read(id, row.Request, null, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance).Value!;
        back.PipelineName.ShouldBe("monthly-sales");
        back.Parameters.ShouldNotBeNull()["Size"].ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunAcceptedASecondTimeHasItsRequestRewrittenInItsRow()
    {
        WritesReturn(GenericResult<int>.Failure(new GenericMessage("duplicate key")), GenericResult<int>.Success(1));
        var id = Guid.NewGuid();

        var result = await _store.Record(Request(id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var update = _calls[1].Command.ShouldBeOfType<UpdateCommand<EtlPipelineRunRequestText>>();
        update.Data.Request.ShouldContain("monthly-sales");
        Conditions(update.Filter).ShouldBe([("ExecutionId", "Equal", (object?)id)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARequestThatCannotBeWrittenIsAFailureNamingTheRunAndTheReason()
    {
        WritesReturn(GenericResult<int>.Failure(new GenericMessage("the store is down")), GenericResult<int>.Success(0));
        var id = Guid.NewGuid();

        var result = await _store.Record(Request(id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(id.ToString());
        result.CurrentMessage.ShouldContain("the store is down");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunWithNoTenantAValueThatCannotBeKeptAndAHostWithNoStoreAreEachRefusedBeforeAnythingIsWritten()
    {
        WritesReturn(GenericResult<int>.Success(1));
        var noTenant = new PipelineExecutionRequest { ExecutionId = Guid.NewGuid(), PipelineId = Guid.NewGuid(), PipelineName = "p", TriggerSource = "Api", RunRoles = [] };

        var none = await _store.Record(noTenant, TestContext.Current.CancellationToken);
        var odd = await _store.Record(Request(Guid.NewGuid(), value: new object()), TestContext.Current.CancellationToken);
        EtlPipelineTypes.OperationalConnection = null;
        var unnamed = await _store.Record(Request(Guid.NewGuid()), TestContext.Current.CancellationToken);

        none.IsFailure.ShouldBeTrue();
        odd.IsFailure.ShouldBeTrue();
        odd.CurrentMessage.ShouldNotBeNull().ShouldContain("Size");
        unnamed.IsFailure.ShouldBeTrue();
        _calls.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheHoldIsWrittenToTheRowOfTheRunAndAHoldWithNoRowIsAFailure()
    {
        WritesReturn(GenericResult<int>.Success(1), GenericResult<int>.Success(0));
        var id = Guid.NewGuid();
        var due = new DateTimeOffset(2026, 10, 30, 2, 0, 0, TimeSpan.Zero);

        var held = await _store.RecordHeld(Request(id), due, TestContext.Current.CancellationToken);
        var missing = await _store.RecordHeld(Request(Guid.NewGuid()), due, TestContext.Current.CancellationToken);

        held.IsSuccess.ShouldBeTrue(held.CurrentMessage);
        var update = _calls[0].Command.ShouldBeOfType<UpdateCommand<EtlPipelineRunRequestHold>>();
        update.Data.HeldUntil.ShouldBe(due);
        Conditions(update.Filter).ShouldBe([("ExecutionId", "Equal", (object?)id)]);
        missing.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ForgetDeletesTheRowOfTheRun()
    {
        WritesReturn(GenericResult<int>.Success(1));
        var id = Guid.NewGuid();

        var result = await _store.Forget(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var (command, target) = _calls.Single();
        target.ShouldBe(new DataStoreTarget("Ops", "etl", "PipelineRunRequest"));
        Conditions(command.ShouldBeOfType<DeleteCommand>().Filter).ShouldBe([("ExecutionId", "Equal", (object?)id)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AfterARestartTheWaitingRunsComeBackOldestFirstAHeldRunWithItsTimeAndARunWaitingForApprovalAsAnApproval()
    {
        var oldest = Request(Guid.NewGuid(), "held-one");
        var newest = Request(Guid.NewGuid(), "waiting-one");
        var approval = Request(Guid.NewGuid(), "approval-one");
        var due = new DateTimeOffset(2026, 10, 30, 2, 0, 0, TimeSpan.Zero);
        RowsAre(Row(newest), Row(approval), Row(oldest, due));
        ItemIs(oldest.ExecutionId, ExecutionStateTypes.Scheduled, 30);
        ItemIs(newest.ExecutionId, ExecutionStateTypes.Scheduled, 5);
        ItemIs(approval.ExecutionId, ExecutionStateTypes.AwaitingApproval, 10);
        NoUnstartedRecordsWithoutRow();
        WritesReturn();

        var recovery = await _store.Recover(TestContext.Current.CancellationToken);

        recovery.IsSuccess.ShouldBeTrue(recovery.CurrentMessage);
        recovery.Value!.Requests.Select(request => request.PipelineName).ShouldBe(["held-one", "waiting-one"]);
        recovery.Value.Requests[0].HeldUntil.ShouldBe(due);
        recovery.Value.Requests[1].HeldUntil.ShouldBeNull();
        recovery.Value.Approvals.Select(request => request.PipelineName).ShouldBe(["approval-one"]);
        recovery.Value.WithoutRequest.ShouldBe(0);
        _calls.Count(call => call.Command is DeleteCommand).ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoredRequestWhoseRunHasMovedOnIsRemovedAndNeverStarted()
    {
        var finished = Request(Guid.NewGuid(), "finished-one");
        RowsAre(Row(finished));
        ItemIs(finished.ExecutionId, ExecutionStateTypes.Completed, 60);
        NoUnstartedRecordsWithoutRow();
        WritesReturn();

        var recovery = await _store.Recover(TestContext.Current.CancellationToken);

        recovery.IsSuccess.ShouldBeTrue(recovery.CurrentMessage);
        recovery.Value!.Requests.ShouldBeEmpty();
        recovery.Value.Approvals.ShouldBeEmpty();
        var removal = _calls.Select(call => call.Command).OfType<DeleteCommand>().ShouldHaveSingleItem();
        Conditions(removal.Filter).ShouldBe([("ExecutionId", "Equal", (object?)finished.ExecutionId)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoredRequestThatCannotBeReadClosesItsRunAsFailedAndIsRemoved()
    {
        var id = Guid.NewGuid();
        RowsAre(new EtlPipelineRunRequestRecord { ExecutionId = id, TenantId = Tenant, Request = "not json" });
        ItemIs(id, ExecutionStateTypes.Scheduled, 5);
        NoUnstartedRecordsWithoutRow();
        WritesReturn();
        _tracker.Setup(t => t.Complete(id, false, "RunRequestUnreadable", It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var recovery = await _store.Recover(TestContext.Current.CancellationToken);

        recovery.IsSuccess.ShouldBeTrue(recovery.CurrentMessage);
        recovery.Value!.Requests.ShouldBeEmpty();
        _tracker.Verify(t => t.Complete(id, false, "RunRequestUnreadable", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _calls.Select(call => call.Command).OfType<DeleteCommand>().ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunWhoseRecordCannotBeReadKeepsItsStoredRequestAndIsNotTakenUp()
    {
        var id = Guid.NewGuid();
        RowsAre(Row(Request(id)));
        _tracker.Setup(t => t.GetItem(id, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Failure(new GenericMessage("the store is down")));
        NoUnstartedRecordsWithoutRow();
        WritesReturn();

        var recovery = await _store.Recover(TestContext.Current.CancellationToken);

        recovery.IsSuccess.ShouldBeTrue(recovery.CurrentMessage);
        recovery.Value!.Requests.ShouldBeEmpty();
        _calls.Select(call => call.Command).OfType<DeleteCommand>().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task UnstartedRecordsWithNoStoredRequestAreCountedAndLeftAsTheyAre()
    {
        var withRow = Request(Guid.NewGuid());
        var left = new[] { Guid.NewGuid(), Guid.NewGuid() };
        RowsAre(Row(withRow));
        ItemIs(withRow.ExecutionId, ExecutionStateTypes.Scheduled, 5);
        IExecutionItem Listed(Guid id)
        {
            var item = new Mock<IExecutionItem>();
            item.SetupGet(i => i.Id).Returns(id);
            return item.Object;
        }

        _tracker
            .Setup(t => t.ListExecutions(1, 100, ExecutionItemTypes.Job, ExecutionStateTypes.Scheduled, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPagedResponse<IExecutionItem>>.Success(
                new PagedResponse<IExecutionItem>([Listed(withRow.ExecutionId), Listed(left[0]), Listed(left[1])], 1, 100, 3)));
        _tracker
            .Setup(t => t.ListExecutions(1, 100, ExecutionItemTypes.Job, ExecutionStateTypes.AwaitingApproval, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPagedResponse<IExecutionItem>>.Success(new PagedResponse<IExecutionItem>([], 1, 100, 0)));
        WritesReturn();

        var recovery = await _store.Recover(TestContext.Current.CancellationToken);

        recovery.IsSuccess.ShouldBeTrue(recovery.CurrentMessage);
        recovery.Value!.Requests.ShouldHaveSingleItem();
        recovery.Value.WithoutRequest.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task StoredRequestsThatCannotBeReadAreAFailureAndNeverAnEmptyRecovery()
    {
        _gateway
            .Setup(g => g.Execute<IEnumerable<EtlPipelineRunRequestRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<EtlPipelineRunRequestRecord>>.Failure(new GenericMessage("the store is down")));

        var recovery = await _store.Recover(TestContext.Current.CancellationToken);

        recovery.IsFailure.ShouldBeTrue();
    }

    private void CreatedItemIs(Guid id)
    {
        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.Id).Returns(id);
        item.SetupGet(i => i.State).Returns(ExecutionStateTypes.Triggered);
        _tracker
            .Setup(t => t.CreateItem(
                It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExecutionItem>.Success(item.Object));
        _tracker
            .Setup(t => t.Complete(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatWasRunningWhenTheHostStoppedComesBackAsInterruptedWithItsRestartsAndNotAsAWaitingRun()
    {
        var running = Request(Guid.NewGuid(), "running-one");
        var row = Row(running);
        row.Restarts = 1;
        RowsAre(row);
        ItemIs(running.ExecutionId, ExecutionStateTypes.Running, 20);
        NoUnstartedRecordsWithoutRow();
        WritesReturn();

        var recovery = await _store.Recover(TestContext.Current.CancellationToken);

        recovery.IsSuccess.ShouldBeTrue(recovery.CurrentMessage);
        recovery.Value!.Requests.ShouldBeEmpty();
        recovery.Value.Approvals.ShouldBeEmpty();
        var interrupted = recovery.Value.Interrupted.ShouldHaveSingleItem();
        interrupted.Request.PipelineName.ShouldBe("running-one");
        interrupted.Restarts.ShouldBe(1);
        _calls.Count(call => call.Command is DeleteCommand).ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task StartingARunAgainStoresTheNewRunWithOneMoreRestartClosesTheOldRunNamingTheNewOneAndRemovesTheOldRequest()
    {
        var old = Request(Guid.NewGuid(), "running-one");
        var newId = Guid.NewGuid();
        CreatedItemIs(newId);
        WritesReturn();

        var again = await _store.StartAgain(new InterruptedRun(old, 1), TestContext.Current.CancellationToken);

        again.IsSuccess.ShouldBeTrue(again.CurrentMessage);
        again.Value!.ExecutionId.ShouldBe(newId);
        again.Value.PipelineId.ShouldBe(old.PipelineId);
        again.Value.TenantId.ShouldBe(Tenant);
        again.Value.Parameters.ShouldNotBeNull()["Size"].ShouldBe(5);
        var stored = _calls.Select(call => call.Command).OfType<InsertCommand<EtlPipelineRunRequestRecord>>().ShouldHaveSingleItem().Data.Single();
        stored.ExecutionId.ShouldBe(newId);
        stored.Restarts.ShouldBe(2);
        _calls.Select(call => call.Command).OfType<InsertCommand<EtlPipelineExecutionRecord>>().ShouldHaveSingleItem().Data.Single().Id.ShouldBe(newId);
        _tracker.Verify(
            t => t.Complete(old.ExecutionId, false, "ServerStopped", It.Is<string?>(message => message!.Contains(newId.ToString())), It.IsAny<CancellationToken>()),
            Times.Once);
        var removal = _calls.Select(call => call.Command).OfType<DeleteCommand>().ShouldHaveSingleItem();
        Conditions(removal.Filter).ShouldBe([("ExecutionId", "Equal", (object?)old.ExecutionId)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANewRunWhoseRequestCannotBeStoredIsClosedAndTheOldRunIsLeftAsItWas()
    {
        var old = Request(Guid.NewGuid(), "running-one");
        var newId = Guid.NewGuid();
        CreatedItemIs(newId);
        // The metrics row is written first; then the request is inserted and, when that fails, rewritten in place: both of those writes fail here.
        WritesReturn(
            GenericResult<int>.Success(1), GenericResult<int>.Failure(new GenericMessage("table missing")), GenericResult<int>.Failure(new GenericMessage("table missing")));

        var again = await _store.StartAgain(new InterruptedRun(old, 0), TestContext.Current.CancellationToken);

        again.IsFailure.ShouldBeTrue();
        _tracker.Verify(t => t.Complete(newId, false, "RunRequestNotStored", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _tracker.Verify(t => t.Complete(old.ExecutionId, It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _calls.Select(call => call.Command).OfType<DeleteCommand>().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunWithNoTenantCannotBeStartedAgain()
    {
        var old = new PipelineExecutionRequest { ExecutionId = Guid.NewGuid(), PipelineId = Guid.NewGuid(), PipelineName = "running-one", TriggerSource = "Api", RunRoles = ["DataWriter"] };

        var again = await _store.StartAgain(new InterruptedRun(old, 0), TestContext.Current.CancellationToken);

        again.IsFailure.ShouldBeTrue();
        _tracker.Verify(
            t => t.CreateItem(
                It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task GivingUpOnARunClosesItWithTheReasonAndRemovesItsRequestWithoutMakingANewRun()
    {
        var old = Request(Guid.NewGuid(), "running-one");
        CreatedItemIs(Guid.NewGuid());
        WritesReturn();

        var result = await _store.GiveUp(new InterruptedRun(old, 3), "it was already started again 3 times", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _tracker.Verify(
            t => t.Complete(old.ExecutionId, false, "ServerStopped", "it was already started again 3 times", It.IsAny<CancellationToken>()), Times.Once);
        _calls.Select(call => call.Command).OfType<InsertCommand<EtlPipelineRunRequestRecord>>().ShouldBeEmpty();
        Conditions(_calls.Select(call => call.Command).OfType<DeleteCommand>().ShouldHaveSingleItem().Filter)
            .ShouldBe([("ExecutionId", "Equal", (object?)old.ExecutionId)]);
    }
}
