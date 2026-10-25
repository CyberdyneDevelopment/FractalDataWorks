using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Commands.Data.Loads;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Pipelines.Loads.LoadTestDoubles;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="PipelineLoadExecutor"/> (FDW-1109): load units run through the unit of work, one transaction per
/// connection, checked against the connection type before any transaction opens.
/// </summary>
public sealed class PipelineLoadExecutorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 9, 10, 11, TimeSpan.Zero);
    private static readonly IReadOnlyDictionary<string, object?> NoParameters = new Dictionary<string, object?>();

    private readonly Guid _warehouse = Guid.NewGuid();
    private readonly Guid _archive = Guid.NewGuid();
    private readonly List<string> _events = [];

    private static PipelineLoadColumnConfiguration Column(string role, string column, int ordinal = 0) =>
        new() { Name = column, Role = role, ColumnName = column, Ordinal = ordinal };

    private static PipelineLoadStepConfiguration Insert(string name, int order, string dataSet = "Orders") =>
        new() { Name = name, StepOrder = order, Kind = "Insert", DestinationDataSet = dataSet, IsEnabled = true, Columns = [Column("Write", "Name")] };

    private static PipelineLoadStepConfiguration Truncate(string name, int order, string dataSet = "Orders") =>
        new() { Name = name, StepOrder = order, Kind = "Truncate", DestinationDataSet = dataSet, IsEnabled = true };

    private static List<IDictionary<string, object?>> Rows(int count = 2) =>
        Enumerable.Range(1, count)
            .Select(i => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Id"] = i, ["Name"] = $"n{i}" })
            .ToList();

    private static LoadUnit[] Units(params PipelineLoadStepConfiguration[] steps) => steps.Select(LoadUnit.ForStep).ToArray();

    private (PipelineLoadExecutor Executor, RecordingConnection Warehouse, RecordingConnection Archive) Executor(
        string? failVerb = null,
        string[]? warehouseCapabilities = null,
        string archiveStagingContainer = "Staging")
    {
        var warehouse = new RecordingConnection("warehouse", _events, failVerb);
        var archive = new RecordingConnection("archive", _events, failVerb);
        var dataSets = DataSetProviderOf(
            ("Orders", [Source("Main", "dbo", "Orders")]),
            ("Archive", [Source("ArchiveStore", "dbo", "Orders")]),
            ("Staging", [Source("Main", "dbo", archiveStagingContainer)]));
        var dataStores = DataStoreProviderOf(
            ("Main", "dbo", "Orders", Container("Orders", _warehouse)),
            ("Main", "dbo", "Staging", Container("Staging", _warehouse, schema: "stg", database: "Other")),
            ("Main", "dbo", "Orders2", Container("Orders2", _warehouse)),
            ("ArchiveStore", "dbo", "Orders", Container("Orders", _archive)));
        var connections = ConnectionProviderOf((_warehouse, warehouse.Connection), (_archive, archive.Connection));
        var types = new TypeResolver(new Dictionary<Guid, IConnectionType>
        {
            [_warehouse] = ConnectionType(warehouseCapabilities ?? AllCapabilities),
            [_archive] = ConnectionType(AllCapabilities),
        });
        return (new PipelineLoadExecutor(new LoadTargetResolver(dataSets.Object, dataStores.Object), connections.Object, types, NullLogger.Instance), warehouse, archive);
    }

    private static async Task<IGenericResult<int>> Run(PipelineLoadExecutor executor, LoadUnit[] units, List<IDictionary<string, object?>>? rows = null, IReadOnlyDictionary<string, object?>? parameters = null) =>
        await executor.Run("copy", units, rows ?? Rows(), parameters ?? NoParameters, Now, TestContext.Current.CancellationToken);

    // ---- one transaction, in order

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnInsertThenATruncateRunInOrderInOneTransactionOnOneConnection()
    {
        // Arrange
        var (executor, _, _) = Executor();

        // Act
        var result = await Run(executor, Units(Insert("load", 1), Truncate("clear", 2)));

        // Assert: one begin, both commands in step order, one commit
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(2);
        _events.ShouldBe(new[] { "begin:warehouse", "exec:warehouse:Insert", "exec:warehouse:Delete", "commit:warehouse" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFailureOnALaterStepRollsBackTheEarlierOnesAndCommitsNothing()
    {
        // Arrange: the delete that empties the destination fails after the insert has run
        var (executor, _, _) = Executor(failVerb: "Delete");

        // Act
        var result = await Run(executor, Units(Insert("load", 1), Truncate("clear", 2)));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _events.ShouldBe(new[] { "begin:warehouse", "exec:warehouse:Insert", "exec:warehouse:Delete", "rollback:warehouse" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFailureOnTheThirdStepRollsBackTheFirstTwo()
    {
        // Arrange
        var (executor, _, _) = Executor(failVerb: "Delete");

        // Act
        var result = await Run(executor, Units(Insert("a", 1), Insert("b", 2), Truncate("c", 3)));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _events.ShouldBe(new[]
        {
            "begin:warehouse", "exec:warehouse:Insert", "exec:warehouse:Insert", "exec:warehouse:Delete", "rollback:warehouse",
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task StepsOnDifferentConnectionsRunInSeparateTransactionsInOrder()
    {
        // Arrange
        var (executor, _, _) = Executor();

        // Act
        var result = await Run(executor, Units(Insert("a", 1), Insert("b", 2, "Archive")));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _events.ShouldBe(new[]
        {
            "begin:warehouse", "exec:warehouse:Insert", "commit:warehouse",
            "begin:archive", "exec:archive:Insert", "commit:archive",
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALaterConnectionsFailureLeavesTheEarlierConnectionsCommitAndFailsTheRun()
    {
        // Arrange: only the archive connection fails
        var warehouse = new RecordingConnection("warehouse", _events);
        var archive = new RecordingConnection("archive", _events, failVerb: "Insert");
        var dataSets = DataSetProviderOf(("Orders", [Source("Main", "dbo", "Orders")]), ("Archive", [Source("ArchiveStore", "dbo", "Orders")]));
        var dataStores = DataStoreProviderOf(("Main", "dbo", "Orders", Container("Orders", _warehouse)), ("ArchiveStore", "dbo", "Orders", Container("Orders", _archive)));
        var types = new TypeResolver(new Dictionary<Guid, IConnectionType>
        {
            [_warehouse] = ConnectionType(AllCapabilities),
            [_archive] = ConnectionType(AllCapabilities),
        });
        var executor = new PipelineLoadExecutor(
            new LoadTargetResolver(dataSets.Object, dataStores.Object),
            ConnectionProviderOf((_warehouse, warehouse.Connection), (_archive, archive.Connection)).Object,
            types,
            NullLogger.Instance);

        // Act
        var result = await Run(executor, Units(Insert("a", 1), Insert("b", 2, "Archive")));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _events.ShouldBe(new[]
        {
            "begin:warehouse", "exec:warehouse:Insert", "commit:warehouse",
            "begin:archive", "exec:archive:Insert", "rollback:archive",
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task StepsThatReturnToAConnectionTheyLeftAreRefusedBeforeAnythingRuns()
    {
        // Arrange
        var (executor, _, _) = Executor();

        // Act
        var result = await Run(executor, Units(Insert("a", 1), Insert("b", 2, "Archive"), Insert("c", 3)));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20220");
        _events.ShouldBeEmpty();
    }

    // ---- rows

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnEmptyOutputRunsNothingWhenAnyStepWorksOnRowsSoADestinationIsNeverEmptiedForAnEmptySource()
    {
        // Arrange
        var (executor, _, _) = Executor();

        // Act
        var result = await Run(executor, Units(Truncate("clear", 1), Insert("load", 2)), rows: []);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0);
        _events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task StepsThatTakeNoRowsStillRunForAnEmptyOutput()
    {
        // Arrange
        var (executor, _, _) = Executor();

        // Act
        var result = await Run(executor, Units(Truncate("clear", 1)), rows: []);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _events.ShouldBe(new[] { "begin:warehouse", "exec:warehouse:Delete", "commit:warehouse" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task NoUnitsIsNothingToDo()
    {
        // Arrange
        var (executor, _, _) = Executor();

        // Act
        var result = await Run(executor, []);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0);
    }

    // ---- Execute resolves through the data store's metadata

    // ---- the connection type's declarations

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALoadIsRefusedWhenTheConnectionTypeDoesNotListTheUnitOfWorkCapability()
    {
        // Arrange
        var (executor, _, _) = Executor(warehouseCapabilities: AllCapabilities.Where(name => name != "UnitOfWork").ToArray());

        // Act
        var result = await Run(executor, Units(Insert("load", 1)));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20238");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("UnitOfWork");
        _events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALoadNeedsNoDataCommandCapabilityFromTheConnectionType()
    {
        // Arrange: the connection type lists only the unit of work; every connection answers every data command
        var (executor, _, _) = Executor(warehouseCapabilities: ["UnitOfWork"]);

        // Act
        var result = await Run(executor, Units(Insert("insert", 1)));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _events.ShouldBe(new[] { "begin:warehouse", "exec:warehouse:Insert", "commit:warehouse" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALoadOnAConnectionThatCannotOpenATransactionIsRefused()
    {
        // Arrange
        var plain = new Mock<IGenericConnection>();
        var dataSets = DataSetProviderOf(("Orders", [Source("Main", "dbo", "Orders")]));
        var dataStores = DataStoreProviderOf(("Main", "dbo", "Orders", Container("Orders", _warehouse)));
        var executor = new PipelineLoadExecutor(
            new LoadTargetResolver(dataSets.Object, dataStores.Object),
            ConnectionProviderOf((_warehouse, plain.Object)).Object,
            new TypeResolver(new Dictionary<Guid, IConnectionType> { [_warehouse] = ConnectionType(AllCapabilities) }),
            NullLogger.Instance);

        // Act
        var result = await Run(executor, Units(Insert("load", 1)));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20219");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALoadWithoutAConnectionTypeResolverIsRefusedLoudly()
    {
        // Arrange
        var warehouse = new RecordingConnection("warehouse", _events);
        var executor = new PipelineLoadExecutor(
            new LoadTargetResolver(
                DataSetProviderOf(("Orders", [Source("Main", "dbo", "Orders")])).Object,
                DataStoreProviderOf(("Main", "dbo", "Orders", Container("Orders", _warehouse))).Object),
            ConnectionProviderOf((_warehouse, warehouse.Connection)).Object,
            null,
            NullLogger.Instance);

        // Act
        var result = await Run(executor, Units(Insert("load", 1)));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20218");
        _events.ShouldBeEmpty();
    }

    // ---- resolution

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALoadOnADataSetWithMoreThanOneSourceIsRefused()
    {
        // Arrange
        var dataSets = DataSetProviderOf(("Orders", [Source("Main", "dbo", "Orders"), Source("Main", "dbo", "Orders2")]));
        var executor = new PipelineLoadExecutor(
            new LoadTargetResolver(dataSets.Object, DataStoreProviderOf(("Main", "dbo", "Orders", Container("Orders", _warehouse))).Object),
            ConnectionProviderOf().Object,
            new TypeResolver(new Dictionary<Guid, IConnectionType>()),
            NullLogger.Instance);

        // Act
        var result = await Run(executor, Units(Insert("load", 1)));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20217");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("exactly one source");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALoadWithNoProvidersIsRefusedLoudly()
    {
        // Arrange
        var executor = new PipelineLoadExecutor(new LoadTargetResolver(null, null), null, null, NullLogger.Instance);

        // Act
        var result = await Run(executor, Units(Insert("load", 1)));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20218");
    }
}
