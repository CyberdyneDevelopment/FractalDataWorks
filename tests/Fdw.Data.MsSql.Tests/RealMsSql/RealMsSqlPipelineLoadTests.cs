using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions.DataDestination.Kinds;
using Fdw.Services.Pipelines.Abstractions.DataSource.Kinds;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Data.MsSql.Tests.RealMsSql.RealMsSqlLoadSupport;

namespace Fdw.Data.MsSql.Tests.RealMsSql;

/// <summary>
/// The pipeline engine against a real SQL Server (FDW-1109), in a schema the run creates and drops: the load modes from one
/// source, the transforms, run parameters, the watermark, and load steps (an insert and a stored procedure) in one
/// transaction. Nothing outside the run's own schema is touched.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class RealMsSqlPipelineLoadTests(RealMsSqlFixture fixture) : IClassFixture<RealMsSqlFixture>
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private readonly Guid _connectionId = Guid.NewGuid();

    // The shape of every table the tests make: a key, a group, a name, points and a moment. The sink may also number its
    // rows (Seq) so the order the engine loaded them in can be read back.
    private static readonly Col[] Columns = [Int("Id"), Text("Grp"), Text("Name"), Int("Points"), Moment("Modified")];

    private sealed record Tables(string Source, string Sink, string Watermark, string Log, string Strict);

    private async Task<Tables> Create(bool sinkNumbersRows = false, string? sinkCheck = null)
    {
        var tables = new Tables(fixture.Unique("Source"), fixture.Unique("Sink"), fixture.Unique("Watermark"), fixture.Unique("LoadLog"), fixture.Unique("Strict"));
        var schema = fixture.Schema;
        var seq = sinkNumbersRows ? "Seq int IDENTITY(1,1) NOT NULL, " : string.Empty;
        var check = sinkCheck is null ? string.Empty : $", CONSTRAINT CK_{tables.Sink} CHECK ({sinkCheck})";
        await fixture.Exec(
            $"CREATE TABLE [{schema}].[{tables.Source}] (Id int NOT NULL PRIMARY KEY, Grp nvarchar(20) NULL, Name nvarchar(50) NULL, Points int NULL, Modified datetimeoffset(7) NULL); "
            + $"CREATE TABLE [{schema}].[{tables.Sink}] ({seq}Id int NOT NULL PRIMARY KEY, Grp nvarchar(20) NULL, Name nvarchar(50) NULL, Points int NULL, Modified datetimeoffset(7) NULL{check}); "
            + $"CREATE TABLE [{schema}].[{tables.Watermark}] (Id uniqueidentifier NOT NULL PRIMARY KEY, PipelineId uniqueidentifier NOT NULL, TenantId uniqueidentifier NOT NULL, LastSuccessfulRun datetimeoffset(7) NOT NULL, ExecutionId uniqueidentifier NOT NULL); "
            + $"CREATE TABLE [{schema}].[{tables.Log}] (Label nvarchar(50) NULL, LoggedAt datetimeoffset(7) NULL, SinkRows int NULL); "
            + $"CREATE TABLE [{schema}].[{tables.Strict}] (Id int NOT NULL PRIMARY KEY, Grp nvarchar(20) NULL, Name nvarchar(50) NULL, Points int NOT NULL CHECK (Points > 0), Modified datetimeoffset(7) NULL);");
        await fixture.Exec(
            $"CREATE PROCEDURE [{schema}].[LogLoad_{tables.Log}] @RunLabel nvarchar(50), @RunAt datetimeoffset(7) AS "
            + $"INSERT INTO [{schema}].[{tables.Log}] (Label, LoggedAt, SinkRows) SELECT @RunLabel, @RunAt, COUNT(*) FROM [{schema}].[{tables.Sink}];");
        return tables;
    }

    private static string Procedure(Tables tables) => $"LogLoad_{tables.Log}";

    private async Task Seed(Tables tables, params (int Id, string Grp, string Name, int Points, string Modified)[] rows)
    {
        foreach (var row in rows)
        {
            await fixture.Exec(
                $"INSERT INTO [{fixture.Schema}].[{tables.Source}] (Id, Grp, Name, Points, Modified) VALUES ({row.Id}, N'{row.Grp}', N'{row.Name}', {row.Points}, '{row.Modified}')");
        }
    }

    private Task SeedFive(Tables tables) => Seed(
        tables,
        (1, "a", "n1", 1, "2026-03-01T00:00:00+00:00"),
        (2, "b", "n2", 2, "2026-03-02T00:00:00+00:00"),
        (3, "a", "n3", 3, "2026-03-03T00:00:00+00:00"),
        (4, "b", "n4", 4, "2026-03-04T00:00:00+00:00"),
        (5, "a", "n5", 5, "2026-03-05T00:00:00+00:00"));

    private async Task<int> Count(string table) => Convert.ToInt32(await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{table}]"));

    private async Task<int[]> Ids(string table, string order = "Id") =>
        (await fixture.Rows($"SELECT Id FROM [{fixture.Schema}].[{table}] ORDER BY {order}")).Select(r => Convert.ToInt32(r["Id"])).ToArray();

    private async Task<List<Dictionary<string, object?>>> Extract(Tables tables) =>
        await fixture.Rows($"SELECT Id, Grp, Name, Points, Modified FROM [{fixture.Schema}].[{tables.Source}] ORDER BY Id");

    private Dictionary<string, IDataContainer> Containers(Tables tables, bool sinkNumbersRows = false)
    {
        Col[] sink = sinkNumbersRows ? [Int("Seq", identity: true), .. Columns] : Columns;
        return new Dictionary<string, IDataContainer>
        {
            ["Source"] = Container(fixture, tables.Source, _connectionId, "Table", Columns),
            ["Sink"] = Container(fixture, tables.Sink, _connectionId, "Table", sink),
            ["Strict"] = Container(fixture, tables.Strict, _connectionId, "Table", Columns),
        };
    }

    private async Task<(BatchCopyPipeline Pipeline, BatchCopyPipelineConfiguration Configuration)> Build(
        Tables tables,
        string? loadMode,
        bool sinkNumbersRows = false,
        IList<PipelineTransformConfiguration>? transforms = null,
        PipelineLoadStepConfiguration? ownLoad = null,
        IList<PipelineLoadStepConfiguration>? steps = null,
        IList<PipelineRunParameterConfiguration>? declared = null,
        IEnumerable<string>? capabilities = null,
        SqlConnectionHolder? holder = null)
    {
        var containers = Containers(tables, sinkNumbersRows);
        var procedure = Container(
            fixture, Procedure(tables), _connectionId, "StoredProcedure", Text("@RunLabel"), Moment("@RunAt"));
        var configuration = new BatchCopyPipelineConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "acceptance",
            BatchSize = 2,
            LoadMode = loadMode,
            LoadSteps = steps ?? (ownLoad is null ? [] : [ownLoad]),
            RunParameters = declared,
            Transforms = transforms,
            SourceKind = new DataSetKind(),
            SourceDataSet = "Source",
            DestinationKind = new DataSetDestinationKind(),
            DestinationDataSet = "Sink",
        };
        var gateway = new RealGateway(fixture, await Extract(tables), containers, tables.Watermark);
        var connection = holder is null ? null : TransactionalConnection(holder.Connection);
        var dataStores = DataStoreProviderOf([.. containers.Values, procedure]);
        var pipeline = new BatchCopyPipeline(
            configuration,
            NullLogger<BatchCopyPipeline>.Instance,
            gateway.Provider,
            connectionProvider: connection is null ? null : ConnectionProviderOf(_connectionId, connection),
            dataStoreProvider: dataStores,
            dataSets: DataSetProviderOf(("Source", tables.Source), ("Sink", tables.Sink), ("Strict", tables.Strict)),
            connectionTypes: new SingleTypeResolver(ConnectionType(capabilities ?? AllCapabilities, "StoredProcedure")));
        return (pipeline, configuration);
    }

    private sealed class SqlConnectionHolder(Microsoft.Data.SqlClient.SqlConnection connection) : IAsyncDisposable
    {
        public Microsoft.Data.SqlClient.SqlConnection Connection { get; } = connection;

        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }

    private async Task<SqlConnectionHolder> OpenHolder() => new(await fixture.Open());

    private static PipelineLoadColumnConfiguration Column(string role, string column, int ordinal = 0) =>
        new() { Name = column, Role = role, ColumnName = column, Ordinal = ordinal };

    private static PipelineLoadStepConfiguration OwnLoad(params PipelineLoadColumnConfiguration[] columns) =>
        new() { Name = PipelineLoadStepConfiguration.PipelineLoadName, Phase = "Load", StepOrder = null, IsEnabled = true, Columns = columns };

    private static PipelineExecutionOptions Run(params (string Name, object? Value)[] parameters) =>
        PipelineExecutionOptions.Production with
        {
            TenantId = Tenant,
            Parameters = parameters.Length == 0 ? null : parameters.ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase),
        };

    private static PipelineTransformConfiguration Filter(string expression) =>
        new() { Id = Guid.NewGuid(), Name = "filter", OperationType = "Filter", ExecutionOrder = 1, IsEnabled = true, FilterExpression = expression };

    private static PipelineLoadStepConfiguration InsertStep(string name, int order, string dataSet) =>
        new()
        {
            Name = name,
            StepOrder = order,
            Kind = "BulkInsert",
            DestinationDataSet = dataSet,
            IsEnabled = true,
            Columns = Columns.Select((c, i) => Column("Write", c.Name, i)).ToList(),
        };

    private static PipelineLoadStepConfiguration ExecuteStep(string name, int order, string container, params PipelineLoadValueSourceConfiguration[] parameters) =>
        new()
        {
            Name = name,
            StepOrder = order,
            Kind = "Execute",
            DestinationDataSet = "Sink",
            DestinationDataStoreName = "Main",
            DestinationPathName = "dbo",
            DestinationContainerName = container,
            IsEnabled = true,
            ValueSources = parameters,
        };

    private static PipelineLoadValueSourceConfiguration Constant(string name, string value) =>
        new() { Name = name, ColumnName = name, SourceKind = "Constant", ConstantValue = value, ConstantDataType = "String" };

    private static PipelineLoadValueSourceConfiguration Now(string name) =>
        new() { Name = name, ColumnName = name, SourceKind = "CurrentTimestamp" };

    // ---- the load modes, from the same source

    [Fact]
    public async Task AppendAddsTheSourceRowsToWhatTheDestinationHolds()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        await fixture.Exec($"INSERT INTO [{fixture.Schema}].[{tables.Sink}] (Id, Name) VALUES (100, N'existing')");
        var (pipeline, _) = await Build(tables, "Append");

        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        outcome.Value!.RecordsLoaded.ShouldBe(5);
        (await Ids(tables.Sink)).ShouldBe(new[] { 1, 2, 3, 4, 5, 100 });
    }

    [Fact]
    public async Task ReplaceEmptiesTheDestinationThenLoadsTheSourceRows()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        await fixture.Exec($"INSERT INTO [{fixture.Schema}].[{tables.Sink}] (Id, Name) VALUES (100, N'existing')");
        var (pipeline, _) = await Build(tables, "Replace");

        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        (await Ids(tables.Sink)).ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public async Task UpsertInsertsNewKeysUpdatesTheChangedRowAndLeavesTheUnchangedRowAlone()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        await fixture.Exec(
            $"INSERT INTO [{fixture.Schema}].[{tables.Sink}] (Id, Grp, Name, Points, Modified) VALUES "
            + "(1, N'a', N'n1', 1, '2026-03-01T00:00:00+00:00'), (2, N'b', N'stale', 2, '2026-03-02T00:00:00+00:00')");
        var own = OwnLoad(
            Column("Key", "Id"), Column("Compare", "Name"),
            Column("Write", "Id"), Column("Write", "Grp", 1), Column("Write", "Name", 2), Column("Write", "Points", 3), Column("Write", "Modified", 4));
        var (pipeline, _) = await Build(tables, "Upsert", ownLoad: own);

        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        outcome.Value!.RecordsLoaded.ShouldBe(4);
        (await Ids(tables.Sink)).ShouldBe(new[] { 1, 2, 3, 4, 5 });
        (await fixture.Scalar($"SELECT Name FROM [{fixture.Schema}].[{tables.Sink}] WHERE Id = 2")).ShouldBe("n2");
    }

    // ---- transforms

    [Fact]
    public async Task SortOrdersTheOutputTheDestinationReceives()
    {
        fixture.RequireDatabase();
        var tables = await Create(sinkNumbersRows: true);
        await SeedFive(tables);
        var sort = new PipelineTransformConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "order",
            OperationType = "Sort",
            ExecutionOrder = 1,
            IsEnabled = true,
            SortFields = [new PipelineTransformSortFieldConfiguration { Name = "Points", FieldName = "Points", Direction = "Descending", Ordinal = 0 }],
        };
        var (pipeline, _) = await Build(tables, "Append", sinkNumbersRows: true, transforms: [sort]);

        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        (await Ids(tables.Sink, order: "Seq")).ShouldBe(new[] { 5, 4, 3, 2, 1 });
    }

    [Fact]
    public async Task LatestByKeepsOneRowPerKey()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        var latest = new PipelineTransformConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "latest",
            OperationType = "LatestBy",
            ExecutionOrder = 1,
            IsEnabled = true,
            GroupByFields = [new PipelineTransformGroupByFieldConfiguration { Name = "Grp", FieldName = "Grp", Ordinal = 0 }],
            OrderField = "Points",
            KeepRule = "Last",
        };
        var (pipeline, _) = await Build(tables, "Append", transforms: [latest]);

        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        (await Ids(tables.Sink)).ShouldBe(new[] { 4, 5 });
    }

    // ---- run parameters and the watermark

    [Fact]
    public async Task ARunParameterFiltersTheExtract()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        var declared = new[] { new PipelineRunParameterConfiguration { Name = "MinPoints", ParameterType = "Int64", IsRequired = true } };
        var (pipeline, _) = await Build(tables, "Append", transforms: [Filter("Points >= {{MinPoints}}")], declared: declared);

        var outcome = await pipeline.Execute(Run(("MinPoints", 4L)), TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        (await Ids(tables.Sink)).ShouldBe(new[] { 4, 5 });
    }

    [Fact]
    public async Task TheWatermarkIsStoredAfterASuccessfulRun()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        var before = DateTimeOffset.UtcNow;
        var (first, configuration) = await Build(tables, "Append");
        var firstOutcome = await first.Execute(Run(), TestContext.Current.CancellationToken);
        firstOutcome.IsSuccess.ShouldBeTrue(firstOutcome.CurrentMessage);

        var stored = (await fixture.Rows($"SELECT LastSuccessfulRun, ExecutionId, PipelineId, TenantId FROM [{fixture.Schema}].[{tables.Watermark}]")).Single();

        ((DateTimeOffset)stored["LastSuccessfulRun"]!).ShouldBeGreaterThanOrEqualTo(before);
        stored["ExecutionId"].ShouldBe(firstOutcome.Value!.ExecutionId);
        stored["PipelineId"].ShouldBe(configuration.Id);
        stored["TenantId"].ShouldBe(Tenant);
    }

    [Fact]
    public async Task AFailedLoadLeavesTheWatermarkWhereItWas()
    {
        fixture.RequireDatabase();
        var tables = await Create(sinkCheck: "Points > 0");
        await Seed(tables, (1, "a", "n1", 1, "2026-03-01T00:00:00+00:00"), (2, "a", "n2", -2, "2026-03-02T00:00:00+00:00"));
        var previous = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rowId = Guid.NewGuid();
        var (pipeline, configuration) = await Build(tables, "Append");
        await fixture.Exec(
            $"INSERT INTO [{fixture.Schema}].[{tables.Watermark}] (Id, PipelineId, TenantId, LastSuccessfulRun, ExecutionId) VALUES ('{rowId}', '{configuration.Id}', '{Tenant}', '{previous:O}', '{Guid.NewGuid()}')");

        var outcome = await pipeline.Execute(Run(), TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeFalse();
        ((DateTimeOffset)(await fixture.Scalar($"SELECT LastSuccessfulRun FROM [{fixture.Schema}].[{tables.Watermark}] WHERE Id = '{rowId}'"))!).ShouldBe(previous);
    }

    [Fact]
    public async Task TheStoredWatermarkFiltersTheNextRunsExtract()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        var (pipeline, configuration) = await Build(tables, "Append", transforms: [Filter("Modified > {{LastSuccessfulRun}}")]);
        await fixture.Exec(
            $"INSERT INTO [{fixture.Schema}].[{tables.Watermark}] (Id, PipelineId, TenantId, LastSuccessfulRun, ExecutionId) VALUES ('{Guid.NewGuid()}', '{configuration.Id}', '{Tenant}', '2026-03-03T00:00:00+00:00', '{Guid.NewGuid()}')");

        var outcome = await pipeline.Execute(Run(), TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        (await Ids(tables.Sink)).ShouldBe(new[] { 4, 5 });
        ((DateTimeOffset)(await fixture.Scalar($"SELECT LastSuccessfulRun FROM [{fixture.Schema}].[{tables.Watermark}]"))!).ShouldBeGreaterThan(new DateTimeOffset(2026, 3, 3, 0, 0, 0, TimeSpan.Zero));
    }

    // ---- load steps, Execute among them

    [Fact]
    public async Task AnInsertThenAStoredProcedureRunInOrderInOneTransactionAndTheProceduresEffectIsVisible()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        await using var holder = await OpenHolder();
        var steps = new[]
        {
            InsertStep("load", 1, "Sink"),
            ExecuteStep("log", 2, Procedure(tables), Constant("RunLabel", "nightly"), Now("RunAt")),
        };
        var (pipeline, _) = await Build(tables, null, steps: steps, holder: holder);
        var before = DateTimeOffset.UtcNow.AddMinutes(-5);

        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        (await Ids(tables.Sink)).ShouldBe(new[] { 1, 2, 3, 4, 5 });
        var log = (await fixture.Rows($"SELECT Label, LoggedAt, SinkRows FROM [{fixture.Schema}].[{tables.Log}]")).Single();
        log["Label"].ShouldBe("nightly");
        ((DateTimeOffset)log["LoggedAt"]!).ShouldBeGreaterThan(before);

        // The procedure counted the sink's rows: it ran after the insert, inside the same transaction, and saw them.
        log["SinkRows"].ShouldBe(5);
    }

    [Fact]
    public async Task AFailureOnALaterStepRollsBackTheInsertAndTheProcedure()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await Seed(tables, (1, "a", "n1", 1, "2026-03-01T00:00:00+00:00"), (2, "a", "n2", -2, "2026-03-02T00:00:00+00:00"));
        await using var holder = await OpenHolder();
        var steps = new[]
        {
            InsertStep("load", 1, "Sink"),
            ExecuteStep("log", 2, Procedure(tables), Constant("RunLabel", "nightly"), Now("RunAt")),
            InsertStep("strict", 3, "Strict"),
        };
        var (pipeline, _) = await Build(tables, null, steps: steps, holder: holder);

        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeFalse();
        (await Count(tables.Sink)).ShouldBe(0);
        (await Count(tables.Log)).ShouldBe(0);
        (await Count(tables.Strict)).ShouldBe(0);
        (await Count(tables.Watermark)).ShouldBe(0);
    }

    [Fact]
    public async Task AnExecuteWhoseParameterIsNotAFieldOfTheContainerIsRefusedAndTheInsertBeforeItIsRolledBack()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        await using var holder = await OpenHolder();
        var steps = new[]
        {
            InsertStep("load", 1, "Sink"),
            ExecuteStep("log", 2, Procedure(tables), Constant("NoSuchParameter", "x")),
        };
        var (pipeline, _) = await Build(tables, null, steps: steps, holder: holder);

        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("NoSuchParameter");
        (await Count(tables.Sink)).ShouldBe(0);
        (await Count(tables.Log)).ShouldBe(0);
    }

    [Fact]
    public async Task AnExecuteNamingAContainerThatIsNotExecutableIsRefusedBeforeAnythingRuns()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        await using var holder = await OpenHolder();
        var steps = new[]
        {
            InsertStep("load", 1, "Sink"),
            ExecuteStep("log", 2, tables.Strict),
        };
        var (pipeline, _) = await Build(tables, null, steps: steps, holder: holder);

        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("StoredProcedure");
        (await Count(tables.Sink)).ShouldBe(0);
    }

    [Fact]
    public async Task AnExecuteIsRefusedWhenTheDestinationsConnectionTypeDoesNotListTheExecuteCapability()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        await using var holder = await OpenHolder();
        var steps = new[]
        {
            InsertStep("load", 1, "Sink"),
            ExecuteStep("log", 2, Procedure(tables), Constant("RunLabel", "nightly")),
        };
        var (pipeline, _) = await Build(tables, null, steps: steps, capabilities: AllCapabilities.Where(name => name != "Execute"), holder: holder);

        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("Execute");
        (await Count(tables.Sink)).ShouldBe(0);
        (await Count(tables.Log)).ShouldBe(0);
    }

    [Fact]
    public async Task AnInsertFromQueryStepCopiesTheSourceDataSetsRowsInOneStatementWithAParameterInItsFilter()
    {
        fixture.RequireDatabase();
        var tables = await Create();
        await SeedFive(tables);
        await using var holder = await OpenHolder();
        var copy = new PipelineLoadStepConfiguration
        {
            Phase = "Load",
            Name = "copy",
            StepOrder = 1,
            Kind = "InsertFromQuery",
            DestinationDataSet = "Sink",
            SourceDataSet = "Source",
            FilterExpression = "Points >= {{MinPoints}}",
            IsEnabled = true,
            Columns = Columns.Select((c, i) => Column("Write", c.Name, i)).ToList(),
        };
        var declared = new[] { new PipelineRunParameterConfiguration { Name = "MinPoints", ParameterType = "Int64", IsRequired = true } };
        var (pipeline, _) = await Build(tables, null, steps: [copy], declared: declared, holder: holder);

        var outcome = await pipeline.Execute(Run(("MinPoints", 3L)), TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        (await Ids(tables.Sink)).ShouldBe(new[] { 3, 4, 5 });
    }
}
