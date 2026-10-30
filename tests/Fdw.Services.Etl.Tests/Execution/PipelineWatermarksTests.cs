using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="PipelineWatermarks"/> (FDW-1109): the last-successful watermark is read and written through the
/// gateway in the operational store the host names, one row per pipeline per tenant.
/// </summary>
public sealed class PipelineWatermarksTests
{
    private static readonly Guid Pipeline = Guid.NewGuid();
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTimeOffset Started = new(2026, 9, 10, 11, 12, 13, TimeSpan.Zero);

    private readonly List<(IDataCommand Command, DataStoreTarget Target)> _calls = [];
    private readonly Mock<IDataGateway> _gateway = new();

    private void ReadReturns(IGenericResult<IEnumerable<EtlPipelineWatermarkRecord>> result) =>
        _gateway
            .Setup(g => g.Execute<IEnumerable<EtlPipelineWatermarkRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataStoreTarget target, CancellationToken token) =>
            {
                _calls.Add((command, target));
                return Task.FromResult(result);
            });

    private void WriteReturns(IGenericResult<int> result) =>
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataStoreTarget target, CancellationToken token) =>
            {
                _calls.Add((command, target));
                return Task.FromResult(result);
            });

    // The writes answer in turn: the first call gets the first result, the second the second, and so on.
    private void WritesReturn(params IGenericResult<int>[] results)
    {
        var next = new Queue<IGenericResult<int>>(results);
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataStoreTarget target, CancellationToken token) =>
            {
                _calls.Add((command, target));
                return Task.FromResult(next.Dequeue());
            });
    }

    private static IGenericResult<IEnumerable<EtlPipelineWatermarkRecord>> Rows(params EtlPipelineWatermarkRecord[] rows) =>
        GenericResult<IEnumerable<EtlPipelineWatermarkRecord>>.Success(rows);

    private static EtlPipelineWatermarkRecord Stored() => new()
    {
        Id = Guid.NewGuid(),
        PipelineId = Pipeline,
        TenantId = Tenant,
        LastSuccessfulRun = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        ExecutionId = Guid.NewGuid(),
    };

    // ---- Read

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ReadAsksTheOperationalStoreForTheRowOfThePipelineAndTenant()
    {
        // Arrange
        ReadReturns(Rows());

        // Act
        var result = await PipelineWatermarks.Read(_gateway.Object, "Ops", Pipeline, Tenant, "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
        var (command, target) = _calls.Single();
        target.ShouldBe(new DataStoreTarget("Ops", "etl", "PipelineWatermark"));
        var group = command.ShouldBeAssignableTo<IQueryCommand>()!.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Cast<FilterCondition>().Select(c => (c.PropertyName, c.Operator.Name, c.Value))
            .ShouldBe(new[] { ("PipelineId", "Equal", (object?)Pipeline), ("TenantId", "Equal", Tenant) });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ReadReturnsTheStoredRow()
    {
        // Arrange
        var stored = Stored();
        ReadReturns(Rows(stored));

        // Act
        var result = await PipelineWatermarks.Read(_gateway.Object, "Ops", Pipeline, Tenant, "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeSameAs(stored);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadFailsLoudWhenTheStoreCannotBeRead()
    {
        // Arrange
        ReadReturns(GenericResult<IEnumerable<EtlPipelineWatermarkRecord>>.Failure(new Fdw.Messages.GenericMessage("store down")));

        // Act
        var result = await PipelineWatermarks.Read(_gateway.Object, "Ops", Pipeline, Tenant, "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20232");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("store down");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadFailsLoudWhenTheHostNamesNoOperationalStore(string? store)
    {
        // Act
        var result = await PipelineWatermarks.Read(_gateway.Object, store, Pipeline, Tenant, "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20231");
        _calls.ShouldBeEmpty();
    }

    // ---- Record

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RecordInsertsTheRowOfAPipelineWithNoSuccessfulRunOnRecord()
    {
        // Arrange
        WriteReturns(GenericResult<int>.Success(1));
        var execution = Guid.NewGuid();

        // Act
        var result = await PipelineWatermarks.Record(
            _gateway.Object, "Ops", null, Pipeline, Tenant, Started, execution, "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var (command, target) = _calls.Single();
        target.ShouldBe(new DataStoreTarget("Ops", "etl", "PipelineWatermark"));
        var record = command.ShouldBeOfType<InsertCommand<EtlPipelineWatermarkRecord>>().Data.Single();
        record.Id.ShouldNotBe(Guid.Empty);
        record.PipelineId.ShouldBe(Pipeline);
        record.TenantId.ShouldBe(Tenant);
        record.LastSuccessfulRun.ShouldBe(Started);
        record.ExecutionId.ShouldBe(execution);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RecordMovesTheExistingRowForwardKeepingItsId()
    {
        // Arrange
        WriteReturns(GenericResult<int>.Success(1));
        var existing = Stored();
        var execution = Guid.NewGuid();

        // Act
        var result = await PipelineWatermarks.Record(
            _gateway.Object, "Ops", existing, Pipeline, Tenant, Started, execution, "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var update = _calls.Single().Command.ShouldBeOfType<UpdateCommand<EtlPipelineWatermarkRecord>>();
        update.Data.Id.ShouldBe(existing.Id);
        update.Data.LastSuccessfulRun.ShouldBe(Started);
        update.Data.ExecutionId.ShouldBe(execution);
        var group = update.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Cast<FilterCondition>().Select(c => (c.PropertyName, c.Operator.Name, c.Value))
            .ShouldBe(new[] { ("Id", "Equal", (object?)existing.Id), ("LastSuccessfulRun", "LessThan", Started) });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordLeavesAWatermarkThatIsAlreadyAheadOfTheRunWhereItIsAndWritesNothing()
    {
        // Arrange
        var existing = Stored();
        existing.LastSuccessfulRun = Started.AddMinutes(5);

        // Act
        var result = await PipelineWatermarks.Record(
            _gateway.Object, "Ops", existing, Pipeline, Tenant, Started, Guid.NewGuid(), "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _calls.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordUpdatesTheRowAnOverlappingRunInsertedFirstInsteadOfFailing()
    {
        // Arrange: the insert is refused, and the row is there when it is read again
        var inserted = Stored();
        inserted.LastSuccessfulRun = Started.AddMinutes(-5);
        WritesReturn(GenericResult<int>.Failure(new Fdw.Messages.GenericMessage("duplicate")), GenericResult<int>.Success(1));
        ReadReturns(Rows(inserted));
        var execution = Guid.NewGuid();

        // Act
        var result = await PipelineWatermarks.Record(
            _gateway.Object, "Ops", null, Pipeline, Tenant, Started, execution, "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _calls.Count.ShouldBe(3);
        _calls[0].Command.ShouldBeOfType<InsertCommand<EtlPipelineWatermarkRecord>>();
        _calls[1].Command.ShouldBeAssignableTo<IQueryCommand>();
        var update = _calls[2].Command.ShouldBeOfType<UpdateCommand<EtlPipelineWatermarkRecord>>();
        update.Data.Id.ShouldBe(inserted.Id);
        update.Data.LastSuccessfulRun.ShouldBe(Started);
        update.Data.ExecutionId.ShouldBe(execution);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordDoesNotMoveTheWatermarkBackWhenTheRowAnOverlappingRunInsertedIsLater()
    {
        // Arrange: the insert is refused, and the row read again is later than this run's start
        var inserted = Stored();
        inserted.LastSuccessfulRun = Started.AddMinutes(3);
        WritesReturn(GenericResult<int>.Failure(new Fdw.Messages.GenericMessage("duplicate")));
        ReadReturns(Rows(inserted));

        // Act
        var result = await PipelineWatermarks.Record(
            _gateway.Object, "Ops", null, Pipeline, Tenant, Started, Guid.NewGuid(), "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _calls.Count.ShouldBe(2);
        _calls[0].Command.ShouldBeOfType<InsertCommand<EtlPipelineWatermarkRecord>>();
        _calls[1].Command.ShouldBeAssignableTo<IQueryCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordFailsLoudSayingTheDataLoadedButTheWatermarkDidNotMove()
    {
        // Arrange: the insert is refused and the row is still not there when it is read again, so the failure is real
        WriteReturns(GenericResult<int>.Failure(new Fdw.Messages.GenericMessage("write refused")));
        ReadReturns(Rows());

        // Act
        var result = await PipelineWatermarks.Record(
            _gateway.Object, "Ops", null, Pipeline, Tenant, Started, Guid.NewGuid(), "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20233");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("loaded its data");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("write refused");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordFailsLoudWithTheUpdatesReasonWhenTheConditionalUpdateIsRefused()
    {
        // Arrange
        WriteReturns(GenericResult<int>.Failure(new Fdw.Messages.GenericMessage("update refused")));

        // Act
        var result = await PipelineWatermarks.Record(
            _gateway.Object, "Ops", Stored(), Pipeline, Tenant, Started, Guid.NewGuid(), "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20233");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("update refused");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordFailsLoudWhenTheHostNamesNoOperationalStore()
    {
        // Act
        var result = await PipelineWatermarks.Record(
            _gateway.Object, null, null, Pipeline, Tenant, Started, Guid.NewGuid(), "copy", NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20231");
        _calls.ShouldBeEmpty();
    }
}
