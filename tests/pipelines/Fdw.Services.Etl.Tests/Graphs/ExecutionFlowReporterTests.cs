using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Moq;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for <see cref="ExecutionFlowReporter"/> (FDW-1602): each step of a tracked run is a stage under the run, a data flow's stage
/// carries an event for each component and each link, and a record that cannot be written never fails the run.
/// </summary>
public sealed class ExecutionFlowReporterTests
{
    private static readonly Guid Run = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Stage = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Tenant = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly Mock<IExecutionTracker> _tracker = new();

    private ExecutionFlowReporter Reporter() => new(_tracker.Object, Run, Tenant);

    private void StageIsCreated()
    {
        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.Id).Returns(Stage);
        _tracker
            .Setup(t => t.CreateItem(
                It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExecutionItem>.Success(item.Object));
        _tracker
            .Setup(t => t.TransitionState(It.IsAny<Guid>(), It.IsAny<IExecutionStateType>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        _tracker
            .Setup(t => t.Complete(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        _tracker
            .Setup(t => t.RecordEvent(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExecutionEvent>.Success(new Mock<IExecutionEvent>().Object));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStepOfTheRunIsAStageUnderTheRunThatGoesRunning()
    {
        StageIsCreated();

        var stage = await Reporter().StepStarting("load", null, TestContext.Current.CancellationToken);

        stage.ShouldBe(Stage);
        _tracker.Verify(t => t.CreateItem(
            ExecutionItemTypes.Stage, "load", Tenant, Run, It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Once);
        _tracker.Verify(t => t.TransitionState(Stage, ExecutionStateTypes.Running, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStepInsideAnotherIsAStageUnderThatStage()
    {
        StageIsCreated();

        await Reporter().StepStarting("inner", Stage, TestContext.Current.CancellationToken);

        _tracker.Verify(t => t.CreateItem(
            ExecutionItemTypes.Stage, "inner", Tenant, Stage, It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStageThatCannotBeCreatedIsNoStageAndTheRunIsNotFailed()
    {
        _tracker
            .Setup(t => t.CreateItem(
                It.IsAny<IExecutionItemType>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExecutionItem>.Failure(new GenericMessage("the store is not named")));

        var stage = await Reporter().StepStarting("load", null, TestContext.Current.CancellationToken);

        stage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASuccessfulStepCompletesAndAFailedOneFails()
    {
        StageIsCreated();
        var reporter = Reporter();

        await reporter.StepFinished(Stage, GenericResult.Success(), TestContext.Current.CancellationToken);
        await reporter.StepFinished(Stage, GenericResult.Failure(new GenericMessage("the load failed")), TestContext.Current.CancellationToken);
        await reporter.StepCancelled(Stage, TestContext.Current.CancellationToken);

        _tracker.Verify(t => t.Complete(Stage, true, "Completed", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _tracker.Verify(t => t.Complete(Stage, false, "Failed", "the load failed", It.IsAny<CancellationToken>()), Times.Once);
        _tracker.Verify(t => t.Complete(Stage, false, "Cancelled", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADataFlowRecordsAnEventForEachComponentAndEachLinkWithTheirNumbers()
    {
        StageIsCreated();
        var events = new List<(string Type, IReadOnlyDictionary<string, object?> Data)>();
        _tracker
            .Setup(t => t.RecordEvent(
                Stage, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string?, IReadOnlyDictionary<string, object?>?, string?, CancellationToken>((_, type, _, data, _, _) =>
                events.Add((type, data!)))
            .ReturnsAsync(GenericResult<IExecutionEvent>.Success(new Mock<IExecutionEvent>().Object));
        var start = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        var report = new FlowRunReport(
            [new FlowLinkCount(new FlowLink("src", "output", "dst", "input"), 10, 4)],
            TimeSpan.FromSeconds(2),
            [new FlowComponentReport("dst", start.AddSeconds(1), start.AddSeconds(2), true), new FlowComponentReport("src", start, start.AddSeconds(1), true)]);

        await Reporter().FlowFinished(Stage, report, TestContext.Current.CancellationToken);

        events.Select(e => e.Type).ShouldBe([ExecutionFlowReporter.ComponentEvent, ExecutionFlowReporter.ComponentEvent, ExecutionFlowReporter.LinkEvent]);
        events[0].Data["component"].ShouldBe("src");
        events[0].Data["rowsOut"].ShouldBe(10L);
        events[0].Data["succeeded"].ShouldBe(true);
        events[1].Data["rowsOut"].ShouldBe(0L);
        events[2].Data["rows"].ShouldBe(10L);
        events[2].Data["batches"].ShouldBe(4L);
        events[2].Data["to"].ShouldBe("dst");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AComponentEventCarriesTheStageNumberOnlyWhenTheComponentHasOne()
    {
        StageIsCreated();
        var events = new List<(string Type, IReadOnlyDictionary<string, object?> Data)>();
        _tracker
            .Setup(t => t.RecordEvent(
                Stage, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string?, IReadOnlyDictionary<string, object?>?, string?, CancellationToken>((_, type, _, data, _, _) =>
                events.Add((type, data!)))
            .ReturnsAsync(GenericResult<IExecutionEvent>.Success(new Mock<IExecutionEvent>().Object));
        var start = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
        var report = new FlowRunReport(
            [],
            TimeSpan.FromSeconds(1),
            [new FlowComponentReport("early", start, start.AddSeconds(1), true), new FlowComponentReport("later", start.AddSeconds(1), start.AddSeconds(2), true, 2)]);

        await Reporter().FlowFinished(Stage, report, TestContext.Current.CancellationToken);

        var byComponent = events.Where(e => e.Type == ExecutionFlowReporter.ComponentEvent).ToDictionary(e => (string)e.Data["component"]!, e => e.Data);
        byComponent["early"].ContainsKey("stage").ShouldBeFalse();
        byComponent["later"]["stage"].ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheComponentThatCausedAFailureIsMarkedFailedAndTheOthersAreNot()
    {
        StageIsCreated();
        var events = new List<(string Type, IReadOnlyDictionary<string, object?> Data)>();
        _tracker
            .Setup(t => t.RecordEvent(
                Stage, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string?, IReadOnlyDictionary<string, object?>?, string?, CancellationToken>((_, type, _, data, _, _) =>
                events.Add((type, data!)))
            .ReturnsAsync(GenericResult<IExecutionEvent>.Success(new Mock<IExecutionEvent>().Object));
        var start = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        var report = new FlowRunReport(
            [new FlowLinkCount(new FlowLink("src", "output", "validate", "input"), 7, 2)],
            TimeSpan.FromSeconds(1),
            [new FlowComponentReport("src", start, start.AddSeconds(1), false), new FlowComponentReport("validate", start, start.AddSeconds(1), false)],
            "validate");

        await Reporter().FlowFinished(Stage, report, TestContext.Current.CancellationToken);

        var failed = events.Where(e => e.Type == ExecutionFlowReporter.ComponentEvent).ToDictionary(e => (string)e.Data["component"]!, e => e.Data["failed"]);
        failed["src"].ShouldBe(false);
        failed["validate"].ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheRowsAnErrorOutputCarriedAreRecordedAsACountAndTheirReasonsWithNoRow()
    {
        StageIsCreated();
        var events = new List<(string Type, string? Message, IReadOnlyDictionary<string, object?> Data)>();
        _tracker
            .Setup(t => t.RecordEvent(
                Stage, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string?, IReadOnlyDictionary<string, object?>?, string?, CancellationToken>((_, type, message, data, _, _) =>
                events.Add((type, message, data!)))
            .ReturnsAsync(GenericResult<IExecutionEvent>.Success(new Mock<IExecutionEvent>().Object));
        var report = new FlowRunReport(
            [], TimeSpan.FromSeconds(1), [], null, [new FlowRejection("check", "bad", 5, [new FlowRejectionReason("Big", 4), new FlowRejectionReason("Odd", 1)])]);

        await Reporter().FlowFinished(Stage, report, TestContext.Current.CancellationToken);

        var rejected = events.ShouldHaveSingleItem();
        rejected.Type.ShouldBe(ExecutionFlowReporter.RejectedRowsEvent);
        rejected.Message.ShouldBe("Component 'check' rejected 5 row(s) through 'bad'");
        rejected.Data["component"].ShouldBe("check");
        rejected.Data["output"].ShouldBe("bad");
        rejected.Data["count"].ShouldBe(5L);
        rejected.Data.ContainsKey("rows").ShouldBeFalse();
        rejected.Data.ContainsKey("kept").ShouldBeFalse();
        var reasons = ((List<Dictionary<string, object?>>)rejected.Data["reasons"]!);
        reasons.Count.ShouldBe(2);
        reasons[0]["reason"].ShouldBe("Big");
        reasons[0]["count"].ShouldBe(4L);
        reasons[1]["reason"].ShouldBe("Odd");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AReportThatRejectedNothingRecordsNoRejectionEvent()
    {
        StageIsCreated();
        var report = new FlowRunReport([], TimeSpan.FromSeconds(1), []);

        await Reporter().FlowFinished(Stage, report, TestContext.Current.CancellationToken);

        _tracker.Verify(
            t => t.RecordEvent(
                It.IsAny<Guid>(), ExecutionFlowReporter.RejectedRowsEvent, It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
