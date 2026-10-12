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

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// <see cref="BatchCopyPipeline"/> runs the pipeline's reject steps (FDW-1142): after the main load committed, in a unit of
/// work of their own, writing each rejected record with the reason and the stage that rejected it.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class BatchCopyPipelineRejectTests : IDisposable
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private readonly List<string> _events = [];

    public BatchCopyPipelineRejectTests()
    {
        EtlPipelineTypes.OperationalConnection = "Ops";
    }

    public void Dispose()
    {
        EtlPipelineTypes.OperationalConnection = null;
    }

    private static PipelineLoadColumnConfiguration Write(string column, int ordinal) =>
        new() { Name = column, Role = "Write", ColumnName = column, Ordinal = ordinal };

    private static BatchCopyPipelineConfiguration Configuration() => new()
    {
        Id = Guid.NewGuid(),
        Name = "copy",
        BatchSize = 2,
        LoadSteps =
        [
            new PipelineLoadStepConfiguration
            {
                Phase = "Load", Name = "load", StepOrder = 1, Kind = "BulkInsert", DestinationDataSet = "Sink", IsEnabled = true, Columns = [Write("Name", 0)],
            },
            new PipelineLoadStepConfiguration
            {
                Phase = "Reject",
                Name = "keep rejects",
                StepOrder = 1,
                Kind = "BulkInsert",
                DestinationDataSet = "Rejects",
                IsEnabled = true,
                Columns = [Write("Name", 0), Write(RejectedRecordColumns.Reason, 1), Write(RejectedRecordColumns.Stage, 2)],
            },
        ],
        Transforms =
        [
            new PipelineTransformConfiguration
            {
                Id = Guid.NewGuid(),
                Name = "Clean",
                OperationType = "Validate",
                ExecutionOrder = 1,
                IsEnabled = true,
                OnFail = "Reject",
                Checks = [new PipelineTransformCheckConfiguration { Name = "AboveOne", Expression = "Points > 1", Ordinal = 1 }],
            },
        ],
        SourceKind = new DataSetKind(),
        SourceDataSet = "Source",
        DestinationKind = new DataSetDestinationKind(),
        DestinationDataSet = "Sink",
    };

    private BatchCopyPipeline Pipeline(BatchCopyPipelineConfiguration configuration, int rows, RecordingConnection connection, Guid warehouse)
    {
        var gateway = new LoadingGateway(LoadingGateway.Rows(rows), _events, null, false, false);
        return new BatchCopyPipeline(
            configuration,
            NullLogger<BatchCopyPipeline>.Instance,
            gateway.Provider,
            connectionProvider: ConnectionProviderOf((warehouse, connection.Connection)).Object,
            dataStoreProvider: DataStoreProviderOf(
                ("Main", "dbo", "Sink", Container("Sink", warehouse)),
                ("Main", "dbo", "Rejects", Container("Rejects", warehouse))).Object,
            dataSets: DataSetProviderOf(
                ("Sink", [Source("Main", "dbo", "Sink")]),
                ("Rejects", [Source("Main", "dbo", "Rejects")])).Object,
            connectionTypes: new TypeResolver(new Dictionary<Guid, IConnectionType> { [warehouse] = ConnectionType(AllCapabilities, []) }));
    }

    private static PipelineExecutionOptions Run() => PipelineExecutionOptions.Production with { TenantId = Tenant };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RejectedRecordsAreWrittenAfterTheMainLoadInATransactionOfTheirOwn()
    {
        // Arrange: the source rows have Id and Points 1, 2, 3, so only row 1 fails the check Points > 1
        var warehouse = Guid.NewGuid();
        var connection = new RecordingConnection("warehouse", _events);

        // Act
        var outcome = await Pipeline(Configuration(), 3, connection, warehouse).Execute(Run(), TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.Where(e => e.StartsWith("begin", StringComparison.Ordinal) || e.StartsWith("exec", StringComparison.Ordinal) || e.StartsWith("commit", StringComparison.Ordinal))
            .ShouldBe(new[]
            {
                "begin:warehouse", "exec:warehouse:BulkInsert", "commit:warehouse",
                "begin:warehouse", "exec:warehouse:BulkInsert", "commit:warehouse",
            });
        var loaded = connection.Commands[0].ShouldBeOfType<BulkInsertCommand<IDictionary<string, object?>>>().Data.ToList();
        loaded.Select(r => r["Name"]).ShouldBe(new object?[] { "n2", "n3" });
        var rejected = connection.Commands[1].ShouldBeOfType<BulkInsertCommand<IDictionary<string, object?>>>().Data.ShouldHaveSingleItem();
        rejected["Name"].ShouldBe("n1");
        rejected[RejectedRecordColumns.Stage].ShouldBe("Clean");
        rejected[RejectedRecordColumns.Reason].ShouldBeOfType<string>().ShouldContain("AboveOne");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task WhenNoRecordIsRejectedTheRejectStepsAreNotRun()
    {
        // Arrange: every row passes Points > 0, since the points are 1, 2, 3
        var warehouse = Guid.NewGuid();
        var connection = new RecordingConnection("warehouse", _events);
        var configuration = Configuration();
        configuration.Transforms![0].Checks[0].Expression = "Points > 0";

        // Act
        var outcome = await Pipeline(configuration, 3, connection, warehouse).Execute(Run(), TestContext.Current.CancellationToken);

        // Assert: one transaction, the main load's
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.Count(e => e.StartsWith("begin", StringComparison.Ordinal)).ShouldBe(1);
        connection.Commands.Count.ShouldBe(1);
    }
}
