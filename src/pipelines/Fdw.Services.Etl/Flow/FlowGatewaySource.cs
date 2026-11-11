using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A source component that reads a data set or one container of a data store through the data gateway as one stream in a stable order,
/// and sends its rows out in batches (FDW-1386, FDW-4911). It has no input node.
/// </summary>
/// <remarks>
/// It opens its rows with the gateway's <c>OpenRecordSource</c>, run as the run's principal, so row-level security and the refusal of
/// host-declared stores apply unchanged. The gateway reads them in a stable order: through the connection's cursor, or in keyset pages by
/// the declared key; a source that can be read neither way is refused when it opens, and the refusal names it. Rows are never read in
/// pages of no stated order, which could repeat or skip a row between pages. A read that fails ends the run. It never names a connection type.
/// </remarks>
public sealed class FlowGatewaySource : IFlowComponent
{
    /// <summary>The name of the source's one output node.</summary>
    public const string OutputName = FlowSource.OutputName;

    private readonly string _name;
    private readonly int _pageSize;
    private readonly int? _rowCap;
    private readonly ILogger _logger;
    private readonly IReadOnlyList<Target> _targets;
    private readonly IDataGateway _gateway;

    private FlowGatewaySource(string name, IDataGateway gateway, int pageSize, int? rowCap, IReadOnlyList<Target> targets, ILogger? logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        if (targets.Count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(targets), targets.Count, "A source reads at least one target.");
        }

        _name = name;
        _gateway = gateway;
        _pageSize = pageSize;
        _rowCap = rowCap;
        _targets = targets;
        _logger = logger ?? NullLogger.Instance;
    }

    // One thing to read: the container a single source reads (File is null), or one file of a list of files (FDW-3089).
    private sealed record Target(
        string? File, Func<IDataGateway, IDataCommand, CancellationToken, Task<IGenericResult<IRecordSource<DataRecord>>>> Open);

    // The first file of a list and its fields, which every later file must have.
    private sealed record FirstFile(string Name, HashSet<string> Fields);

    // What the stream has done so far: how many rows went out, the first file once there is one, and the target whose fields were last checked.
    private sealed class StreamState
    {
        public int Written { get; set; }

        public FirstFile? First { get; set; }

        public Target? Checked { get; set; }
    }

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [OutputName];

    /// <summary>Makes a source that reads a data set.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="gateway">The data gateway.</param>
    /// <param name="dataSet">The data set to read.</param>
    /// <param name="pageSize">How many rows go in a batch; at least one. The gateway's own read is not cut by it.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="rowCap">The most rows to read, for a test run; none reads every row.</param>
    /// <returns>The source.</returns>
    public static FlowGatewaySource ForDataSet(string name, IDataGateway gateway, string dataSet, int pageSize, ILogger? logger = null, int? rowCap = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataSet);
        var target = new DataSetTarget(dataSet);
        return new FlowGatewaySource(
            name, gateway, pageSize, rowCap,
            [new Target(null, (g, command, token) => g.OpenRecordSource(command, target, token))],
            logger);
    }

    /// <summary>Makes a source that reads one container of a data store.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="gateway">The data gateway.</param>
    /// <param name="dataStore">The data store.</param>
    /// <param name="path">The path in the store, or <see langword="null"/> when the store states none.</param>
    /// <param name="container">The container to read.</param>
    /// <param name="pageSize">How many rows go in a batch; at least one. The gateway's own read is not cut by it.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="rowCap">The most rows to read, for a test run; none reads every row.</param>
    /// <param name="physicalPath">The path to read in place of the declared container's (FDW-1952); needs the ETL data gateway.</param>
    /// <param name="physicalName">The name to read in place of the declared container's (FDW-1952); needs the ETL data gateway.</param>
    /// <returns>The source.</returns>
    public static FlowGatewaySource ForContainer(
        string name, IDataGateway gateway, string dataStore, string? path, string container, int pageSize, ILogger? logger = null, int? rowCap = null,
        string? physicalPath = null, string? physicalName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(container);
        var target = new DataStoreTarget(dataStore, path, container);
        if (string.IsNullOrWhiteSpace(physicalPath) && string.IsNullOrWhiteSpace(physicalName))
        {
            return new FlowGatewaySource(
                name, gateway, pageSize, rowCap,
                [new Target(null, (g, command, token) => g.OpenRecordSource(command, target, token))],
                logger);
        }

        if (gateway is not IEtlDataGateway)
        {
            throw new ArgumentException("A physical path or name needs the ETL data gateway; the gateway given does not read a declared container at another address.", nameof(gateway));
        }

        return new FlowGatewaySource(
            name, gateway, pageSize, rowCap,
            [new Target(null, (g, command, token) => ((IEtlDataGateway)g).OpenRecordSource(command, target, physicalPath, physicalName, token))],
            logger);
    }

    /// <summary>
    /// Makes a source that reads a list of files of one container of a data store, one file after another, as one stream (FDW-3089): the rows of every
    /// file go out on its one output, in the order of the list, with no more held than a batch. Every file must have the fields of the first.
    /// </summary>
    /// <param name="name">The component's name.</param>
    /// <param name="gateway">The data gateway; it must be the ETL data gateway, which reads a declared container at another address.</param>
    /// <param name="dataStore">The data store.</param>
    /// <param name="path">The path in the store, or <see langword="null"/> when the store states none.</param>
    /// <param name="container">The declared container each file is read as.</param>
    /// <param name="pageSize">How many rows go in a batch; at least one. The gateway's own read is not cut by it.</param>
    /// <param name="files">The names of the files to read, in order; at least one.</param>
    /// <param name="physicalPath">The path to read the files at in place of the declared container's, or <see langword="null"/> to read the declared path.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="rowCap">The most rows to read from the whole stream, for a test run; none reads every row.</param>
    /// <returns>The source.</returns>
    public static FlowGatewaySource ForContainerFiles(
        string name, IDataGateway gateway, string dataStore, string? path, string container, int pageSize, IReadOnlyList<string> files, string? physicalPath = null,
        ILogger? logger = null, int? rowCap = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(container);
        ArgumentNullException.ThrowIfNull(files);
        if (gateway is not IEtlDataGateway)
        {
            throw new ArgumentException("A list of files needs the ETL data gateway; the gateway given does not read a declared container at another address.", nameof(gateway));
        }

        var target = new DataStoreTarget(dataStore, path, container);
        return new FlowGatewaySource(
            name, gateway, pageSize, rowCap,
            [.. files.Select(file => new Target(
                file, (g, command, token) => ((IEtlDataGateway)g).OpenRecordSource(command, target, physicalPath, file, token)))],
            logger);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var output = ports.Output(OutputName);
        var state = new StreamState();
        foreach (var target in _targets)
        {
            // Why the cap is checked before a file: a test run reads the first rows of the whole stream, and a file after the cap is not opened.
            if (_rowCap is { } cap && state.Written >= cap)
            {
                break;
            }

            var read = await ReadTarget(target, output, state, cancellationToken).ConfigureAwait(false);
            if (read.IsFailure)
            {
                return read;
            }
        }

        return GenericResult.Success();
    }

    // Opens one target and streams it. The gateway's record source owns what it reads through, so it is disposed whatever happens; a test
    // run's row cap ends the read early, and disposing it then stops the store's read.
    private async Task<IGenericResult> ReadTarget(Target target, IFlowOutput output, StreamState state, CancellationToken cancellationToken)
    {
        var opened = await target.Open(_gateway, new QueryCommand<Dictionary<string, object?>>(), cancellationToken).ConfigureAwait(false);
        if (opened.IsFailure || opened.Value is not { } source)
        {
            return FlowFailures.Caused(FlowLog.SourceOpenFailed(_logger, _name, FlowFailures.ReasonOf(opened)), opened);
        }

        await using (source.ConfigureAwait(false))
        {
            return await Stream(target, source, output, state, cancellationToken).ConfigureAwait(false);
        }
    }

    // Sends the records out in batches of the batch size; the last batch holds what is left. Only a batch is held at a time.
    private async Task<IGenericResult> Stream(Target target, IRecordSource<DataRecord> source, IFlowOutput output, StreamState state, CancellationToken cancellationToken)
    {
        var names = source.Schema.Fields.Select(field => field.Name).ToArray();
        var batch = new List<IDictionary<string, object?>>();
        await foreach (var record in source.Read(cancellationToken).ConfigureAwait(false))
        {
            if (record.IsFailure)
            {
                return FlowFailures.Caused(FlowLog.SourceRecordFailed(_logger, _name, state.Written + batch.Count, FlowFailures.ReasonOf(record)), record);
            }

            var fields = FieldsAtFirstRecord(target, names, state);
            if (fields.IsFailure)
            {
                return fields;
            }

            batch.Add(Row(names, record.Value));
            var capped = _rowCap is { } cap && state.Written + batch.Count >= cap;
            if (batch.Count < _pageSize && !capped)
            {
                continue;
            }

            await output.Write(new FlowBatch(batch), cancellationToken).ConfigureAwait(false);
            state.Written += batch.Count;
            batch = [];
            if (capped)
            {
                return GenericResult.Success();
            }
        }

        if (batch.Count > 0)
        {
            await output.Write(new FlowBatch(batch), cancellationToken).ConfigureAwait(false);
            state.Written += batch.Count;
        }

        return GenericResult.Success();
    }

    // A file's fields are checked at its first record: a file with no rows has no fields to compare, as before, and a file whose fields differ
    // fails before a row of it goes out. A source that reads no list of files has nothing to compare.
    private IGenericResult FieldsAtFirstRecord(Target target, string[] names, StreamState state)
    {
        if (target.File is not { } file || ReferenceEquals(state.Checked, target))
        {
            return GenericResult.Success();
        }

        state.Checked = target;
        return SameFields(file, names, state);
    }

    // A record as the row a component takes: its fields by name, matched without regard to case as a store's rows are.
    private static Dictionary<string, object?> Row(string[] names, DataRecord record)
    {
        var row = new Dictionary<string, object?>(names.Length, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < names.Length; i++)
        {
            row[names[i]] = record[i];
        }

        return row;
    }

    // A file's fields are the fields its read carries. The first file sets the fields every later file must have; a file that differs fails the
    // run naming the file and the fields that differ, and no file is skipped (FDW-3089).
    private IGenericResult SameFields(string file, string[] fields, StreamState state)
    {
        if (state.First is not { } first)
        {
            state.First = new FirstFile(file, new HashSet<string>(fields, StringComparer.Ordinal));
            return GenericResult.Success();
        }

        if (first.Fields.SetEquals(fields))
        {
            return GenericResult.Success();
        }

        var missing = first.Fields.Except(fields, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var extra = fields.Except(first.Fields, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var difference = string.Concat(
            missing.Count > 0 ? $"missing {string.Join(", ", missing)}" : string.Empty,
            missing.Count > 0 && extra.Count > 0 ? "; " : string.Empty,
            extra.Count > 0 ? $"extra {string.Join(", ", extra)}" : string.Empty);
        return GenericResult.Failure(FlowLog.SourceFilesDiffer(_logger, _name, file, first.Name, difference));
    }
}
