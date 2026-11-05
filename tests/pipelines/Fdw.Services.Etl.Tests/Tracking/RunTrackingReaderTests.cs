using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Tracking;
using Moq;

namespace Fdw.Services.Etl.Tests.Tracking;

/// <summary>
/// Tests for reading what a run did from its records and events (FDW-3381): the run's totals are its stages' added together, a stage's run time is its record's, a figure the
/// records do not hold is null and never zero, and a record that is not a stage of a flow is left out.
/// </summary>
public sealed class RunTrackingReaderTests
{
    private static readonly Guid Run = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Start = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IExecutionTracker> _tracker = new();

    private static Mock<IExecutionItem> Item(Guid id, string name, DateTimeOffset created, DateTimeOffset? started, DateTimeOffset? completed)
    {
        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.Id).Returns(id);
        item.SetupGet(i => i.Name).Returns(name);
        item.SetupGet(i => i.CreatedAt).Returns(created);
        item.SetupGet(i => i.StartedAt).Returns(started);
        item.SetupGet(i => i.CompletedAt).Returns(completed);
        item.SetupGet(i => i.State).Returns(ExecutionStateTypes.Running);
        return item;
    }

    private static IExecutionEvent Event(string type, Dictionary<string, object?> data)
    {
        var recorded = new Mock<IExecutionEvent>();
        recorded.SetupGet(e => e.EventType).Returns(type);
        recorded.SetupGet(e => e.Data).Returns(data);
        return recorded.Object;
    }

    private static Dictionary<string, object?> Component(string name, int seconds, long rowsIn, long rowsOut, long bytesOut, long rejected, bool failed = false) => new()
    {
        ["component"] = name,
        ["startedAt"] = Start.AddSeconds(seconds).ToString("o", System.Globalization.CultureInfo.InvariantCulture),
        ["endedAt"] = Start.AddSeconds(seconds + 1).ToString("o", System.Globalization.CultureInfo.InvariantCulture),
        ["succeeded"] = !failed,
        ["failed"] = failed,
        ["rowsIn"] = rowsIn,
        ["rowsOut"] = rowsOut,
        ["bytesOut"] = bytesOut,
        ["rejected"] = rejected,
    };

    private void Stages(IReadOnlyList<(Mock<IExecutionItem> Item, IReadOnlyList<IExecutionEvent> Events)> stages)
    {
        var run = Item(Run, "Nightly", Start, Start, Start.AddSeconds(10));
        _tracker.Setup(t => t.GetItem(Run, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Success(run.Object));
        var page = new Mock<IPagedResponse<IExecutionItem>>();
        page.SetupGet(p => p.Items).Returns([.. stages.Select(stage => stage.Item.Object)]);
        page.SetupGet(p => p.HasNext).Returns(false);
        _tracker
            .Setup(t => t.ListExecutions(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IExecutionItemType?>(), It.IsAny<IExecutionStateType?>(), It.IsAny<string?>(), Run, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPagedResponse<IExecutionItem>>.Success(page.Object));
        foreach (var (item, events) in stages)
        {
            _tracker.Setup(t => t.GetEvents(item.Object.Id, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IExecutionEvent>>.Success(events));
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheRunTotalsAreTheStagesAddedTogetherAndEachStageKeepsItsOwnRunTimeAndComponents()
    {
        var one = Item(Guid.NewGuid(), "Stage 1", Start, Start, Start.AddSeconds(3));
        var two = Item(Guid.NewGuid(), "Stage 2", Start.AddSeconds(3), Start.AddSeconds(3), Start.AddSeconds(8));
        var step = Item(Guid.NewGuid(), "load", Start.AddSeconds(1), Start, Start.AddSeconds(9));
        Stages([
            (two, [
                Event(ExecutionFlowReporter.ComponentEvent, Component("dst", 4, 8, 0, 0, 0)),
                Event(ExecutionFlowReporter.ComponentEvent, Component("check", 3, 10, 10, 400, 2)),
                Event(ExecutionFlowReporter.TotalsEvent, new() { ["rowsRead"] = 0L, ["rowsWritten"] = 8L, ["rowsRejected"] = 2L, ["bytesMoved"] = 320L }),
            ]),
            (one, [
                Event(ExecutionFlowReporter.ComponentEvent, Component("src", 0, 0, 10, 400, 0)),
                Event(ExecutionFlowReporter.TotalsEvent, new() { ["rowsRead"] = 10L, ["rowsWritten"] = 0L, ["rowsRejected"] = 0L, ["bytesMoved"] = 400L }),
            ]),
            (step, []),
        ]);

        var result = await RunTrackingReader.Read(_tracker.Object, Run, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var tracking = result.Value.ShouldNotBeNull();
        tracking.RowsRead.ShouldBe(10L);
        tracking.RowsWritten.ShouldBe(8L);
        tracking.RowsRejected.ShouldBe(2L);
        tracking.BytesMoved.ShouldBe(720L);
        tracking.RunTimeMs.ShouldBe(10_000L);
        tracking.Stages.Select(stage => stage.Name).ShouldBe(["Stage 1", "Stage 2"]);
        tracking.Stages.Select(stage => stage.Number).ShouldBe([1, 2]);
        tracking.Stages[1].RunTimeMs.ShouldBe(2_000L);
        tracking.Stages[1].Components.Select(component => component.Name).ShouldBe(["check", "dst"]);
        tracking.Stages[1].Components[0].RowsIn.ShouldBe(10L);
        tracking.Stages[1].Components[0].BytesOut.ShouldBe(400L);
        tracking.Stages[1].Components[0].Rejected.ShouldBe(2L);
        tracking.Stages[1].Components[0].RunTimeMs.ShouldBe(1_000L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStageRanFromItsFirstComponentStartingToItsLastEndingNotForAsLongAsItsRecordWasOpen()
    {
        // The records of a flow close together when the flow ends: this stage's record claims ten seconds, its two components ran from 0 to 2.
        var one = Item(Guid.NewGuid(), "No stage", Start, Start, Start.AddSeconds(10));
        Stages([(one, [Event(ExecutionFlowReporter.ComponentEvent, Component("a", 0, 0, 0, 0, 0)), Event(ExecutionFlowReporter.ComponentEvent, Component("b", 1, 0, 0, 0, 0))])]);

        var result = await RunTrackingReader.Read(_tracker.Object, Run, TestContext.Current.CancellationToken);

        var stage = result.Value.ShouldNotBeNull().Stages.ShouldHaveSingleItem();
        stage.StartedAt.ShouldBe(Start);
        stage.CompletedAt.ShouldBe(Start.AddSeconds(2));
        stage.RunTimeMs.ShouldBe(2_000L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStageWhoseComponentsHoldNoTimeHasNoStartEndOrRunTime()
    {
        var one = Item(Guid.NewGuid(), "Stage 1", Start, Start, Start.AddSeconds(3));
        Stages([(one, [Event(ExecutionFlowReporter.ComponentEvent, new() { ["component"] = "src", ["rowsOut"] = 10L })])]);

        var result = await RunTrackingReader.Read(_tracker.Object, Run, TestContext.Current.CancellationToken);

        var stage = result.Value.ShouldNotBeNull().Stages.ShouldHaveSingleItem();
        stage.StartedAt.ShouldBeNull();
        stage.CompletedAt.ShouldBeNull();
        stage.RunTimeMs.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFigureTheRecordsDoNotHoldIsNullAndNeverZero()
    {
        var one = Item(Guid.NewGuid(), "Stage 1", Start, Start, Start.AddSeconds(3));
        Stages([(one, [Event(ExecutionFlowReporter.ComponentEvent, new() { ["component"] = "src", ["rowsOut"] = 10L, ["succeeded"] = true })])]);

        var tracking = (await RunTrackingReader.Read(_tracker.Object, Run, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        tracking.RowsRead.ShouldBeNull();
        tracking.BytesMoved.ShouldBeNull();
        var component = tracking.Stages.ShouldHaveSingleItem().Components.ShouldHaveSingleItem();
        component.RowsOut.ShouldBe(10L);
        component.RowsIn.ShouldBeNull();
        component.BytesOut.ShouldBeNull();
        component.StartedAt.ShouldBeNull();
        component.RunTimeMs.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ValuesTheStoreGivesBackAsJsonElementsAreRead()
    {
        var one = Item(Guid.NewGuid(), "No stage", Start, Start, Start.AddSeconds(3));
        var data = new Dictionary<string, object?>
        {
            ["component"] = JsonDocument.Parse("\"src\"").RootElement.Clone(),
            ["rowsOut"] = JsonDocument.Parse("12").RootElement.Clone(),
            ["succeeded"] = JsonDocument.Parse("true").RootElement.Clone(),
            ["startedAt"] = JsonDocument.Parse("\"2026-11-04T12:00:00.0000000+00:00\"").RootElement.Clone(),
        };
        Stages([(one, [Event(ExecutionFlowReporter.ComponentEvent, data)])]);

        var tracking = (await RunTrackingReader.Read(_tracker.Object, Run, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        var stage = tracking.Stages.ShouldHaveSingleItem();
        stage.Number.ShouldBe(0);
        var component = stage.Components.ShouldHaveSingleItem();
        component.Name.ShouldBe("src");
        component.RowsOut.ShouldBe(12L);
        component.Succeeded.ShouldBe(true);
        component.StartedAt.ShouldBe(Start);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatIsNotFoundFailsTheRead()
    {
        _tracker.Setup(t => t.GetItem(Run, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Failure(new Fdw.Messages.GenericMessage("not found")));

        var result = await RunTrackingReader.Read(_tracker.Object, Run, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    private static Dictionary<string, object?> Committed(string destination, long rows, bool emptied) => new()
    {
        ["destination"] = destination,
        ["rowsCommitted"] = rows,
        ["targetEmptied"] = emptied,
        ["wholeSet"] = false,
        ["repeatsToSameResult"] = emptied,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADestinationsCommittedRowsAreTheGreatestTotalItReportedAndTheFlagIsKeptOnceSet()
    {
        var stage = Item(Guid.NewGuid(), "Stage 1", Start, Start, Start.AddSeconds(3));
        Stages([
            (stage, [
                Event(ExecutionFlowReporter.DestinationCommittedEvent, Committed("dst", 0, true)),
                Event(ExecutionFlowReporter.DestinationCommittedEvent, Committed("dst", 200, true)),
                Event(ExecutionFlowReporter.DestinationCommittedEvent, Committed("dst", 100, true)),
                Event(ExecutionFlowReporter.ComponentEvent, Component("dst", 0, 200, 0, 0, 0)),
                Event(ExecutionFlowReporter.ComponentEvent, Component("src", 0, 0, 200, 8000, 0)),
            ]),
        ]);

        var result = await RunTrackingReader.Read(_tracker.Object, Run, TestContext.Current.CancellationToken);

        var components = result.Value.ShouldNotBeNull().Stages.ShouldHaveSingleItem().Components;
        var destination = components.Single(component => component.Name == "dst");
        destination.RowsCommitted.ShouldBe(200L);
        destination.TargetEmptied.ShouldBe(true);
        destination.RepeatsToSameResult.ShouldBe(true);
        var source = components.Single(component => component.Name == "src");
        source.RowsCommitted.ShouldBeNull();
        source.TargetEmptied.ShouldBeNull();
        source.RepeatsToSameResult.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADestinationOfARunThatNeverFinishedItsStageStillAppearsWithItsCommittedRows()
    {
        var stage = Item(Guid.NewGuid(), "Stage 1", Start, Start, null);
        Stages([(stage, [Event(ExecutionFlowReporter.DestinationCommittedEvent, Committed("dst", 40, false))])]);

        var result = await RunTrackingReader.Read(_tracker.Object, Run, TestContext.Current.CancellationToken);

        var component = result.Value.ShouldNotBeNull().Stages.ShouldHaveSingleItem().Components.ShouldHaveSingleItem();
        component.Name.ShouldBe("dst");
        component.RowsCommitted.ShouldBe(40L);
        component.TargetEmptied.ShouldBe(false);
        component.RepeatsToSameResult.ShouldBe(false);
        component.Succeeded.ShouldBeNull();
        component.RowsOut.ShouldBeNull();
    }
}
