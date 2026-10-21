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
    private readonly Func<IDataGateway, QueryCommand<Dictionary<string, object?>>, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> _read;
    private readonly IDataGateway _gateway;

    private FlowGatewaySource(
        string name,
        IDataGateway gateway,
        int pageSize,
        int? rowCap,
        Func<IDataGateway, QueryCommand<Dictionary<string, object?>>, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> read,
        ILogger? logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        _name = name;
        _gateway = gateway;
        _pageSize = pageSize;
        _rowCap = rowCap;
        _read = read;
        _logger = logger ?? NullLogger.Instance;
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
            (g, command, token) => g.Execute<IEnumerable<Dictionary<string, object?>>>(command, target, token),
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
                (g, command, token) => g.Execute<IEnumerable<Dictionary<string, object?>>>(command, target, token),
                logger);
        }

        if (gateway is not IEtlDataGateway)
        {
            throw new ArgumentException("A physical path or name needs the ETL data gateway; the gateway given does not read a declared container at another address.", nameof(gateway));
        }

        return new FlowGatewaySource(
            name, gateway, pageSize, rowCap,
            (g, command, token) => ((IEtlDataGateway)g).Execute<IEnumerable<Dictionary<string, object?>>>(command, target, physicalPath, physicalName, token),
            logger);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var output = ports.Output(OutputName);
        var skipped = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Why the page is cut to what the cap leaves: a test run reads the first rows only, and asks for no more than that.
            var take = _rowCap is { } cap ? Math.Min(_pageSize, cap - skipped) : _pageSize;
            if (take < 1)
            {
                return GenericResult.Success();
            }

            var command = new QueryCommand<Dictionary<string, object?>>
            {
                Paging = new PagingExpression { Skip = skipped, Take = take },
            };

            var read = await _read(_gateway, command, cancellationToken).ConfigureAwait(false);
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

            // The rule a pipeline's own extract keeps: another page only follows a page of exactly the size asked for. A source
            // that cannot page answers its whole set whatever it was asked, which is more than a page, so it ends there.
            var last = rows.Count != take;
            if (_rowCap is { } limit && rows.Count > limit - skipped)
            {
                // Why: a source that cannot page answers more than the cap in one go; a test run keeps the first rows of it.
                rows = rows.Take(limit - skipped).ToList();
            }

            await output.Write(new FlowBatch(rows), cancellationToken).ConfigureAwait(false);
            if (last)
            {
                return GenericResult.Success();
            }

            skipped += rows.Count;
        }
    }
}
