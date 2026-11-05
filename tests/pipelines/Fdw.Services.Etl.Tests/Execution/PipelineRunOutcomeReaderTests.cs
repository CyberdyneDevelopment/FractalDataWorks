using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// The reader of a pipeline's run outcomes for the failure rule (FDW-3864): a success ends the count, a failure counts unless its recorded kind says transient, a failure
/// with no recorded kind counts as unclassified, and runs that are not finished are left out.
/// </summary>
public sealed class PipelineRunOutcomeReaderTests
{
    private static readonly DateTimeOffset Done = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private static Dictionary<string, object?> Row(Guid id, string status, DateTimeOffset? completed) => new()
    {
        ["Id"] = id,
        ["Status"] = status,
        ["StartedAt"] = Done.AddMinutes(-1),
        ["CompletedAt"] = completed,
    };

    private static Mock<IDataGateway> Gateway(params Dictionary<string, object?>[] rows)
    {
        var gateway = new Mock<IDataGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<QueryCommand<Dictionary<string, object?>>>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        return gateway;
    }

    private static IExecutionEvent Kind(bool transient, string category, string code, bool refused = false)
    {
        var data = new Dictionary<string, object?> { ["transient"] = transient, ["category"] = category, ["code"] = code, ["refused"] = refused };
        var item = new Mock<IExecutionEvent>();
        item.SetupGet(e => e.EventType).Returns(RunFailureClassification.Event);
        item.SetupGet(e => e.Data).Returns(data);
        return item.Object;
    }

    private static Mock<IExecutionTracker> Tracker(Guid id, params IExecutionEvent[] events)
    {
        var tracker = new Mock<IExecutionTracker>();
        tracker.Setup(t => t.GetEvents(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IExecutionEvent>>.Success(events));
        return tracker;
    }

    private static Task<IGenericResult<IReadOnlyList<PipelineRunOutcome>>> Read(Mock<IDataGateway> gateway, Mock<IExecutionTracker> tracker) =>
        PipelineRunOutcomeReader.Page(gateway.Object, tracker.Object, NullLogger.Instance, "Ops", "Orders", 0, 10, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARefusedRunDoesNotCountEvenWhenItsCategoryIsNotTransient()
    {
        var id = Guid.NewGuid();

        var read = await Read(Gateway(Row(id, "Failed", Done)), Tracker(id, Kind(false, "Conflict", "CONN-40001", refused: true)));

        var outcome = read.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        outcome.Succeeded.ShouldBeFalse();
        outcome.Counts.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASuccessfulRunIsASuccess()
    {
        var id = Guid.NewGuid();

        var read = await Read(Gateway(Row(id, "Succeeded", Done)), Tracker(id));

        var outcome = read.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        outcome.Succeeded.ShouldBeTrue();
        outcome.At.ShouldBe(Done);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFailureThatIsNotTransientCountsAndCarriesItsCategoryAndCode()
    {
        var id = Guid.NewGuid();

        var read = await Read(Gateway(Row(id, "Failed", Done)), Tracker(id, Kind(false, "Validation", "FDW-20000")));

        var outcome = read.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        outcome.Succeeded.ShouldBeFalse();
        outcome.Counts.ShouldBeTrue();
        outcome.Category.ShouldBe("Validation");
        outcome.Code.ShouldBe("FDW-20000");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ATransientFailureDoesNotCount()
    {
        var id = Guid.NewGuid();

        var read = await Read(Gateway(Row(id, "Failed", Done)), Tracker(id, Kind(true, "Dependency", "FDW-71000")));

        read.Value.ShouldNotBeNull().ShouldHaveSingleItem().Counts.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFailureWithNoRecordedKindCountsAsUnclassified()
    {
        var id = Guid.NewGuid();

        var read = await Read(Gateway(Row(id, "Failed", Done)), Tracker(id));

        var outcome = read.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        outcome.Counts.ShouldBeTrue();
        outcome.Category.ShouldBeNull();
        outcome.Code.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("Running")]
    [InlineData("Cancelled")]
    [InlineData("Interrupted")]
    [InlineData("Pending")]
    public async Task ARunThatIsNotFinishedOrWasCutOffIsLeftOut(string status)
    {
        var read = await Read(Gateway(Row(Guid.NewGuid(), status, null)), new Mock<IExecutionTracker>());

        read.Value.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFinishedRunWithNoCompletionTimeFailsTheReadRatherThanBeingPlacedByGuess()
    {
        var read = await Read(Gateway(Row(Guid.NewGuid(), "Failed", null)), new Mock<IExecutionTracker>());

        read.IsFailure.ShouldBeTrue();
    }
}
