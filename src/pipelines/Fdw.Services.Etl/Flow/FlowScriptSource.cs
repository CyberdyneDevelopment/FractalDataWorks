using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
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
/// The script is read whole and then batched, so <c>maxRows</c> is stated, never assumed: a script that selects more fails the
/// run rather than loading a table. The gateway holds the caller to the DataEngineer role and to the store's limits.
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

    // One more than the most the script may select, so too many rows is seen; a test run asks for the cap and keeps the first rows.
    private int ReadLimit() => _rowCap is { } cap ? Math.Min(cap, _maxRows + 1) : _maxRows + 1;

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [OutputName];

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ports);
        var read = await _gateway
            .Execute(new ScriptReadCommand(_text, _parameters, ReadLimit()), _dataStore, cancellationToken)
            .ConfigureAwait(false);
        if (read.IsFailure)
        {
            return GenericResult.Failure(FlowScriptLog.ReadFailed(_logger, Name, _dataStore, FlowFailureReason.Of(read, _logger, Name, _dataStore)));
        }

        if (read.Value is not { } rows)
        {
            return GenericResult.Failure(FlowScriptLog.ReadReturnedNothing(_logger, Name, _dataStore));
        }

        // Why always: a test run cap at or below the limit can never trip this, and one above it must still refuse a script that selects too much.
        if (rows.Count > _maxRows)
        {
            return GenericResult.Failure(FlowScriptLog.TooManyRows(_logger, Name, _dataStore, _maxRows));
        }

        var output = ports.Output(OutputName);
        var total = _rowCap is { } cap ? Math.Min(rows.Count, cap) : rows.Count;
        for (var start = 0; start < total; start += _batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = new List<IDictionary<string, object?>>(Math.Min(_batchSize, total - start));
            for (var i = start; i < Math.Min(start + _batchSize, total); i++)
            {
                batch.Add(rows[i]);
            }

            await output.Write(new FlowBatch(batch), cancellationToken).ConfigureAwait(false);
        }

        return GenericResult.Success();
    }
}
