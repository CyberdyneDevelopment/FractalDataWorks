using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.EtlGateway;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A source that reads the rows a script selects (FDW-1443): the text a data engineer wrote runs once on a data store, through
/// the ETL gateway, with its named parameters, and its rows leave in batches.
/// </summary>
/// <remarks>
/// <para>
/// The rows stream (FDW-4903): each batch leaves as soon as it is read, and writing it waits while the link it goes to is full,
/// so the run holds what its links buffer rather than the whole result. <c>maxRows</c> is stated, never assumed: the row after
/// it fails the run rather than loading a table, and the batches before it have already left.
/// </para>
/// <para>
/// A failure part way through (too many rows, or the store failing) fails this component, and the run stops and records the
/// failure: it is never a partial success. A load downstream has committed the batches it was given, and its committed rows are
/// on the run's record (<c>DestinationCommit</c>).
/// </para>
/// <para>
/// The gateway holds the caller to the DataEngineer role and refuses a store the host declares. It does not apply the
/// connection's limits (concurrency, rate, budget) to a script; FDW-4916 adds them.
/// </para>
/// </remarks>
public sealed class FlowScriptSource : IFlowComponent
{
    /// <summary>The name of the one output node.</summary>
    public const string OutputName = FlowSource.OutputName;

    private readonly IEtlGateway _gateway;
    private readonly string _dataStore;
    private readonly string _text;
    private readonly IReadOnlyList<ScriptParameter> _parameters;
    private readonly int _maxRows;
    private readonly int? _rowCap;
    private readonly int _batchSize;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowScriptSource"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="gateway">The ETL gateway the script runs through.</param>
    /// <param name="dataStore">The data store it runs on.</param>
    /// <param name="text">The query.</param>
    /// <param name="parameters">The values of its named parameters.</param>
    /// <param name="maxRows">The most rows it may select; at least one.</param>
    /// <param name="batchSize">How many rows go in a batch; at least one.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="rowCap">The most rows to keep, for a test run; none keeps every row the script selects (up to the limit it states).</param>
    public FlowScriptSource(
        string name,
        IEtlGateway gateway,
        string dataStore,
        string text,
        IReadOnlyList<ScriptParameter> parameters,
        int maxRows,
        int batchSize,
        ILogger? logger = null,
        int? rowCap = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        Name = name;
        _gateway = gateway;
        _dataStore = dataStore;
        _text = text;
        _parameters = parameters;
        _maxRows = maxRows;
        _batchSize = batchSize;
        _rowCap = rowCap;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [OutputName];

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ports);

        // Why one more than the most: the row after the most is what shows the script selects too many; this source fails on it
        // with its own message, and the read's own limit is the guard behind it.
        var opened = await _gateway
            .Execute(new ScriptStreamCommand(_text, _parameters, _maxRows + 1), _dataStore, cancellationToken)
            .ConfigureAwait(false);
        if (opened.IsFailure)
        {
            return FlowFailures.Caused(FlowScriptLog.ReadFailed(_logger, Name, _dataStore, FlowFailureReason.Of(opened, _logger, Name, _dataStore)), opened);
        }

        if (opened.Value is not { } source)
        {
            return GenericResult.Failure(FlowScriptLog.ReadReturnedNothing(_logger, Name, _dataStore));
        }

        await using (source.ConfigureAwait(false))
        {
            return await Send(source, ports.Output(OutputName), cancellationToken).ConfigureAwait(false);
        }
    }

    // Reads the rows and writes each batch as it fills. Why a test run stops at its cap rather than reading on: the first rows are
    // all it keeps, and stopping releases the reader.
    private async Task<IGenericResult> Send(IRecordSource<DataRecord> source, IFlowOutput output, CancellationToken cancellationToken)
    {
        var read = 0;
        var batch = new List<IDictionary<string, object?>>(_batchSize);
        await foreach (var record in source.Read(cancellationToken).ConfigureAwait(false))
        {
            if (record.IsFailure)
            {
                return FlowFailures.Caused(FlowScriptLog.ReadFailed(_logger, Name, _dataStore, FlowFailureReason.Of(record, _logger, Name, _dataStore)), record);
            }

            read++;
            if (read > _maxRows)
            {
                return GenericResult.Failure(FlowScriptLog.TooManyRows(_logger, Name, _dataStore, _maxRows));
            }

            batch.Add(Row(record.Value));
            if (batch.Count == _batchSize)
            {
                await output.Write(new FlowBatch(batch), cancellationToken).ConfigureAwait(false);
                batch = new List<IDictionary<string, object?>>(_batchSize);
            }

            if (_rowCap is { } cap && read >= cap)
            {
                break;
            }
        }

        if (batch.Count > 0)
        {
            await output.Write(new FlowBatch(batch), cancellationToken).ConfigureAwait(false);
        }

        return GenericResult.Success();
    }

    // A record as the row a batch carries: its columns by name, matched without regard to case.
    private static Dictionary<string, object?> Row(DataRecord record)
    {
        var row = new Dictionary<string, object?>(record.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < record.FieldCount; i++)
        {
            row[record.Schema.GetFieldName(i)] = record[i];
        }

        return row;
    }
}
