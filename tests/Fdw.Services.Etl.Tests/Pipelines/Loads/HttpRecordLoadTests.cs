using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Pipelines.Loads.LoadTestDoubles;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for loads that go to an HTTP endpoint (FDW-1672): the rows go to the connection's WriteRecords, a load that matches on keys
/// is refused for such a connection, and a load for any other connection is left to the unit of work.
/// </summary>
public sealed class HttpRecordLoadTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 9, 10, 11, TimeSpan.Zero);
    private static readonly IReadOnlyDictionary<string, object?> NoParameters = new Dictionary<string, object?>();

    private readonly Guid _endpoint = Guid.NewGuid();

    private static List<IDictionary<string, object?>> Rows(int count) =>
        [.. Enumerable.Range(0, count).Select(i => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Name"] = $"n{i}" })];

    private static PipelineLoadStepConfiguration Step(string kind, params string[] keys)
    {
        var step = new PipelineLoadStepConfiguration { Name = "load", StepOrder = 1, Kind = kind, DestinationDataSet = "Orders", IsEnabled = true };
        step.Columns.Add(new PipelineLoadColumnConfiguration { Name = "Name", Role = "Write", ColumnName = "Name", Ordinal = 0 });
        for (var ordinal = 0; ordinal < keys.Length; ordinal++)
        {
            step.Columns.Add(new PipelineLoadColumnConfiguration { Name = keys[ordinal], Role = "Key", ColumnName = keys[ordinal], Ordinal = ordinal });
        }

        return step;
    }

    private (PipelineLoadExecutor Executor, Mock<IHttpRecordWriterConnection> Writer) Executor(int answers)
    {
        var writer = new Mock<IHttpRecordWriterConnection>();
        writer
            .Setup(w => w.WriteRecords(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(answers));
        var combined = writer.As<IGenericConnection>();
        var executor = new PipelineLoadExecutor(
            new LoadTargetResolver(
                DataSetProviderOf(("Orders", [Source("Main", "dbo", "Orders")])).Object,
                DataStoreProviderOf(("Main", "dbo", "Orders", Container("Orders", _endpoint))).Object),
            ConnectionProviderOf((_endpoint, combined.Object)).Object,
            null,
            NullLogger.Instance);
        return (executor, writer);
    }

    private Task<IGenericResult<int>> Run(PipelineLoadExecutor executor, PipelineLoadStepConfiguration step, int rows) =>
        executor.Run("copy", [LoadUnit.ForStep(step)], Rows(rows), NoParameters, Now, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnInsertToAnHttpRecordWriterSendsTheRowsToItAndCountsWhatItTook()
    {
        var (executor, writer) = Executor(answers: 3);

        var result = await Run(executor, Step("Insert"), 3);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(3);
        writer.Verify(
            w => w.WriteRecords(It.IsAny<IDataContainer>(), It.Is<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(rows => rows.Count == 3), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpsertToAnHttpRecordWriterIsRefusedBecauseItCannotMatchOnKeys()
    {
        var (executor, writer) = Executor(answers: 3);

        var result = await Run(executor, Step("Upsert", "Name"), 3);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20226");
        writer.Verify(
            w => w.WriteRecords(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
