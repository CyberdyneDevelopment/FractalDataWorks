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
        var record = command.ShouldBeOfType<InsertCommand<EtlPipelineWatermarkRecord>>().Data;
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
        var condition = update.Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Id");
        condition.Value.ShouldBe(existing.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordFailsLoudSayingTheDataLoadedButTheWatermarkDidNotMove()
    {
        // Arrange
        WriteReturns(GenericResult<int>.Failure(new Fdw.Messages.GenericMessage("write refused")));

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
