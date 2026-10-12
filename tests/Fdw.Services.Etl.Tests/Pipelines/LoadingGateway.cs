using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// A gateway fake for the load tests (FDW-1109): serves a source a page at a time, accepts the pipeline's inserts, upserts
/// and truncates by data set, and serves and records the watermark row by data store. It records what happened, in order,
/// in a shared event list ("read@2", "insert:2", "upsert:2", "truncate", "watermark:read", "watermark:insert").
/// </summary>
internal sealed class LoadingGateway
{
    /// <summary>Initializes a new instance of the <see cref="LoadingGateway"/> class.</summary>
    /// <param name="source">The rows the source holds.</param>
    /// <param name="events">Receives one entry per gateway call, in call order.</param>
    /// <param name="watermarks">The watermark rows the operational store holds.</param>
    /// <param name="failLoads">Whether every insert and upsert fails.</param>
    /// <param name="failWatermarkWrite">Whether writing the watermark fails.</param>
    public LoadingGateway(
        IReadOnlyList<Dictionary<string, object?>> source,
        List<string> events,
        IReadOnlyList<EtlPipelineWatermarkRecord>? watermarks = null,
        bool failLoads = false,
        bool failWatermarkWrite = false)
    {
        var gateway = new Mock<IDataGateway>();

        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataSetTarget target, CancellationToken cancellationToken) =>
            {
                var paging = ((IQueryCommand)command).Paging;
                var skip = paging is null ? 0 : paging.Skip;
                events.Add($"read@{skip}");
                IGenericResult<IEnumerable<Dictionary<string, object?>>> page =
                    GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(source.Skip(skip).Take(paging?.Take ?? int.MaxValue).ToList());
                return Task.FromResult(page);
            });

        gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataSetTarget target, CancellationToken cancellationToken) =>
            {
                var (label, count) = command switch
                {
                    BulkInsertCommand<IDictionary<string, object?>> insert => ("insert", insert.Data.Count()),
                    BulkUpsertCommand<IDictionary<string, object?>> upsert => ("upsert", upsert.Data.Count()),
                    _ => ("truncate", 0),
                };
                events.Add(label == "truncate" ? "truncate" : $"{label}:{count}");
                Loads.Add(command);
                return Task.FromResult(failLoads && label != "truncate"
                    ? GenericResult<int>.Failure(new GenericMessage("load refused"))
                    : GenericResult<int>.Success(count));
            });

        gateway
            .Setup(g => g.Execute<IEnumerable<EtlPipelineWatermarkRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken) =>
            {
                events.Add("watermark:read");
                IGenericResult<IEnumerable<EtlPipelineWatermarkRecord>> rows =
                    GenericResult<IEnumerable<EtlPipelineWatermarkRecord>>.Success(watermarks is null ? new List<EtlPipelineWatermarkRecord>() : watermarks.ToList());
                return Task.FromResult(rows);
            });

        gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken) =>
            {
                var record = command switch
                {
                    InsertCommand<EtlPipelineWatermarkRecord> insert => insert.Data,
                    UpdateCommand<EtlPipelineWatermarkRecord> update => update.Data,
                    _ => throw new InvalidOperationException($"unexpected store command {command.GetType().Name}"),
                };
                events.Add(command is InsertCommand<EtlPipelineWatermarkRecord> ? "watermark:insert" : "watermark:update");
                WatermarkWrites.Add(record);
                return Task.FromResult(failWatermarkWrite
                    ? GenericResult<int>.Failure(new GenericMessage("watermark refused"))
                    : GenericResult<int>.Success(1));
            });

        var provider = new Mock<IDataGatewayProvider>();
        provider.Setup(p => p.ByName("Main")).Returns(gateway.Object);
        Provider = provider.Object;
    }

    /// <summary>Gets the provider that hands the fake gateway to a pipeline as "Main".</summary>
    public IDataGatewayProvider Provider { get; }

    /// <summary>Gets the load commands the gateway was given, in order.</summary>
    public List<IDataCommand> Loads { get; } = [];

    /// <summary>Gets the watermark rows written, in order.</summary>
    public List<EtlPipelineWatermarkRecord> WatermarkWrites { get; } = [];

    /// <summary>Builds <paramref name="count"/> rows with an id, a name and points that equal the id.</summary>
    /// <param name="count">How many rows.</param>
    /// <returns>The rows.</returns>
    public static List<Dictionary<string, object?>> Rows(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Dictionary<string, object?> { ["Id"] = i, ["Name"] = $"n{i}", ["Points"] = i })
            .ToList();
}
