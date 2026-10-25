using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// A gateway fake for pipeline tests: serves a source one skip/take page at a time, accepts bulk inserts
/// and truncates, and records what happened -- in order -- in a shared event list ("read@2", "load:2",
/// "truncate"). The order of those events is what the pipeline tests assert.
/// </summary>
internal sealed class PagedSourceGateway
{
    /// <summary>Initializes a new instance of the <see cref="PagedSourceGateway"/> class.</summary>
    /// <param name="source">The rows the source holds.</param>
    /// <param name="events">Receives one entry per gateway call, in call order.</param>
    /// <param name="failReads">Whether every read fails instead of returning a page.</param>
    /// <param name="onRead">Called with the requested skip after each read is recorded.</param>
    /// <param name="ignoresPaging">Whether the source answers its whole set whatever skip and take it is asked for, as a source that cannot page does.</param>
    public PagedSourceGateway(
        IReadOnlyList<Dictionary<string, object?>> source,
        List<string> events,
        bool failReads = false,
        Action<int>? onRead = null,
        bool ignoresPaging = false)
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
                onRead?.Invoke(skip);

                IGenericResult<IEnumerable<Dictionary<string, object?>>> page = failReads
                    ? GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(new GenericMessage("source unavailable"))
                    : GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
                        (ignoresPaging ? source : source.Skip(skip).Take(paging?.Take ?? int.MaxValue)).ToList());
                return Task.FromResult(page);
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
