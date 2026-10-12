using Fdw.Commands.Data;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions.DataDestination.Kinds;
using Fdw.Services.Pipelines.Abstractions.DataSource.Kinds;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Pipelines.Loads.LoadTestDoubles;
using EtlExecution = Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// <see cref="BatchCopyPipeline"/> honors the load definition (FDW-1109): the load mode, the load steps through the unit of
/// work, the run parameters and the last-successful watermark. The order of gateway calls is the behavior these tests read.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class BatchCopyPipelineLoadTests : IDisposable
{
    private const string OperationalStore = "Ops";

    private static readonly Guid Tenant = Guid.NewGuid();
    private readonly List<string> _events = [];

    // The operational store is host configuration (EtlPipelineTypes.OperationalConnection, no default); a run that tracks a
    // watermark reads and writes it there, so these tests name it as a host would and restore it afterwards.
    public BatchCopyPipelineLoadTests()
    {
        EtlPipelineTypes.OperationalConnection = OperationalStore;
    }

    public void Dispose()
    {
        EtlPipelineTypes.OperationalConnection = null;
    }

    private static PipelineLoadColumnConfiguration Column(string role, string column, int ordinal = 0) =>
        new() { Name = column, Role = role, ColumnName = column, Ordinal = ordinal };

    private static PipelineLoadStepConfiguration OwnLoad(params PipelineLoadColumnConfiguration[] columns) =>
        new() { Name = PipelineLoadStepConfiguration.PipelineLoadName, Phase = "Load", StepOrder = null, IsEnabled = true, Columns = columns };

    private static BatchCopyPipelineConfiguration Configuration(
        string? loadMode,
        int batchSize = 2,
        PipelineLoadStepConfiguration? ownLoad = null,
        IList<PipelineTransformConfiguration>? transforms = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "copy",
            BatchSize = batchSize,
            LoadMode = loadMode,
            LoadSteps = ownLoad is null ? [] : [ownLoad],
            Transforms = transforms,
            SourceKind = new DataSetKind(),
            SourceDataSet = "Source",
            DestinationKind = new DataSetDestinationKind(),
            DestinationDataSet = "Sink",
        };

    private static PipelineTransformConfiguration Filter(string expression) =>
        new() { Id = Guid.NewGuid(), Name = "filter", OperationType = "Filter", ExecutionOrder = 1, IsEnabled = true, FilterExpression = expression };

    private static BatchCopyPipeline Pipeline(BatchCopyPipelineConfiguration configuration, LoadingGateway gateway) =>
        new(configuration, NullLogger<BatchCopyPipeline>.Instance, gateway.Provider);

    private LoadingGateway Gateway(int rows = 5, IReadOnlyList<EtlExecution.EtlPipelineWatermarkRecord>? watermarks = null, bool failLoads = false, bool failWatermarkWrite = false) =>
        new(LoadingGateway.Rows(rows), _events, watermarks, failLoads, failWatermarkWrite);

    private static PipelineExecutionOptions Run(params (string Name, object? Value)[] parameters) =>
        PipelineExecutionOptions.Production with
        {
            TenantId = Tenant,
            Parameters = parameters.Length == 0 ? null : parameters.ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase),
        };

    // ---- the load mode

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AppendInsertsEachPageAsItArrives()
    {
        // Arrange
        var gateway = Gateway();

        // Act
        var outcome = await Pipeline(Configuration("Append"), gateway).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.ShouldBe(new[] { "read@0", "insert:2", "read@2", "insert:2", "read@4", "insert:1" });
        outcome.Value!.RecordsLoaded.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ReplaceTruncatesOnceAfterTheFirstReadThenInserts()
    {
        // Arrange
        var gateway = Gateway();

        // Act
        var outcome = await Pipeline(Configuration("Replace"), gateway).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.ShouldBe(new[] { "read@0", "truncate", "insert:2", "read@2", "insert:2", "read@4", "insert:1" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task UpsertUpsertsEachPageOnItsKeyWithItsCompareAndWriteColumns()
    {
        // Arrange
        var gateway = Gateway();
        var own = OwnLoad(Column("Key", "Id"), Column("Compare", "Name"), Column("Write", "Id"), Column("Write", "Name", 1));

        // Act
        var outcome = await Pipeline(Configuration("Upsert", ownLoad: own), gateway).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.ShouldBe(new[] { "read@0", "upsert:2", "read@2", "upsert:2", "read@4", "upsert:1" });
        var command = gateway.Loads.First().ShouldBeOfType<BulkUpsertCommand<IDictionary<string, object?>>>();
        command.Options.KeyColumns.ShouldBe(new[] { "Id" });
        command.Options.CompareColumns.ShouldBe(new[] { "Name" });
        command.Options.WriteColumns.ShouldBe(new[] { "Id", "Name" });
        command.Data.First().Keys.OrderBy(k => k, StringComparer.Ordinal).ShouldBe(new[] { "Id", "Name" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnAppendWithValueSourcesGivesEveryRowTheirColumns()
    {
        // Arrange
        var gateway = Gateway(rows: 2);
        var own = OwnLoad();
        own.ValueSources =
        [
            new PipelineLoadValueSourceConfiguration { Name = "Batch", ColumnName = "Batch", SourceKind = "Constant", ConstantValue = "7", ConstantDataType = "Int64" },
            new PipelineLoadValueSourceConfiguration { Name = "LoadedAt", ColumnName = "LoadedAt", SourceKind = "CurrentTimestamp" },
        ];

        // Act
        var outcome = await Pipeline(Configuration("Append", ownLoad: own), gateway).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        var rows = gateway.Loads.Cast<BulkInsertCommand<IDictionary<string, object?>>>().SelectMany(c => c.Data).ToList();
        rows.Count.ShouldBe(2);
        rows.ShouldAllBe(r => (long)r["Batch"]! == 7L && r["LoadedAt"] is DateTimeOffset);
    }

    // ---- refusals before anything is read

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatStatesNoLoadModeAndNoStepsIsRefusedBeforeAnythingIsRead()
    {
        // Act
        var outcome = await Pipeline(Configuration(null), Gateway()).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("must state a load mode");
        _events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpsertWithNoOptionsIsRefusedBeforeAnythingIsRead()
    {
        // Act
        var outcome = await Pipeline(Configuration("Upsert"), Gateway()).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        _events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TruncateBeforeLoadBesideAppendIsRefusedNotQuietlyHonored()
    {
        // Arrange
        var configuration = Configuration("Append");
        configuration.TruncateBeforeLoad = true;

        // Act
        var outcome = await Pipeline(configuration, Gateway()).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("TruncateBeforeLoad");
        _events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineWithLoadStepsAndNoServicesToRunThemIsRefusedLoudlyAfterTheWholeOutputIsGathered()
    {
        // Arrange: load steps need the whole output at once, so every page is read before the load is attempted
        var configuration = Configuration(null);
        configuration.LoadSteps =
        [
            new PipelineLoadStepConfiguration
            {
                Phase = "Load",
                Name = "load", StepOrder = 1, Kind = "BulkInsert", DestinationDataSet = "Sink", IsEnabled = true, Columns = [Column("Write", "Name")],
            },
        ];

        // Act
        var outcome = await Pipeline(configuration, Gateway()).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        _events.ShouldBe(new[] { "read@0", "read@2", "read@4" });
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("data set and data store providers");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpsertThatFlagsTheKeysAbsentFromTheSourceGathersTheWholeOutputToo()
    {
        // Arrange
        var own = OwnLoad(Column("Key", "Id"), Column("Write", "Id"), Column("Write", "Name", 1));
        own.SoftDeleteMode = "AbsentFromSource";
        own.DeletedFlagColumn = "IsDeleted";

        // Act
        var outcome = await Pipeline(Configuration("Upsert", ownLoad: own), Gateway()).Execute(TestContext.Current.CancellationToken);

        // Assert: the pages are all read before the first (refused, here) load
        outcome.IsSuccess.ShouldBeFalse();
        _events.ShouldBe(new[] { "read@0", "read@2", "read@4" });
    }

    // ---- the new transforms in a run

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASortGathersTheWholeOutputThenLoadsItInOrderAcrossTheBatches()
    {
        // Arrange
        var sort = new PipelineTransformConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "order",
            OperationType = "Sort",
            ExecutionOrder = 1,
            IsEnabled = true,
            SortFields = [new PipelineTransformSortFieldConfiguration { Name = "Points", FieldName = "Points", Direction = "Descending", Ordinal = 0 }],
        };
        var gateway = Gateway();

        // Act
        var outcome = await Pipeline(Configuration("Append", transforms: [sort]), gateway).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        // Why across the batches: the gathered, sorted set is loaded in BatchSize batches one after another, so the order
        // holds across them; what a sort changes is that every page is read before the first write, not how many calls load it
        _events.Take(3).ShouldBe(new[] { "read@0", "read@2", "read@4" });
        _events.Skip(3).ShouldAllBe(e => e.StartsWith("insert:", StringComparison.Ordinal));
        var rows = gateway.Loads.Cast<BulkInsertCommand<IDictionary<string, object?>>>().SelectMany(c => c.Data).Select(r => (int)r["Points"]!).ToArray();
        rows.ShouldBe(new[] { 5, 4, 3, 2, 1 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALatestByKeepsOneRowPerKeyBeforeTheLoad()
    {
        // Arrange: ten rows whose key is Points modulo 3 would need a computed field, so key on a field with duplicates instead
        var source = Enumerable.Range(1, 6)
            .Select(i => new Dictionary<string, object?> { ["Id"] = i, ["Group"] = i % 2 == 0 ? "even" : "odd", ["Version"] = i })
            .ToList();
        var gateway = new LoadingGateway(source, _events);
        var latest = new PipelineTransformConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "latest",
            OperationType = "LatestBy",
            ExecutionOrder = 1,
            IsEnabled = true,
            GroupByFields = [new PipelineTransformGroupByFieldConfiguration { Name = "Group", FieldName = "Group", Ordinal = 0 }],
            OrderField = "Version",
            KeepRule = "Last",
        };

        // Act
        var outcome = await Pipeline(Configuration("Append", transforms: [latest]), gateway).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        var rows = gateway.Loads.Cast<BulkInsertCommand<IDictionary<string, object?>>>().SelectMany(c => c.Data).Select(r => (int)r["Id"]!).OrderBy(i => i).ToArray();
        rows.ShouldBe(new[] { 5, 6 });
    }

    // ---- run parameters

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunParameterFiltersTheExtractTheRunLoads()
    {
        // Arrange
        var configuration = Configuration("Append", transforms: [Filter("Points >= {{MinPoints}}")]);
        configuration.RunParameters = [new PipelineRunParameterConfiguration { Name = "MinPoints", ParameterType = "Int64", IsRequired = true }];
        var gateway = Gateway();

        // Act
        var outcome = await Pipeline(configuration, gateway).Execute(
            PipelineExecutionOptions.Production with { Parameters = new Dictionary<string, object?> { ["MinPoints"] = 4L } },
            TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        outcome.Value!.RecordsLoaded.ShouldBe(2);
        gateway.Loads.Cast<BulkInsertCommand<IDictionary<string, object?>>>().SelectMany(c => c.Data).Select(r => (int)r["Points"]!).OrderBy(p => p)
            .ShouldBe(new[] { 4, 5 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunSuppliedAParameterThePipelineDoesNotDeclareIsRefusedBeforeAnythingIsRead()
    {
        // Act
        var outcome = await Pipeline(Configuration("Append"), Gateway()).Execute(
            PipelineExecutionOptions.Production with { Parameters = new Dictionary<string, object?> { ["Other"] = 1L } },
            TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("Other");
        _events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunMissingARequiredParameterIsRefusedBeforeAnythingIsRead()
    {
        // Arrange
        var configuration = Configuration("Append");
        configuration.RunParameters = [new PipelineRunParameterConfiguration { Name = "MinPoints", ParameterType = "Int64", IsRequired = true }];

        // Act
        var outcome = await Pipeline(configuration, Gateway()).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("MinPoints");
        _events.ShouldBeEmpty();
    }

    // ---- the watermark

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASuccessfulRunInsertsTheWatermarkAfterTheLoadCommitted()
    {
        // Arrange
        var gateway = Gateway(rows: 2);
        var before = DateTimeOffset.UtcNow;

        // Act
        var outcome = await Pipeline(Configuration("Append"), gateway).Execute(Run(), TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.ShouldBe(new[] { "watermark:read", "read@0", "insert:2", "read@2", "watermark:insert" });
        var written = gateway.WatermarkWrites.Single();
        written.TenantId.ShouldBe(Tenant);
        written.ExecutionId.ShouldBe(outcome.Value!.ExecutionId);
        written.LastSuccessfulRun.ShouldBeInRange(before, DateTimeOffset.UtcNow);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASuccessfulRunMovesAnExistingWatermarkForward()
    {
        // Arrange
        var existing = new EtlExecution.EtlPipelineWatermarkRecord
        {
            Id = Guid.NewGuid(),
            LastSuccessfulRun = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };
        var gateway = Gateway(rows: 2, watermarks: [existing]);

        // Act
        var outcome = await Pipeline(Configuration("Append"), gateway).Execute(Run(), TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.ShouldBe(new[] { "watermark:read", "read@0", "insert:2", "read@2", "watermark:update" });
        gateway.WatermarkWrites.Single().Id.ShouldBe(existing.Id);
        gateway.WatermarkWrites.Single().LastSuccessfulRun.ShouldBeGreaterThan(existing.LastSuccessfulRun);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheStoredWatermarkFiltersTheNextRunsExtract()
    {
        // Arrange: rows modified on three days, the watermark after the first two
        var source = new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Modified"] = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero) },
            new() { ["Id"] = 2, ["Modified"] = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero) },
            new() { ["Id"] = 3, ["Modified"] = new DateTimeOffset(2026, 3, 3, 0, 0, 0, TimeSpan.Zero) },
        };
        var stored = new EtlExecution.EtlPipelineWatermarkRecord
        {
            Id = Guid.NewGuid(),
            LastSuccessfulRun = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero),
        };
        var gateway = new LoadingGateway(source, _events, [stored]);

        // Act
        var outcome = await Pipeline(Configuration("Append", transforms: [Filter("Modified > {{LastSuccessfulRun}}")]), gateway)
            .Execute(Run(), TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        gateway.Loads.Cast<BulkInsertCommand<IDictionary<string, object?>>>().SelectMany(c => c.Data).Select(r => (int)r["Id"]!).ShouldBe(new[] { 3 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFirstRunThatReferencesTheWatermarkIsRefusedAndWritesNothing()
    {
        // Arrange: no row is stored and no parameter overrides it
        var gateway = Gateway();

        // Act
        var outcome = await Pipeline(Configuration("Append", transforms: [Filter("Id > 0 AND Points < {{LastSuccessfulRun}}")]), gateway)
            .Execute(Run(), TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("LastSuccessfulRun");
        _events.ShouldBe(new[] { "watermark:read", "read@0" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASuppliedLastSuccessfulRunLetsAFirstRunReferenceTheWatermark()
    {
        // Arrange
        var source = new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Modified"] = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero) },
            new() { ["Id"] = 2, ["Modified"] = new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero) },
        };
        var gateway = new LoadingGateway(source, _events);

        // Act
        var outcome = await Pipeline(Configuration("Append", transforms: [Filter("Modified > {{LastSuccessfulRun}}")]), gateway).Execute(
            Run(("LastSuccessfulRun", new DateTimeOffset(2026, 3, 3, 0, 0, 0, TimeSpan.Zero))),
            TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        gateway.Loads.Cast<BulkInsertCommand<IDictionary<string, object?>>>().SelectMany(c => c.Data).Select(r => (int)r["Id"]!).ShouldBe(new[] { 2 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailedLoadLeavesTheWatermarkWhereItWas()
    {
        // Arrange
        var gateway = Gateway(rows: 2, failLoads: true);

        // Act
        var outcome = await Pipeline(Configuration("Append"), gateway).Execute(Run(), TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        _events.ShouldBe(new[] { "watermark:read", "read@0", "insert:2" });
        gateway.WatermarkWrites.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunThatRecordedErrorsUnderContinueOnErrorLeavesTheWatermarkWhereItWas()
    {
        // Arrange: the load fails but the run is told to carry on, so it ends without failing and with errors recorded
        var configuration = Configuration("Append");
        configuration.ContinueOnError = true;
        var gateway = Gateway(rows: 2, failLoads: true);

        // Act
        var outcome = await Pipeline(configuration, gateway).Execute(Run(), TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        outcome.Value!.Errors.ShouldNotBeEmpty();
        gateway.WatermarkWrites.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AWatermarkThatCannotBeWrittenFailsTheRunSayingTheDataLoaded()
    {
        // Arrange
        var gateway = Gateway(rows: 2, failWatermarkWrite: true);

        // Act
        var outcome = await Pipeline(Configuration("Append"), gateway).Execute(Run(), TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("loaded its data");
        _events.ShouldContain("insert:2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ATestModeRunReadsTheWatermarkButNeverWritesIt()
    {
        // Arrange
        var gateway = Gateway(rows: 2);

        // Act
        var outcome = await Pipeline(Configuration("Append"), gateway).Execute(
            PipelineExecutionOptions.DefaultTestMode with { TenantId = Tenant }, TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.ShouldBe(new[] { "watermark:read", "read@0", "read@2" });
        gateway.WatermarkWrites.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunWithNoTenantNeitherReadsNorWritesAWatermark()
    {
        // Arrange
        var gateway = Gateway(rows: 2);

        // Act
        var outcome = await Pipeline(Configuration("Append"), gateway).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.ShouldBe(new[] { "read@0", "insert:2", "read@2" });
    }

    // ---- load steps through the unit of work, end to end through the engine

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task LoadStepsOfAnInsertThenAnExecuteRunInOneTransactionAfterTheWholeOutputIsGathered()
    {
        // Arrange
        var warehouse = Guid.NewGuid();
        var connection = new RecordingConnection("warehouse", _events);
        var configuration = Configuration(null);
        configuration.LoadSteps =
        [
            new PipelineLoadStepConfiguration
            {
                Phase = "Load",
                Name = "load", StepOrder = 1, Kind = "BulkInsert", DestinationDataSet = "Sink", IsEnabled = true, Columns = [Column("Write", "Name")],
            },
            new PipelineLoadStepConfiguration
            {
                Phase = "Load",
                Name = "summarise",
                StepOrder = 2,
                Kind = "Execute",
                DestinationDataSet = "Sink",
                DestinationDataStoreName = "Main",
                DestinationPathName = "dbo",
                DestinationContainerName = "LoadSummary",
                IsEnabled = true,
                ValueSources = [new PipelineLoadValueSourceConfiguration { Name = "RunAt", ColumnName = "RunAt", SourceKind = "CurrentTimestamp" }],
            },
        ];
        var gateway = Gateway(rows: 3);
        var pipeline = new BatchCopyPipeline(
            configuration,
            NullLogger<BatchCopyPipeline>.Instance,
            gateway.Provider,
            connectionProvider: ConnectionProviderOf((warehouse, connection.Connection)).Object,
            dataStoreProvider: DataStoreProviderOf(
                ("Main", "dbo", "Sink", Container("Sink", warehouse)),
                ("Main", "dbo", "LoadSummary", Container("LoadSummary", warehouse, "StoredProcedure"))).Object,
            dataSets: DataSetProviderOf(("Sink", [Source("Main", "dbo", "Sink")])).Object,
            connectionTypes: new TypeResolver(new Dictionary<Guid, IConnectionType> { [warehouse] = ConnectionType(AllCapabilities, ["StoredProcedure"]) }));

        // Act
        var outcome = await pipeline.Execute(Run(), TestContext.Current.CancellationToken);

        // Assert: the pages are read, then one transaction holds the insert and the procedure, then the watermark moves
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.ShouldBe(new[]
        {
            "watermark:read", "read@0", "read@2",
            "begin:warehouse", "exec:warehouse:BulkInsert", "exec:warehouse:Execute", "commit:warehouse",
            "watermark:insert",
        });
        outcome.Value!.RecordsLoaded.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailedStepRollsTheTransactionBackAndLeavesTheWatermark()
    {
        // Arrange
        var warehouse = Guid.NewGuid();
        var connection = new RecordingConnection("warehouse", _events, failVerb: "Execute");
        var configuration = Configuration(null);
        configuration.LoadSteps =
        [
            new PipelineLoadStepConfiguration
            {
                Phase = "Load",
                Name = "load", StepOrder = 1, Kind = "BulkInsert", DestinationDataSet = "Sink", IsEnabled = true, Columns = [Column("Write", "Name")],
            },
            new PipelineLoadStepConfiguration
            {
                Phase = "Load",
                Name = "summarise",
                StepOrder = 2,
                Kind = "Execute",
                DestinationDataSet = "Sink",
                DestinationDataStoreName = "Main",
                DestinationPathName = "dbo",
                DestinationContainerName = "LoadSummary",
                IsEnabled = true,
            },
        ];
        var gateway = Gateway(rows: 2);
        var pipeline = new BatchCopyPipeline(
            configuration,
            NullLogger<BatchCopyPipeline>.Instance,
            gateway.Provider,
            connectionProvider: ConnectionProviderOf((warehouse, connection.Connection)).Object,
            dataStoreProvider: DataStoreProviderOf(
                ("Main", "dbo", "Sink", Container("Sink", warehouse)),
                ("Main", "dbo", "LoadSummary", Container("LoadSummary", warehouse, "StoredProcedure"))).Object,
            dataSets: DataSetProviderOf(("Sink", [Source("Main", "dbo", "Sink")])).Object,
            connectionTypes: new TypeResolver(new Dictionary<Guid, IConnectionType> { [warehouse] = ConnectionType(AllCapabilities, ["StoredProcedure"]) }));

        // Act
        var outcome = await pipeline.Execute(Run(), TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        _events.ShouldBe(new[]
        {
            "watermark:read", "read@0", "read@2",
            "begin:warehouse", "exec:warehouse:BulkInsert", "exec:warehouse:Execute", "rollback:warehouse",
        });
        gateway.WatermarkWrites.ShouldBeEmpty();
    }
}
