using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A source component that reads a data set or one container of a data store through the data gateway, a page at a time, and
/// sends each page out as a batch (FDW-1386). It has no input node.
/// </summary>
/// <remarks>
/// It reads exactly as a pipeline's extract does: a query command with a skip and a take, run as the run's principal, so row-level
/// security and the refusal of host-declared stores apply unchanged. A read that fails ends the run; an empty page ends the
/// source. It never names a connection type.
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
        string? File, Func<IDataGateway, QueryCommand<Dictionary<string, object?>>, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> Read);

    // The first file of a list and its fields, which every later file must have.
    private sealed record FirstFile(string Name, HashSet<string> Fields);

    // What the stream has done so far: how many rows went out, and the first file once there is one.
    private sealed class StreamState
    {
        public int Written { get; set; }

        public FirstFile? First { get; set; }
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
    /// <param name="pageSize">How many rows go in a batch; at least one.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="rowCap">The most rows to read, for a test run; none reads every row.</param>
    /// <returns>The source.</returns>
    public static FlowGatewaySource ForDataSet(string name, IDataGateway gateway, string dataSet, int pageSize, ILogger? logger = null, int? rowCap = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataSet);
        var target = new DataSetTarget(dataSet);
        return new FlowGatewaySource(
            name, gateway, pageSize, rowCap,
            [new Target(null, (g, command, token) => g.Execute<IEnumerable<Dictionary<string, object?>>>(command, target, token))],
            logger);
    }

    /// <summary>Makes a source that reads one container of a data store.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="gateway">The data gateway.</param>
    /// <param name="dataStore">The data store.</param>
    /// <param name="path">The path in the store, or <see langword="null"/> when the store states none.</param>
    /// <param name="container">The container to read.</param>
    /// <param name="pageSize">How many rows go in a batch; at least one.</param>
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
                [new Target(null, (g, command, token) => g.Execute<IEnumerable<Dictionary<string, object?>>>(command, target, token))],
                logger);
        }

        if (gateway is not IEtlDataGateway)
        {
            throw new ArgumentException("A physical path or name needs the ETL data gateway; the gateway given does not read a declared container at another address.", nameof(gateway));
        }

        return new FlowGatewaySource(
            name, gateway, pageSize, rowCap,
            [new Target(null, (g, command, token) => ((IEtlDataGateway)g).Execute<IEnumerable<Dictionary<string, object?>>>(command, target, physicalPath, physicalName, token))],
            logger);
    }

    /// <summary>
    /// Makes a source that reads a list of files of one container of a data store, one file after another, as one stream (FDW-3089): the rows of every
    /// file go out on its one output, in the order of the list, with no more held than a page. Every file must have the fields of the first.
    /// </summary>
    /// <param name="name">The component's name.</param>
    /// <param name="gateway">The data gateway; it must be the ETL data gateway, which reads a declared container at another address.</param>
    /// <param name="dataStore">The data store.</param>
    /// <param name="path">The path in the store, or <see langword="null"/> when the store states none.</param>
    /// <param name="container">The declared container each file is read as.</param>
    /// <param name="pageSize">How many rows go in a batch; at least one.</param>
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
                file, (g, command, token) => ((IEtlDataGateway)g).Execute<IEnumerable<Dictionary<string, object?>>>(command, target, physicalPath, file, token)))],
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

    // Reads one target a page at a time and sends each page out, the rule a pipeline's own extract keeps: another page only follows a page of
    // exactly the size asked for. A source that cannot page answers its whole set whatever it was asked, which is more than a page, so it ends there.
    private async Task<IGenericResult> ReadTarget(Target target, IFlowOutput output, StreamState state, CancellationToken cancellationToken)
    {
        var skipped = 0;
        var checkedFields = false;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Why the page is cut to what the cap leaves: a test run reads the first rows only, and asks for no more than that.
            var take = _rowCap is { } cap ? Math.Min(_pageSize, cap - state.Written) : _pageSize;
            if (take < 1)
            {
                return GenericResult.Success();
            }

            var command = new QueryCommand<Dictionary<string, object?>>
            {
                Paging = new PagingExpression { Skip = skipped, Take = take },
            };

            var read = await target.Read(_gateway, command, cancellationToken).ConfigureAwait(false);
            if (read.IsFailure)
            {
                return GenericResult.Failure(FlowLog.SourceReadFailed(_logger, _name, skipped, read.CurrentMessage));
            }

            if (read.Value is not { } page)
            {
                return GenericResult.Failure(FlowLog.SourceReturnedNothing(_logger, _name));
            }

            var rows = page.Cast<IDictionary<string, object?>>().ToList();
            if (rows.Count == 0)
            {
                return GenericResult.Success();
            }

            if (target.File is not null && !checkedFields)
            {
                var same = SameFields(target.File, rows[0], state);
                if (same.IsFailure)
                {
                    return same;
                }

                checkedFields = true;
            }

            var last = rows.Count != take;
            if (_rowCap is { } limit && rows.Count > limit - state.Written)
            {
                // Why: a source that cannot page answers more than the cap in one go; a test run keeps the first rows of it.
                rows = rows.Take(limit - state.Written).ToList();
            }

            await output.Write(new FlowBatch(rows), cancellationToken).ConfigureAwait(false);
            state.Written += rows.Count;
            if (last)
            {
                return GenericResult.Success();
            }

            skipped += rows.Count;
        }
    }

    // The first row of a file stands for the file's fields. The first file sets the fields every later file must have; a file that differs fails the
    // run naming the file and the fields that differ, and no file is skipped (FDW-3089).
    private IGenericResult SameFields(string file, IDictionary<string, object?> firstRow, StreamState state)
    {
        if (state.First is not { } first)
        {
            state.First = new FirstFile(file, new HashSet<string>(firstRow.Keys, StringComparer.Ordinal));
            return GenericResult.Success();
        }

        if (first.Fields.SetEquals(firstRow.Keys))
        {
            return GenericResult.Success();
        }

        var missing = first.Fields.Except(firstRow.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var extra = firstRow.Keys.Except(first.Fields, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var difference = string.Concat(
            missing.Count > 0 ? $"missing {string.Join(", ", missing)}" : string.Empty,
            missing.Count > 0 && extra.Count > 0 ? "; " : string.Empty,
            extra.Count > 0 ? $"extra {string.Join(", ", extra)}" : string.Empty);
        return GenericResult.Failure(FlowLog.SourceFilesDiffer(_logger, _name, file, first.Name, difference));
    }
}
