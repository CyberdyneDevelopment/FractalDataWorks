using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration;

/// <summary>
/// What the ranked searches over a configuration container share: windows of the closest rows read at the database, and the names a hit
/// sits under read by following the keys of the row it hangs from.
/// </summary>
public abstract class RankedSearchBase : IRankedSearch
{
    private readonly string _container;
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly Func<string, ConfigurationRowReader> _readers;
    private readonly RankedRowSearch _ranked;
    private readonly IRowDescriptions? _descriptions;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RankedSearchBase"/> class.</summary>
    /// <param name="gateways">The gateways the container is read through.</param>
    /// <param name="dataStoreName">The data store the container lives in.</param>
    /// <param name="pathName">The path of the container in the data store.</param>
    /// <param name="container">The container whose rows are searched by <c>Name</c> and <c>Description</c>.</param>
    /// <param name="logger">The logger.</param>
    protected RankedSearchBase(IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, string container, ILogger logger)
        : this(gateways, dataStoreName, pathName, container, logger, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RankedSearchBase"/> class for a container whose description is kept outside its table (FDW-6208).</summary>
    /// <param name="gateways">The gateways the container is read through.</param>
    /// <param name="dataStoreName">The data store the container lives in.</param>
    /// <param name="pathName">The path of the container in the data store.</param>
    /// <param name="container">The container whose rows are searched by <c>Name</c> and by the description kept in <paramref name="descriptions"/>.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="descriptions">Where the description is kept: none when the container's table has a <c>Description</c> column.</param>
    protected RankedSearchBase(
        IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, string container, ILogger logger, IRowDescriptions? descriptions)
    {
        _container = container;
        _logger = logger;
        _gateways = gateways;
        _descriptions = descriptions;
        _readers = name => new ConfigurationRowReader(gateways, dataStoreName, pathName, name, logger);
        _ranked = new RankedRowSearch(
            _readers(container), "Name", descriptions is null ? "Description" : null, "RowId", logger, descriptions, "Id", gateways.KeyChunkSize);
    }

    /// <inheritdoc />
    public abstract string Kind { get; }

    /// <inheritdoc />
    public int MaxWindow => _gateways.KeyChunkSize;

    /// <inheritdoc />
    public async Task<IGenericResult<SearchPage>> Search(string text, int window, CancellationToken cancellationToken = default)
    {
        if (window < 1 || window > MaxWindow)
        {
            return GenericResult<SearchPage>.Failure(ConfigurationRowReaderLog.WindowRefused(_logger, _container, window, MaxWindow));
        }

        var found = await _ranked.Search(text, window, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.ToNewResult<SearchPage>();
        }

        try
        {
            var hits = found.Value!.Rows.Select(ToHit).ToList();
            if (_descriptions is not null)
            {
                var described = await Describe(found.Value.Rows, hits, cancellationToken).ConfigureAwait(false);
                if (described.IsFailure)
                {
                    return described.ToNewResult<SearchPage>();
                }

                hits = described.Value!;
            }

            return GenericResult<SearchPage>.Success(new SearchPage(hits, found.Value.Matches));
        }
        catch (InvalidOperationException exception)
        {
            return GenericResult<SearchPage>.Failure(ConfigurationRowReaderLog.ColumnMissing(_logger, _container, exception.Message));
        }
    }

    // The description of the hits of the window, read once for all of them from where it is kept; a row with none has none.
    private async Task<IGenericResult<List<SearchHit>>> Describe(IReadOnlyList<RankedRow> rows, List<SearchHit> hits, CancellationToken cancellationToken)
    {
        var idOfKey = rows.ToDictionary(
            row => row.Key,
            row => row.Row.Identifier("Id") ?? throw new InvalidOperationException($"A row of {_container} has no value in its column 'Id'"));
        var texts = await _descriptions!.TextsOf(idOfKey.Values.ToList(), cancellationToken).ConfigureAwait(false);
        if (texts.IsFailure)
        {
            return texts.ToNewResult<List<SearchHit>>();
        }

        return GenericResult<List<SearchHit>>.Success(
            hits.Select(hit => texts.Value!.TryGetValue(idOfKey[hit.Key], out var text) ? hit.Described(text) : hit).ToList());
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<SearchHit>>> Place(IReadOnlyList<SearchHit> hits, CancellationToken cancellationToken = default)
    {
        // The names each hit's parent sits under, found by the table of the parent and then its key.
        var names = new Dictionary<string, IReadOnlyDictionary<long, IReadOnlyList<string>>>(StringComparer.Ordinal);
        foreach (var parents in hits.GroupBy(hit => hit.ParentTable, StringComparer.Ordinal))
        {
            var chain = new ParentChain(_readers, HopsFrom(parents.Key), _gateways.KeyChunkSize, _logger);
            var read = await chain.Names(parents.Select(hit => hit.ParentKey).ToList(), cancellationToken).ConfigureAwait(false);
            if (read.IsFailure)
            {
                return read.ToNewResult<IReadOnlyList<SearchHit>>();
            }

            names[parents.Key] = read.Value!;
        }

        var located = hits
            .Where(hit => names[hit.ParentTable].ContainsKey(hit.ParentKey))
            .Select(hit => hit.Under(names[hit.ParentTable][hit.ParentKey]))
            .ToList();
        return GenericResult<IReadOnlyList<SearchHit>>.Success(located);
    }

    /// <summary>Makes the hit a row stands for.</summary>
    /// <param name="ranked">The row and its rank.</param>
    /// <returns>The hit; throws <see cref="InvalidOperationException"/> naming a column the row lacks or a parent it does not have.</returns>
    protected abstract SearchHit ToHit(RankedRow ranked);

    /// <summary>Gets the steps from the row a hit hangs from up to the thing that names where it sits.</summary>
    /// <param name="parentTable">The container of the row the hit hangs from.</param>
    /// <returns>The steps, innermost first.</returns>
    protected abstract IReadOnlyList<RowHop> HopsFrom(string parentTable);
}
