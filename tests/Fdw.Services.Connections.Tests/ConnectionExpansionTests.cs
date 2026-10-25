using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// A kind with no translator for a command that declares an expansion runs the simpler commands it expands into, in
/// order, and answers the rows they affected together; any other command with no translator still fails.
/// </summary>
public sealed class ConnectionExpansionTests
{
    private readonly Mock<IDataContainer> _container = new();

    private static IDataCommand Single(string type)
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns(type);
        return command.Object;
    }

    private static Mock<IExpandableDataCommand> Expandable(string type, params IDataCommand[] steps)
        => ExpandableOf(type, steps.Select(step => new ExpandedCommand(step)).ToArray());

    private static Mock<IExpandableDataCommand> ExpandableOf(string type, params ExpandedCommand[] steps)
    {
        var command = new Mock<IExpandableDataCommand>();
        command.Setup(c => c.CommandType).Returns(type);
        command.Setup(c => c.Expand(It.IsAny<IStorageContainer>()))
            .Returns(GenericResult<IReadOnlyList<ExpandedCommand>>.Success(steps.ToList()));
        return command;
    }

    private static ExpandingConnection Connection(string[] handled, string? failing = null) => new(handled, failing);

    [Fact]
    public async Task CommandExpansionRunsEachStepThroughTheExecuteItIsGivenAndLeavesUncountedStepsOutOfTheAnswer()
    {
        var ran = new List<string>();
        var command = ExpandableOf("Upsert", new ExpandedCommand(Single("Delete"), CountsAsAffected: false), new ExpandedCommand(Single("Insert")));

        var result = await CommandExpansion.Run<int>(
            command.Object,
            _container.Object,
            (step, _) =>
            {
                ran.Add(step.CommandType);
                return Task.FromResult<IGenericResult<int>>(GenericResult<int>.Success(1));
            },
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(1);
        ran.ShouldBe(["Delete", "Insert"]);
    }

    [Fact]
    public async Task AnExpandableCommandWithNoTranslatorRunsEachStepAndAnswersTheirRowsTogether()
    {
        var connection = Connection(["Upsert"]);
        var command = Expandable("BulkUpsert", Single("Upsert"), Single("Upsert"), Single("Upsert"));

        var result = await connection.Execute<int>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(3);
        connection.Ran.ShouldBe(["Upsert", "Upsert", "Upsert"]);
    }

    [Fact]
    public async Task TheFirstFailureEndsTheExpansionAndIsReturned()
    {
        var connection = Connection(["Delete", "Insert"], failing: "Insert");
        var command = Expandable("Upsert", Single("Delete"), Single("Insert"), Single("Delete"));

        var result = await connection.Execute<int>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("boom");
        connection.Ran.ShouldBe(["Delete", "Insert"]);
    }

    [Fact]
    public async Task AStepThatHasNoTranslatorEitherExpandsAgain()
    {
        var connection = Connection(["Insert"]);
        var inner = Expandable("Upsert", Single("Insert"), Single("Insert"));
        var command = Expandable("BulkUpsert", inner.Object, Single("Insert"));

        var result = await connection.Execute<int>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.Value.ShouldBe(3);
        connection.Ran.ShouldBe(["Insert", "Insert", "Insert"]);
    }

    [Fact]
    public async Task ACommandThatIsNotExpandableAndHasNoTranslatorStillFails()
    {
        var connection = Connection(["Insert"]);

        var result = await connection.Execute<int>(Single("Find"), _container.Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Find");
        connection.Ran.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnExpansionTheCommandRefusesIsReturnedAndNothingRuns()
    {
        var connection = Connection(["Update"]);
        var command = new Mock<IExpandableDataCommand>();
        command.Setup(c => c.CommandType).Returns("BulkUpdate");
        command.Setup(c => c.Expand(It.IsAny<IStorageContainer>()))
            .Returns(GenericResult<IReadOnlyList<ExpandedCommand>>.Failure(new GenericMessage("a row has no key")));

        var result = await connection.Execute<int>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("no key");
        connection.Ran.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnExpansionAnsweredForAResultThatIsNotARowCountFailsWithoutRunningAnything()
    {
        var connection = Connection(["Upsert"]);
        var command = Expandable("BulkUpsert", Single("Upsert"));

        var result = await connection.Execute<string>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        connection.Ran.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnExpandableCommandWithATranslatorIsRunAsItIsAndNeverExpanded()
    {
        var connection = Connection(["BulkUpsert"]);
        var command = Expandable("BulkUpsert", Single("Upsert"));

        var result = await connection.Execute<int>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.Value.ShouldBe(1);
        connection.Ran.ShouldBe(["BulkUpsert"]);
        command.Verify(c => c.Expand(It.IsAny<IStorageContainer>()), Times.Never);
    }

    [Fact]
    public async Task AStepThatDoesNotCountAddsNothingToTheRowsAnswered()
    {
        var connection = Connection(["Delete", "Insert"]);
        var command = ExpandableOf("Upsert",
            new ExpandedCommand(Single("Delete"), CountsAsAffected: false),
            new ExpandedCommand(Single("Insert")),
            new ExpandedCommand(Single("Delete"), CountsAsAffected: false),
            new ExpandedCommand(Single("Insert")));

        var result = await connection.Execute<int>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.Value.ShouldBe(2);
        connection.Ran.ShouldBe(["Delete", "Insert", "Delete", "Insert"]);
    }

    [Fact]
    public async Task AnExpansionOfNoStepsAnswersNoRows()
    {
        var connection = Connection(["Upsert"]);

        var result = await connection.Execute<int>(Expandable("BulkUpsert").Object, _container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0);
    }

    private static Mock<IAssembledExpandableDataCommand<string>> Assembled(string type, List<IReadOnlyList<ExpansionAnswer>> handed, params ExpandedCommand[] steps)
    {
        var command = new Mock<IAssembledExpandableDataCommand<string>>();
        command.Setup(c => c.CommandType).Returns(type);
        command.Setup(c => c.Expand(It.IsAny<IStorageContainer>()))
            .Returns(GenericResult<IReadOnlyList<ExpandedCommand>>.Success(steps.ToList()));
        command.Setup(c => c.Assemble(It.IsAny<IReadOnlyList<ExpansionAnswer>>()))
            .Returns((IReadOnlyList<ExpansionAnswer> answers) =>
            {
                handed.Add(answers);
                return GenericResult<string>.Success("assembled");
            });
        return command;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> OneRow(string key)
        => [new Dictionary<string, object?> { ["Id"] = key }];

    [Fact]
    public async Task AnAssembledExpansionRunsEachStepByWhatItAnswersAndHandsTheAnswersToTheCommand()
    {
        var handed = new List<IReadOnlyList<ExpansionAnswer>>();
        var ran = new List<string>();
        var command = Assembled(
            "Save",
            handed,
            new ExpandedCommand(Single("Query"), CountsAsAffected: false, AnswersRows: true),
            new ExpandedCommand(Single("Close")),
            new ExpandedCommand(Single("Insert"), AnswersRows: true));

        var result = await CommandExpansion.RunAssembled(
            command.Object,
            _container.Object,
            (step, _) =>
            {
                ran.Add(step.CommandType);
                return Task.FromResult<IGenericResult<int>>(GenericResult<int>.Success(4));
            },
            (step, _) =>
            {
                ran.Add(step.CommandType);
                return Task.FromResult<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>>(
                    GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(OneRow(step.CommandType)));
            },
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("assembled");
        ran.ShouldBe(["Query", "Close", "Insert"]);
        var answers = handed.ShouldHaveSingleItem();
        answers.Count.ShouldBe(3);
        answers[0].Rows.ShouldNotBeNull().Single()["Id"].ShouldBe("Query");
        answers[1].Affected.ShouldBe(4);
        answers[1].Rows.ShouldBeNull();
        answers[2].Rows.ShouldNotBeNull().Single()["Id"].ShouldBe("Insert");
    }

    [Fact]
    public async Task AnAssembledExpansionEndsAtTheFirstFailureAndNeverAssembles()
    {
        var handed = new List<IReadOnlyList<ExpansionAnswer>>();
        var ran = new List<string>();
        var command = Assembled("Save", handed, new ExpandedCommand(Single("Close")), new ExpandedCommand(Single("Insert")), new ExpandedCommand(Single("Close")));

        var result = await CommandExpansion.RunAssembled(
            command.Object,
            _container.Object,
            (step, _) =>
            {
                ran.Add(step.CommandType);
                return Task.FromResult<IGenericResult<int>>(string.Equals(step.CommandType, "Insert", System.StringComparison.Ordinal)
                    ? GenericResult<int>.Failure(new GenericMessage("boom"))
                    : GenericResult<int>.Success(1));
            },
            (_, _) => Task.FromResult<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>>(
                GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success([])),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("boom");
        ran.ShouldBe(["Close", "Insert"]);
        handed.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnExpansionTheAssembledCommandRefusesIsReturnedAndNothingRuns()
    {
        var command = new Mock<IAssembledExpandableDataCommand<string>>();
        command.Setup(c => c.CommandType).Returns("Save");
        command.Setup(c => c.Expand(It.IsAny<IStorageContainer>()))
            .Returns(GenericResult<IReadOnlyList<ExpandedCommand>>.Failure(new GenericMessage("no key")));
        var ran = 0;

        var result = await CommandExpansion.RunAssembled(
            command.Object,
            _container.Object,
            (_, _) =>
            {
                ran++;
                return Task.FromResult<IGenericResult<int>>(GenericResult<int>.Success(1));
            },
            (_, _) =>
            {
                ran++;
                return Task.FromResult<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>>(
                    GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success([]));
            },
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("no key");
        ran.ShouldBe(0);
        command.Verify(c => c.Assemble(It.IsAny<IReadOnlyList<ExpansionAnswer>>()), Times.Never);
    }

    [Fact]
    public async Task ARowCountExpansionRefusesAStepThatAnswersRows()
    {
        var ran = 0;
        var command = ExpandableOf("Save", new ExpandedCommand(Single("Query"), AnswersRows: true));

        var result = await CommandExpansion.Run<int>(
            command.Object,
            _container.Object,
            (_, _) =>
            {
                ran++;
                return Task.FromResult<IGenericResult<int>>(GenericResult<int>.Success(1));
            },
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Save");
        ran.ShouldBe(0);
    }

    private static Mock<IAssembledExpandableDataCommand<string>> SaveLike(List<IReadOnlyList<ExpansionAnswer>> handed)
        => Assembled(
            "Save",
            handed,
            new ExpandedCommand(Single("Query"), CountsAsAffected: false, AnswersRows: true),
            new ExpandedCommand(Single("Close")),
            new ExpandedCommand(Single("Insert"), AnswersRows: true));

    [Fact]
    public async Task AnAssembledCommandOnAConnectionWithTransactionsRunsItsStepsInOneAndCommits()
    {
        var handed = new List<IReadOnlyList<ExpansionAnswer>>();
        var connection = new TransactionalExpandingConnection(failing: null);
        var command = SaveLike(handed);

        var result = await connection.Execute<string>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("assembled");
        connection.Scope.Ran.ShouldBe(["Query", "Close", "Insert"]);
        connection.Scope.Committed.ShouldBeTrue();
        connection.Scope.RolledBack.ShouldBeFalse();
        var answers = handed.ShouldHaveSingleItem();
        answers[0].Rows.ShouldNotBeNull().Single()["Id"].ShouldBe("Query");
        answers[1].Affected.ShouldBe(1);
        answers[2].Rows.ShouldNotBeNull().Single()["Id"].ShouldBe("Insert");
    }

    [Fact]
    public async Task AnAssembledCommandOnAConnectionAlreadyInAUnitOfWorkRunsItsStepsOnThatConnectionAndOpensNoTransaction()
    {
        var handed = new List<IReadOnlyList<ExpansionAnswer>>();
        var connection = new TransactionalExpandingConnection(failing: null, inUnitOfWork: true);
        var command = Assembled("Retire", handed, new ExpandedCommand(Single("Close")), new ExpandedCommand(Single("Close")));

        var result = await connection.Execute<string>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.Value.ShouldBe("assembled");
        connection.Ran.ShouldBe(["Close", "Close"]);
        connection.Scope.Ran.ShouldBeEmpty();
        connection.Scope.Committed.ShouldBeFalse();
        connection.Scope.RolledBack.ShouldBeFalse();
    }

    [Fact]
    public async Task AFailingStepRollsTheTransactionBackAndTheCommandNeverAssembles()
    {
        var handed = new List<IReadOnlyList<ExpansionAnswer>>();
        var connection = new TransactionalExpandingConnection(failing: "Insert");
        var command = SaveLike(handed);

        var result = await connection.Execute<string>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("boom");
        connection.Scope.Ran.ShouldBe(["Query", "Close", "Insert"]);
        connection.Scope.RolledBack.ShouldBeTrue();
        connection.Scope.Committed.ShouldBeFalse();
        handed.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAssembledCommandOnAConnectionWithoutTransactionsRunsItsStepsInOrder()
    {
        var handed = new List<IReadOnlyList<ExpansionAnswer>>();
        var connection = Connection(["Close"]);
        var command = Assembled("Retire", handed, new ExpandedCommand(Single("Close")), new ExpandedCommand(Single("Close")));

        var result = await connection.Execute<string>(command.Object, _container.Object, TestContext.Current.CancellationToken);

        result.Value.ShouldBe("assembled");
        connection.Ran.ShouldBe(["Close", "Close"]);
        handed.ShouldHaveSingleItem().Select(answer => answer.Affected).ShouldBe([1, 1]);
    }

    [Fact]
    public async Task AskingForRowsRunsTheTranslatedCommandForRows()
    {
        var connection = Connection(["Query"]);

        var result = await connection.Execute<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(
            Single("Query"), _container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Single()["Id"].ShouldBe("Query");
    }

    /// <summary>A connection that has a translator for the command types it is given and answers one row for each command it runs.</summary>
    private sealed class ExpandingConnection
        : ConnectionBase<ConnectionBaseTests.TestNativeCommand, ConnectionBaseTests.TestConnectionConfiguration, ExpandingConnection>
    {
        private readonly string[] _handled;
        private readonly string? _failing;

        public ExpandingConnection(string[] handled, string? failing)
            : base(NullLogger<ExpandingConnection>.Instance, new ConnectionBaseTests.TestConnectionConfiguration { Name = "expanding" })
        {
            _handled = handled;
            _failing = failing;
        }

        /// <summary>Gets the native commands that ran, in order.</summary>
        public List<string> Ran { get; } = [];

        protected override IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand> GetTranslator(string commandType)
        {
            var translator = new Mock<IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand>>();
            translator.Setup(t => t.Name).Returns(_handled.Contains(commandType) ? commandType : "NotFound");
            translator
                .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<ConnectionBaseTests.TestNativeCommand>.Success(new ConnectionBaseTests.TestNativeCommand { Sql = commandType }));
            return translator.Object;
        }

        protected override Task<IGenericResult<T>> Execute<T>(
            ConnectionBaseTests.TestNativeCommand command, IStorageContainer container, CancellationToken cancellationToken)
        {
            Ran.Add(command.Sql);
            return Task.FromResult<IGenericResult<T>>(string.Equals(command.Sql, _failing, System.StringComparison.Ordinal)
                ? GenericResult<T>.Failure(new GenericMessage("boom"))
                : GenericResult<T>.Success(typeof(T) == typeof(int) ? (T)(object)1 : default!));
        }

        protected override Task<IGenericResult> Execute(
            ConnectionBaseTests.TestNativeCommand command, IStorageContainer container, CancellationToken cancellationToken)
            => Task.FromResult<IGenericResult>(GenericResult.Success());

        protected override Task<IGenericResult<IEnumerable<object>>> ExecuteRowsByType(
            ConnectionBaseTests.TestNativeCommand command, IStorageContainer container, System.Type elementType, CancellationToken cancellationToken)
        {
            Ran.Add(command.Sql);
            return Task.FromResult<IGenericResult<IEnumerable<object>>>(
                GenericResult<IEnumerable<object>>.Success([new Dictionary<string, object?> { ["Id"] = command.Sql }]));
        }
    }

    /// <summary>A connection that can open a transaction; every command it is given is run inside the transaction scope, never natively.</summary>
    private sealed class TransactionalExpandingConnection
        : ConnectionBase<ConnectionBaseTests.TestNativeCommand, ConnectionBaseTests.TestConnectionConfiguration, TransactionalExpandingConnection>,
          ITransactionalDataConnection
    {
        private readonly bool _inUnitOfWork;

        public TransactionalExpandingConnection(string? failing, bool inUnitOfWork = false)
            : base(NullLogger<TransactionalExpandingConnection>.Instance, new ConnectionBaseTests.TestConnectionConfiguration { Name = "transactional" })
        {
            Scope = new RecordingScope(failing);
            _inUnitOfWork = inUnitOfWork;
        }

        /// <summary>Gets the native commands that ran on this connection itself, in order.</summary>
        public List<string> Ran { get; } = [];

        protected override bool InUnitOfWork => _inUnitOfWork;

        /// <summary>Gets the scope every transaction hands out.</summary>
        public RecordingScope Scope { get; }

        public Task<IGenericResult<IDataConnectionTransaction>> BeginTransaction(CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<IDataConnectionTransaction>>(GenericResult<IDataConnectionTransaction>.Success(Scope));

        protected override IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand> GetTranslator(string commandType)
        {
            var translator = new Mock<IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand>>();
            // Only the step type has a translator, so the assembled command itself has none and expands.
            translator.Setup(t => t.Name).Returns(_inUnitOfWork && string.Equals(commandType, "Close", System.StringComparison.Ordinal) ? commandType : "NotFound");
            translator
                .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<ConnectionBaseTests.TestNativeCommand>.Success(new ConnectionBaseTests.TestNativeCommand { Sql = commandType }));
            return translator.Object;
        }

        protected override Task<IGenericResult<T>> Execute<T>(
            ConnectionBaseTests.TestNativeCommand command, IStorageContainer container, CancellationToken cancellationToken)
        {
            Ran.Add(command.Sql);
            return Task.FromResult<IGenericResult<T>>(GenericResult<T>.Success(typeof(T) == typeof(int) ? (T)(object)1 : default!));
        }

        protected override Task<IGenericResult> Execute(
            ConnectionBaseTests.TestNativeCommand command, IStorageContainer container, CancellationToken cancellationToken)
            => Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    /// <summary>A transaction scope that records what ran in it and how it ended, and fails the one command type it is told to.</summary>
    private sealed class RecordingScope : IDataConnectionTransaction
    {
        private readonly string? _failing;

        public RecordingScope(string? failing)
        {
            _failing = failing;
        }

        public List<string> Ran { get; } = [];

        public bool Committed { get; private set; }

        public bool RolledBack { get; private set; }

        public bool IsActive => !Committed && !RolledBack;

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
        {
            Ran.Add(command.CommandType);
            if (string.Equals(command.CommandType, _failing, System.StringComparison.Ordinal))
                return Task.FromResult<IGenericResult<T>>(GenericResult<T>.Failure(new GenericMessage("boom")));

            object answer = typeof(T) == typeof(int) ? 1 : OneRow(command.CommandType);
            return Task.FromResult<IGenericResult<T>>(GenericResult<T>.Success((T)answer));
        }

        public Task<IGenericResult> Execute(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult>(GenericResult.Success());

        public Task<IGenericResult> Commit(CancellationToken cancellationToken = default)
        {
            Committed = true;
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        public Task<IGenericResult> Rollback(CancellationToken cancellationToken = default)
        {
            RolledBack = true;
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
