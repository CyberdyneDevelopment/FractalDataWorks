using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Finds the loads that go to an HTTP endpoint and sends their rows there (FDW-1672).
/// </summary>
/// <remarks>
/// A connection that writes records takes rows, not commands, so such a load is not planned as a command in a unit of work: the rows
/// go to the connection's <c>WriteRecords</c> for the container. A load that matches rows on keys is refused for such a connection,
/// because an HTTP record writer sends rows and cannot match them. Every other load is left to the unit of work.
/// </remarks>
internal sealed class HttpRecordLoads
{
    private readonly LoadTargetResolver _resolver;
    private readonly IConnectionProvider? _connections;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="HttpRecordLoads"/> class.</summary>
    /// <param name="resolver">Resolves each load's destination.</param>
    /// <param name="connections">The connection provider; none means no load is taken for an HTTP endpoint.</param>
    /// <param name="logger">The logger.</param>
    internal HttpRecordLoads(LoadTargetResolver resolver, IConnectionProvider? connections, ILogger logger)
    {
        _resolver = resolver;
        _connections = connections;
        _logger = logger;
    }

    /// <summary>Finds which of the loads go to an HTTP endpoint.</summary>
    /// <param name="pipelineName">The pipeline, for messages.</param>
    /// <param name="units">The loads, in the order they run.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The loads for the unit of work and the loads for an HTTP endpoint, or the refusal of a keyed load aimed at an endpoint.</returns>
    internal async Task<IGenericResult<LoadSplit>> Split(
        string pipelineName, IReadOnlyList<LoadUnit> units, CancellationToken cancellationToken)
    {
        var database = new List<LoadUnit>();
        var http = new List<HttpRecordLoad>();
        foreach (var unit in units)
        {
            var found = await Find(unit, cancellationToken).ConfigureAwait(false);
            if (found.IsFailure)
            {
                return found.ToNewResult<LoadSplit>();
            }

            if (found.Value is not { } load)
            {
                database.Add(unit);
            }
            else if (unit.Kind.UsesKeyColumns)
            {
                return GenericResult<LoadSplit>.Failure(PipelineRunLog.LoadModeNotForDestination(
                    _logger, pipelineName, unit.Kind.Name, "an HTTP record writer sends rows and cannot match them on keys"));
            }
            else
            {
                http.Add(load);
            }
        }

        return GenericResult<LoadSplit>.Success(new LoadSplit(database, http));
    }

    /// <summary>Sends the rows of each load to its endpoint.</summary>
    /// <param name="pipelineName">The pipeline, for messages.</param>
    /// <param name="loads">The loads that go to an HTTP endpoint.</param>
    /// <param name="rows">The rows to send.</param>
    /// <param name="cancellationToken">Cancels the writes.</param>
    /// <returns>How many rows the endpoints took, or the first failure.</returns>
    internal async Task<IGenericResult<int>> Write(
        string pipelineName, IReadOnlyList<HttpRecordLoad> loads, IReadOnlyList<IDictionary<string, object?>> rows, CancellationToken cancellationToken)
    {
        IReadOnlyList<IReadOnlyDictionary<string, object?>> sent = [.. rows.Select(row => (IReadOnlyDictionary<string, object?>)new ReadOnlyDictionary<string, object?>(row))];
        var written = 0;
        foreach (var load in loads)
        {
            EtlLog.LoadingViaHttpRecordWriter(_logger, pipelineName, sent.Count, load.Description);
            var result = await load.Writer.WriteRecords(load.Container, sent, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                return GenericResult<int>.Failure(EtlLog.LoadViaHttpRecordWriterFailed(_logger, pipelineName, load.Description, LoadFailureText.Of(result)));
            }

            EtlLog.LoadViaHttpRecordWriterCompleted(_logger, pipelineName, result.Value, load.Description);
            written += result.Value;
        }

        return GenericResult<int>.Success(written);
    }

    // The load as an HTTP load, or none when it is not a load of rows into a container reached over an HTTP record writer.
    private async Task<IGenericResult<HttpRecordLoad?>> Find(LoadUnit unit, CancellationToken cancellationToken)
    {
        if (_connections is null || !unit.Kind.UsesRows || unit.Kind.UsesSourceDataSet)
        {
            return GenericResult<HttpRecordLoad?>.Success(null);
        }

        var target = unit.DataSet is not null
            ? await _resolver.ForDataSet(unit.Owner, unit.DataSet, cancellationToken).ConfigureAwait(false)
            : await _resolver.ForContainer(unit.Owner, unit.DataStore!, unit.Path!, unit.Container!, cancellationToken).ConfigureAwait(false);
        if (target.IsFailure || target.Value is null)
        {
            return target.ToNewResult<HttpRecordLoad?>();
        }

        var connection = await _connections.Get(target.Value.ConnectionId, cancellationToken).ConfigureAwait(false);
        if (connection.IsFailure)
        {
            return connection.ToNewResult<HttpRecordLoad?>();
        }

        return GenericResult<HttpRecordLoad?>.Success(
            connection.Value is IHttpRecordWriterConnection writer ? new HttpRecordLoad(unit, writer, target.Value.Container, target.Value.Description) : null);
    }
}
