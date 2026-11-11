using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;
using static Fdw.Services.Etl.Tests.Pipelines.Loads.LoadTestDoubles;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// A destination with an absent-key soft delete into a database (FDW-4956) never holds its rows: each batch is written with its keys staged in the same
/// unit of work, one sweep flags the target rows with no staged key once every row has arrived, and the stage rows are removed before the first batch
/// and when the destination ends, however it ends.
/// </summary>
public sealed class FlowLoadDestinationStageTests
{
    private const string StageKey = "00000000000000000000000000000001/dst";
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 10, 11, TimeSpan.Zero);

    private readonly Guid _db = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();
    private readonly List<string> _events = [];

    // Sends a row whose Name is "bad" through its error output and every other row on.
    private sealed class Splitter(string name) : IFlowComponent, IFlowErrorOutputs
    {
        public string Name { get; } = name;

        public IReadOnlyList<string> Inputs { get; } = ["input"];

        public IReadOnlyList<string> Outputs { get; } = ["output", "errors"];

        public IReadOnlyList<string> ErrorOutputs { get; } = ["errors"];

        public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
        {
            var input = ports.Input("input");
            while (await input.Read(cancellationToken) is { } batch)
            {
                var bad = batch.Rows.Where(row => Equals(row["Name"], "bad")).ToList();
                var good = batch.Rows.Where(row => !Equals(row["Name"], "bad")).ToList();
                if (good.Count > 0)
                    await ports.Output("output").Write(new FlowBatch(good), cancellationToken);
                if (bad.Count > 0)
                    await ports.Output("errors").Write(new FlowBatch(bad), cancellationToken);
            }

            return GenericResult.Success();
        }
    }

    private static IDataContainer Stage(Guid connectionId, params string[] fields)
    {
        var schema = new Mock<IContainerSchema>();
        schema.SetupGet(s => s.Fields).Returns([.. fields.Select(name => Mock.Of<IField>(field => field.Name == name))]);
        var stage = Mock.Get(Container("CustomersStage", connectionId));
        stage.SetupGet(c => c.Schema).Returns(schema.Object);
        return stage.Object;
    }

    private static LoadUnit Unit(string rule)
    {
        var row = new PipelineLoadStepConfiguration
        {
            Name = "dst",
            Kind = "Upsert",
            IsEnabled = true,
            SoftDeleteMode = "AbsentFromSource",
            DeletedFlagColumn = "IsDeleted",
            DeletedFlagValue = true,
            StageContainer = "CustomersStage",
            StageKeyColumn = "StageKey",
            SweepRejectedRowsRule = rule,
            Columns =
            [
                new PipelineLoadColumnConfiguration { Name = "Key:Code", Role = "Key", ColumnName = "Code", Ordinal = 0 },
                new PipelineLoadColumnConfiguration { Name = "Compare:Name", Role = "Compare", ColumnName = "Name", Ordinal = 0 },
                new PipelineLoadColumnConfiguration { Name = "Write:Code", Role = "Write", ColumnName = "Code", Ordinal = 0 },
                new PipelineLoadColumnConfiguration { Name = "Write:Name", Role = "Write", ColumnName = "Name", Ordinal = 1 },
            ],
        };
        return LoadUnit.ForContainer("graph destination 'dst'", LoadStepKinds.ByName("Upsert"), row, "Store", "dbo", "Customers").WithStageKey(StageKey);
    }

    private (PipelineLoadExecutor Executor, RecordingConnection Connection) Executor(Guid stageConnection, string? failVerb = null)
    {
        var connection = new RecordingConnection("db", _events, failVerb);
        var dataStores = DataStoreProviderOf(
            ("Store", "dbo", "Customers", Container("Customers", _db)),
            ("Store", "dbo", "CustomersStage", Stage(stageConnection, "Code", "StageKey")));
        var type = ConnectionType(["SoftDelete", "UnitOfWork", "KeyStage"]);
        var types = new TypeResolver(new Dictionary<Guid, IConnectionType> { [_db] = type, [_other] = type });
        return (new PipelineLoadExecutor(new LoadTargetResolver(null, dataStores.Object), ConnectionProviderOf((_db, connection.Connection)).Object, types, NullLogger.Instance), connection);
    }

    private static IDictionary<string, object?> Customer(string code, string name) => Fields(("Code", code), ("Name", name));

    private async Task<(FlowLoadDestination Destination, RecordingConnection Connection, IGenericResult Ran)> Load(
        string rule, IDictionary<string, object?>[] rows, bool split = false, Guid? stageConnection = null, string? failVerb = null)
    {
        var (executor, connection) = Executor(stageConnection ?? _db, failVerb);
        var destination = new FlowLoadDestination("dst", executor, "load", Unit(rule), null, new Dictionary<string, object?>(), Now, TimeSpan.Zero);
        var graph = split
            ? Graph([FromRows("src", rows), new Splitter("split"), destination], [Link("src", "split"), Link("split", "dst")], new FlowDroppedOutput("split", "errors"))
            : Graph([FromRows("src", rows), destination], [Link("src", "dst")]);
        var context = new FlowStepContext(new FlowVariables(), new FlowExecutor(), new FlowRunOptions { LinkCapacity = 2 }, null, null, null, null);
        var ran = await new FlowPipeline("run", [new FlowStageStep("flow", graph)]).Run(context, TestContext.Current.CancellationToken);
        return (destination, connection, ran);
    }

    private static bool IsStageClear(IDataCommand command) => command is DeleteCommand { Filter: not null, SoftDelete: null };

    private static IEnumerable<DeleteCommand> Sweeps(RecordingConnection connection) =>
        connection.Commands.OfType<DeleteCommand>().Where(delete => delete.SoftDelete?.AbsentFromStage is not null);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task EachBatchIsWrittenWithItsKeysStagedAndOneSweepFollowsEveryRow()
    {
        var (destination, connection, ran) = await Load("Refuse", [Customer("A", "Ann"), Customer("B", "Bo"), Customer("C", "Cy")]);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        var commands = connection.Commands;
        IsStageClear(commands[0]).ShouldBeTrue();
        IsStageClear(commands[^1]).ShouldBeTrue();
        commands.Count(command => command is BulkUpsertCommand<IDictionary<string, object?>>).ShouldBe(2);
        commands.Count(command => command is DeleteCommand { SoftDelete.AbsentFromSource: not null }).ShouldBe(0);

        var staged = commands.OfType<InsertCommand<IDictionary<string, object?>>>().SelectMany(insert => insert.Data).ToList();
        staged.Select(row => row["Code"]).ShouldBe(["A", "B", "C"]);
        staged.ShouldAllBe(row => Equals(row["StageKey"], StageKey) && row.Count == 2);

        var sweep = Sweeps(connection).ShouldHaveSingleItem();
        ReferenceEquals(sweep.SoftDelete!.Mode, SoftDeleteModes.AbsentFromStage).ShouldBeTrue();
        sweep.SoftDelete.AbsentFromStage!.ExpectedRows.ShouldBe(3L);
        sweep.SoftDelete.AbsentFromStage.StageKey.ShouldBe(StageKey);
        sweep.SoftDelete.AbsentFromStage.KeyColumns.ShouldBe(["Code"]);
        commands.IndexOf(sweep).ShouldBe(commands.Count - 2);

        // Each batch's upsert and its staged keys commit together; the staged keys are not rows loaded (the recording connection answers 1 for each command).
        _events.Count(e => e == "commit:db").ShouldBe(5);
        destination.RowsLoaded.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnEmptySourceNeverSweeps()
    {
        var (_, connection, ran) = await Load("Refuse", []);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Sweeps(connection).ShouldBeEmpty();
        connection.Commands.ShouldAllBe(command => IsStageClear(command));
        connection.Commands.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailedBatchMeansNoSweepAndTheStageIsStillCleared()
    {
        var (_, connection, ran) = await Load("Refuse", [Customer("A", "Ann"), Customer("B", "Bo"), Customer("C", "Cy")], failVerb: "BulkUpsert");

        ran.IsFailure.ShouldBeTrue();
        Sweeps(connection).ShouldBeEmpty();
        IsStageClear(connection.Commands[^1]).ShouldBeTrue();
        _events.ShouldContain("rollback:db");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RowsRejectedUpstreamUnderRefuseFailTheDestinationAndFlagNothing()
    {
        var (_, connection, ran) = await Load("Refuse", [Customer("A", "Ann"), Customer("B", "bad"), Customer("C", "Cy")], split: true);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("rejected upstream");
        Sweeps(connection).ShouldBeEmpty();
        IsStageClear(connection.Commands[^1]).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RowsRejectedUpstreamUnderProceedStillSweep()
    {
        var (_, connection, ran) = await Load("Proceed", [Customer("A", "Ann"), Customer("B", "bad"), Customer("C", "Cy")], split: true);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Sweeps(connection).ShouldHaveSingleItem().SoftDelete!.AbsentFromStage!.ExpectedRows.ShouldBe(2L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStageOnAnotherConnectionIsRefusedBeforeAnyRowIsWritten()
    {
        var (_, connection, ran) = await Load("Refuse", [Customer("A", "Ann")], stageConnection: _other);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("is not on the connection");
        connection.Commands.ShouldBeEmpty();
    }
}
