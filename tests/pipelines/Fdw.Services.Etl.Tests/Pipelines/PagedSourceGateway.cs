using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// A gateway fake for pipeline tests: serves a source as one stable read (FDW-4911), accepts bulk inserts and truncates, and records what
/// happened -- in order -- in a shared event list ("open", "closed after 5", "load:2", "truncate"). The order of those events is what the
/// pipeline tests assert.
/// </summary>
internal sealed class PagedSourceGateway
{
    /// <summary>Initializes a new instance of the <see cref="PagedSourceGateway"/> class.</summary>
    /// <param name="source">The rows the source holds.</param>
    /// <param name="events">Receives one entry per gateway call, in call order.</param>
    /// <param name="failReads">Whether every open fails instead of returning the rows.</param>
    public PagedSourceGateway(
        IReadOnlyList<Dictionary<string, object?>> source,
        List<string> events,
        bool failReads = false)
    {
        var gateway = new Mock<IDataGateway>();

        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataSetTarget target, CancellationToken cancellationToken) =>
            {
                events.Add("open");
                IGenericResult<IRecordSource<DataRecord>> opened = failReads
                    ? GenericResult<IRecordSource<DataRecord>>.Failure(new GenericMessage("source unavailable"))
                    : GenericResult<IRecordSource<DataRecord>>.Success(new CountingRecordSource([.. source], events));
                return Task.FromResult(opened);
            });

        gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataSetTarget target, CancellationToken cancellationToken) =>
            {
                if (command is InsertCommand<IDictionary<string, object?>> bulk)
                {
                    var count = bulk.Data.Count();
                    events.Add($"load:{count}");
                    return Task.FromResult(GenericResult<int>.Success(count));
                }

                events.Add("truncate");
                return Task.FromResult(GenericResult<int>.Success(0));
            });

        var provider = new Mock<IDataGatewayProvider>();
        provider.Setup(p => p.ByName("Main")).Returns(gateway.Object);
        Provider = provider.Object;
    }

    /// <summary>Gets the provider that hands the fake gateway to a pipeline as "Main".</summary>
    public IDataGatewayProvider Provider { get; }

    /// <summary>Builds <paramref name="count"/> rows, each carrying its own index as <c>Id</c>.</summary>
    /// <param name="count">How many rows.</param>
    /// <returns>The rows.</returns>
    public static List<Dictionary<string, object?>> Rows(int count)
        => Enumerable.Range(0, count)
            .Select(i => new Dictionary<string, object?> { ["Id"] = i })
            .ToList();
}
