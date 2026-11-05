using System;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Tracking;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="CancelledRunMessage"/> (FDW-3784): a cancelled run's message says why it stopped, the rows it moved and where it was stopped, and leaves out
/// a figure its records do not hold.
/// </summary>
public sealed class CancelledRunMessageTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private static ComponentTracking Component(string name, bool succeeded) =>
        new(name, Start, Start.AddSeconds(1), 1000, succeeded, false, null, null, null, null, null, null, null);

    private static StageTracking Stage(string name, string state, params ComponentTracking[] components) =>
        new(Guid.NewGuid(), name, null, state, Start, null, null, null, null, null, null, components);

    private static RunTracking Run(long? read, long? written, long? rejected, params StageTracking[] stages) =>
        new(Guid.NewGuid(), "run", "Running", Start, null, null, read, written, rejected, null, stages);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheMessageSaysWhyTheRowsItMovedAndWhereItStopped()
    {
        var tracking = Run(
            120, 100, 2,
            Stage("Stage 1", "Completed", Component("Read", true)),
            Stage("Stage 2", "Cancelled", Component("Map", true), Component("Load", false), Component("Audit", false)));

        var message = CancelledRunMessage.Of("Execution was cancelled", tracking);

        message.ShouldBe("Execution was cancelled. Rows read 120, written 100, rejected 2. Stopped in Stage 2 (Load, Audit)");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AFigureTheRecordsDoNotHoldIsLeftOutAndNeverWrittenAsZero()
    {
        var tracking = Run(null, 7, null);

        var message = CancelledRunMessage.Of("The run timed out", tracking);

        message.ShouldBe("The run timed out. Rows written 7");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARunWithNoFiguresAndNoUnfinishedStageSaysOnlyWhy()
    {
        var tracking = Run(null, null, null, Stage("Stage 1", "Completed", Component("Read", true)));

        var message = CancelledRunMessage.Of("Execution was cancelled", tracking);

        message.ShouldBe("Execution was cancelled");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStageWhoseComponentsAllSucceededButWasNotClosedIsNamedAlone()
    {
        var tracking = Run(null, null, null, Stage("Stage 1", "Running", Component("Read", true)));

        var message = CancelledRunMessage.Of("Execution was cancelled", tracking);

        message.ShouldBe("Execution was cancelled. Stopped in Stage 1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AReasonThatIsBlankIsRefused()
    {
        Should.Throw<ArgumentException>(() => CancelledRunMessage.Of(" ", Run(null, null, null)));
    }
}
