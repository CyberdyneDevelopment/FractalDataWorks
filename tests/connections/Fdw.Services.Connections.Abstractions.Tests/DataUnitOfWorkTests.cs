using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.UnitOfWork;

namespace Fdw.Services.Connections.Abstractions.Tests;

public sealed class DataUnitOfWorkTests
{
    private readonly Mock<IDataContainer> _container = new();

    private static UnitOfWorkStep Step(IDataContainer container)
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Test");
        return new UnitOfWorkStep(command.Object, container);
    }

    private static Mock<IDataConnectionTransaction> Transaction(params IGenericResult<int>[] results)
    {
        var transaction = new Mock<IDataConnectionTransaction>();
        var queue = new Queue<IGenericResult<int>>(results);
        transaction
            .Setup(t => t.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => queue.Dequeue());
        transaction.Setup(t => t.Commit(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        transaction.Setup(t => t.Rollback(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        return transaction;
    }

    private static Mock<ITransactionalDataConnection> Connection(IDataConnectionTransaction transaction)
    {
        var connection = new Mock<ITransactionalDataConnection>();
        connection
            .Setup(c => c.BeginTransaction(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnectionTransaction>.Success(transaction));
        return connection;
    }

    [Fact]
    public async Task AllStepsSucceedingCommitsOnceAndSumsTheRows()
    {
        var transaction = Transaction(GenericResult<int>.Success(3), GenericResult<int>.Success(4));
        var steps = new[] { Step(_container.Object), Step(_container.Object) };

        var result = await new DataUnitOfWork().Run(Connection(transaction.Object).Object, steps, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(7);
        transaction.Verify(t => t.Commit(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(t => t.Rollback(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AFailureOnTheThirdStepRollsBackAndNeverRunsOrCommitsTheRest()
    {
        var failure = GenericResult<int>.Failure(new Mock<IGenericMessage>().Object);
        var transaction = Transaction(GenericResult<int>.Success(1), GenericResult<int>.Success(1), failure, GenericResult<int>.Success(1));
        var steps = new[] { Step(_container.Object), Step(_container.Object), Step(_container.Object), Step(_container.Object) };

        var result = await new DataUnitOfWork().Run(Connection(transaction.Object).Object, steps, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.ShouldBeSameAs(failure);
        transaction.Verify(t => t.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        transaction.Verify(t => t.Rollback(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(t => t.Commit(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunEachAnswersWhatEachStepAffectedInStepOrder()
    {
        var transaction = Transaction(GenericResult<int>.Success(3), GenericResult<int>.Success(4));
        var steps = new[] { Step(_container.Object), Step(_container.Object) };

        var result = await new DataUnitOfWork().RunEach(Connection(transaction.Object).Object, steps, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([3, 4]);
        transaction.Verify(t => t.Commit(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunEachCarriesTheFailingStepsMessageAndRollsBack()
    {
        // A real message: a mocked one has no text, and GenericResult replaces it with its placeholder before RunEach sees it.
        var message = new GenericMessage("the second step failed");
        var transaction = Transaction(GenericResult<int>.Success(1), GenericResult<int>.Failure(message));
        var steps = new[] { Step(_container.Object), Step(_container.Object) };

        var result = await new DataUnitOfWork().RunEach(Connection(transaction.Object).Object, steps, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("the second step failed");
        transaction.Verify(t => t.Rollback(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(t => t.Commit(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static UnitOfWorkStep ReportingStep(IDataContainer container, System.Func<Fdw.Commands.Data.Abstractions.BulkWriteAnswer, IGenericMessage?>? check)
    {
        var command = new Mock<IReportingDataCommand>();
        command.Setup(c => c.CommandType).Returns("BulkUpdate");
        command.Setup(c => c.UnmatchedKeySample).Returns(5);
        return new UnitOfWorkStep(command.Object, container) { CheckAnswer = check };
    }

    private static Mock<IDataConnectionTransaction> Answering(Fdw.Commands.Data.Abstractions.BulkWriteAnswer answer)
    {
        var transaction = Transaction(GenericResult<int>.Success(1));
        transaction
            .Setup(t => t.Execute<Fdw.Commands.Data.Abstractions.BulkWriteAnswer>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Fdw.Commands.Data.Abstractions.BulkWriteAnswer>.Success(answer));
        return transaction;
    }

    [Fact]
    public async Task RunAnsweringRunsAReportingStepForItsAnswerAndAnyOtherForItsCount()
    {
        var answer = new Fdw.Commands.Data.Abstractions.BulkWriteAnswer(4, 2, []);
        var transaction = Answering(answer);
        var steps = new[] { Step(_container.Object), ReportingStep(_container.Object, check: null) };

        var result = await new DataUnitOfWork().RunAnswering(Connection(transaction.Object).Object, steps, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Affected.ShouldBe([1, 4]);
        result.Value.Answers[0].ShouldBeNull();
        result.Value.Answers[1].ShouldBe(answer);
        transaction.Verify(t => t.Commit(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AStepWhoseCheckRefusesItsAnswerRollsTheUnitBack()
    {
        var transaction = Answering(new Fdw.Commands.Data.Abstractions.BulkWriteAnswer(4, 2, []));
        var steps = new[] { ReportingStep(_container.Object, a => a.UnmatchedKeys > 0 ? new GenericMessage("two keys matched no row") : null) };

        var result = await new DataUnitOfWork().RunAnswering(Connection(transaction.Object).Object, steps, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("two keys matched no row");
        transaction.Verify(t => t.Rollback(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(t => t.Commit(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoStepsIsASuccessWithoutOpeningATransaction()
    {
        var connection = new Mock<ITransactionalDataConnection>();

        var result = await new DataUnitOfWork().Run(connection.Object, [], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0);
        connection.Verify(c => c.BeginTransaction(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ATransactionThatCannotStartIsAFailureOfTheWholeUnit()
    {
        var connection = new Mock<ITransactionalDataConnection>();
        connection
            .Setup(c => c.BeginTransaction(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnectionTransaction>.Failure(new Mock<IGenericMessage>().Object));

        var result = await new DataUnitOfWork().Run(connection.Object, [Step(_container.Object)], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
