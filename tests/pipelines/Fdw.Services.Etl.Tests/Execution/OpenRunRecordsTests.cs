using Fdw.Messages;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for closing the records of a run the host stopped or lost (FDW-3787): every record still open under the run ends in the stated
/// state, children before the records above them, a record that already ended is left as it was, and one that cannot be closed does not
/// stop the others.
/// </summary>
public sealed class OpenRunRecordsTests
{
    private static readonly Guid Run = Guid.NewGuid();
    private static readonly Guid StageOne = Guid.NewGuid();
    private static readonly Guid ActionOne = Guid.NewGuid();
    private static readonly Guid StageTwo = Guid.NewGuid();
    private static readonly Guid NoStage = Guid.NewGuid();

    private readonly Mock<IExecutionTracker> _tracker = new();
    private readonly List<Guid> _closed = [];

    private static IExecutionItem Item(Guid id, IExecutionStateType state)
    {
        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.Id).Returns(id);
        item.SetupGet(i => i.State).Returns(state);
        return item.Object;
    }

    private void Tree()
    {
        _tracker.Setup(t => t.GetItem(Run, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Success(Item(Run, ExecutionStateTypes.Running)));
        _tracker.Setup(t => t.GetChildren(Run, It.IsAny<CancellationToken>())).ReturnsAsync(Children(Item(StageOne, ExecutionStateTypes.Running), Item(StageTwo, ExecutionStateTypes.Completed), Item(NoStage, ExecutionStateTypes.Failed)));
        _tracker.Setup(t => t.GetChildren(StageOne, It.IsAny<CancellationToken>())).ReturnsAsync(Children(Item(ActionOne, ExecutionStateTypes.Running)));
        _tracker.Setup(t => t.GetChildren(ActionOne, It.IsAny<CancellationToken>())).ReturnsAsync(Children());
        _tracker.Setup(t => t.GetChildren(StageTwo, It.IsAny<CancellationToken>())).ReturnsAsync(Children());
        _tracker.Setup(t => t.GetChildren(NoStage, It.IsAny<CancellationToken>())).ReturnsAsync(Children());
        _tracker
            .Setup(t => t.Close(It.IsAny<Guid>(), It.IsAny<IExecutionStateType>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, IExecutionStateType _, string? _, string? _, CancellationToken _) =>
            {
                _closed.Add(id);
                return GenericResult.Success();
            });
    }

    private static IGenericResult<IReadOnlyList<IExecutionItem>> Children(params IExecutionItem[] items) => GenericResult<IReadOnlyList<IExecutionItem>>.Success(items);

    private OpenRunRecords Sut() => new(_tracker.Object, NullLogger.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EveryOpenRecordUnderTheRunAndTheRunItselfEndInTheStatedStateChildrenFirst()
    {
        Tree();

        var result = await Sut().Close(Run, ExecutionStateTypes.Interrupted, "ServerStopped", "the host stopped", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _closed.ShouldBe([ActionOne, StageOne, Run]);
        _tracker.Verify(
            t => t.Close(It.IsAny<Guid>(), ExecutionStateTypes.Interrupted, "ServerStopped", "the host stopped", It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARecordThatAlreadyEndedKeepsWhatItSaid()
    {
        Tree();

        _ = await Sut().Close(Run, ExecutionStateTypes.Interrupted, "ServerStopped", "the host stopped", TestContext.Current.CancellationToken);

        _closed.ShouldNotContain(StageTwo);
        _closed.ShouldNotContain(NoStage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARecordThatCannotBeClosedIsReportedAndTheOthersAreStillClosed()
    {
        Tree();
        _tracker
            .Setup(t => t.Close(StageOne, It.IsAny<IExecutionStateType>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("the store is down")));

        var result = await Sut().Close(Run, ExecutionStateTypes.Interrupted, "ServerStopped", "the host stopped", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        _closed.ShouldBe([ActionOne, Run]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ARunWhoseChildrenCannotBeReadIsReportedAndItsOwnRecordIsStillClosed()
    {
        Tree();
        _tracker.Setup(t => t.GetChildren(Run, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IExecutionItem>>.Failure(new GenericMessage("the store is down")));

        var result = await Sut().Close(Run, ExecutionStateTypes.Interrupted, "ServerStopped", "the host stopped", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        _closed.ShouldBe([Run]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ARunThatAlreadyEndedIsNotClosedAgain()
    {
        Tree();
        _tracker.Setup(t => t.GetItem(Run, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Success(Item(Run, ExecutionStateTypes.Completed)));

        var result = await Sut().Close(Run, ExecutionStateTypes.Interrupted, "ServerStopped", "the host stopped", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _closed.ShouldNotContain(Run);
    }
}
