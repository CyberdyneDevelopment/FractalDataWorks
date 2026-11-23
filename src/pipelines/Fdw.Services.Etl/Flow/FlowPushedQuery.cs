using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The component a planner puts in place of a sub-graph of joins and lookups whose sources all read one store (FDW-6969): it runs one query in
/// that store through the data gateway and sends its rows out where the sub-graph's top component sent them.
/// </summary>
/// <remarks>
/// <para>
/// It has the top component's name and outputs, so every line downstream is unchanged. The query states the order the in-memory components would
/// have sent their rows in (each source's key, in join order); the rows a join's left side has no row for are held and sent last, as the in-memory
/// join sends them, so where a store sorts nulls does not matter.
/// </para>
/// <para>
/// A row whose stated side is absent (<see cref="FlowPushedAbsence"/>) loses the fields that side brought: held and sent last (the right rows of a
/// full join), sent to the lookup's unmatched output, or, with none, the run fails as the lookup fails. A read that cannot open or a record that
/// fails ends the run, naming the component. It never names a connection type.
/// </para>
/// </remarks>
public sealed class FlowPushedQuery : IFlowComponent
{
    private readonly IDataGateway _gateway;
    private readonly DataStoreTarget _target;
    private readonly IDataCommand _query;
    private readonly string _output;
    private readonly FlowPushedAbsence? _absence;
    private readonly int _batchSize;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowPushedQuery"/> class.</summary>
    /// <param name="name">The name of the component it stands in for.</param>
    /// <param name="gateway">The data gateway the read arrived on.</param>
    /// <param name="target">The primary container the query reads, in its store.</param>
    /// <param name="query">The query: its joins, projection and order.</param>
    /// <param name="output">The output the matched rows leave by.</param>
    /// <param name="absence">What happens to a row whose stated side is absent; null when the query has no such side.</param>
    /// <param name="batchSize">How many rows go in an output batch; at least one.</param>
    /// <param name="logger">The logger.</param>
    public FlowPushedQuery(
        string name, IDataGateway gateway, DataStoreTarget target, IDataCommand query, string output, FlowPushedAbsence? absence, int batchSize, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(output);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        Name = name;
        _gateway = gateway;
        _target = target;
        _query = query;
        _output = output;
        _absence = absence;
        _batchSize = batchSize;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => _absence?.Output is { } unmatched ? [_output, unmatched] : [_output];

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var opened = await _gateway.OpenRecordSource(_query, _target, cancellationToken).ConfigureAwait(false);
        if (opened.IsFailure || opened.Value is not { } source)
        {
            return FlowFailures.Caused(FlowLog.SourceOpenFailed(_logger, Name, FlowFailures.ReasonOf(opened)), opened);
        }

        var held = FlowHeld.Of(ports);
        var main = new FlowBatchWriter(ports.Output(_output), _batchSize, cancellationToken);
        var unmatched = _absence?.Output is { } other ? new FlowBatchWriter(ports.Output(other), _batchSize, cancellationToken) : null;
        var last = new List<IDictionary<string, object?>>();
        try
        {
            await using (source.ConfigureAwait(false))
            {
                var read = await Read(source, main, unmatched, last, cancellationToken).ConfigureAwait(false);
                if (read.IsFailure)
                {
                    return read;
                }
            }

            return await Finish(main, unmatched, last, held).ConfigureAwait(false);
        }
        finally
        {
            held?.ReleaseAll();
        }
    }

    private async Task<IGenericResult> Read(
        IRecordSource<DataRecord> source, FlowBatchWriter main, FlowBatchWriter? unmatched, List<IDictionary<string, object?>> last, CancellationToken cancellationToken)
    {
        var names = source.Schema.Fields.Select(field => field.Name).ToArray();
        var rows = 0;
        await foreach (var record in source.Read(cancellationToken).ConfigureAwait(false))
        {
            if (record.IsFailure)
            {
                return FlowFailures.Caused(FlowLog.SourceRecordFailed(_logger, Name, rows, FlowFailures.ReasonOf(record)), record);
            }

            rows++;
            var sent = await Send(Row(names, record.Value), main, unmatched, last).ConfigureAwait(false);
            if (sent.IsFailure)
            {
                return sent;
            }
        }

        return GenericResult.Success();
    }

    // A row with its stated side present goes out on the main output; one without it loses that side's fields and is held, sent to the unmatched
    // output, or fails the run, as the component it stands in for would.
    private async Task<IGenericResult> Send(
        Dictionary<string, object?> row, FlowBatchWriter main, FlowBatchWriter? unmatched, List<IDictionary<string, object?>> last)
    {
        if (_absence is not { } absence)
        {
            await main.Add(row).ConfigureAwait(false);
            return GenericResult.Success();
        }

        var present = row.TryGetValue(absence.PresenceField, out var marker) && marker is not null;
        row.Remove(absence.PresenceField);
        if (present)
        {
            await main.Add(row).ConfigureAwait(false);
            return GenericResult.Success();
        }

        foreach (var field in absence.Dropped)
        {
            row.Remove(field);
        }

        if (absence.SentLast)
        {
            last.Add(row);
            return GenericResult.Success();
        }

        if (unmatched is not null)
        {
            await unmatched.Add(row).ConfigureAwait(false);
            return GenericResult.Success();
        }

        return GenericResult.Failure(FlowLog.LookupNoMatch(_logger, Name, string.Join(", ", absence.Keys.Select(key => row.TryGetValue(key, out var value) ? value : null))));
    }

    // The held rows go last, as the in-memory join sends the rows its left side had none for after every left row.
    private static async Task<IGenericResult> Finish(
        FlowBatchWriter main, FlowBatchWriter? unmatched, List<IDictionary<string, object?>> last, FlowHeldGauge? held)
    {
        if (last.Count > 0)
        {
            held?.Hold(new FlowBatch(last));
        }

        foreach (var row in last)
        {
            await main.Add(row).ConfigureAwait(false);
        }

        if (unmatched is not null)
        {
            var flushed = await unmatched.Flush().ConfigureAwait(false);
            if (flushed.IsFailure)
            {
                return flushed;
            }
        }

        return await main.Flush().ConfigureAwait(false);
    }

    // A record as the row a component takes: its fields by name, matched without regard to case as a store's rows are.
    private static Dictionary<string, object?> Row(string[] names, DataRecord record)
    {
        var row = new Dictionary<string, object?>(names.Length, StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < names.Length; index++)
        {
            row[names[index]] = record[index];
        }

        return row;
    }
}
