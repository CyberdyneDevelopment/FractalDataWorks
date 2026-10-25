using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// The default <see cref="IDataSetResolver"/>: the data set of that name, else the variation of that name (FDW-2390).
/// </summary>
/// <remarks>
/// Both providers are asked, so a name held by a data set and by a variation is refused instead of resolved by picking one.
/// A variation is read through the variation domain provider, which finds the implementation its row names, so a kind a developer
/// registers resolves with no change here.
/// </remarks>
public sealed class DataSetResolver : IDataSetResolver
{
    /// <summary>The most views a chain may hold, the view itself included: the same limit the variation domain refuses a deeper chain at.</summary>
    public const int MaxChainDepth = 8;

    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataSetVariationConfigurationProvider _variations;
    private readonly ILogger<DataSetResolver> _logger;

    /// <summary>Initializes a new instance of the <see cref="DataSetResolver"/> class.</summary>
    /// <param name="dataSets">Reads a data set by name or id.</param>
    /// <param name="variations">Reads a variation by name or id, whatever its kind.</param>
    /// <param name="logger">The logger.</param>
    public DataSetResolver(
        IDataSetConfigurationProvider dataSets, IDataSetVariationConfigurationProvider variations, ILogger<DataSetResolver>? logger)
    {
        _dataSets = dataSets ?? throw new ArgumentNullException(nameof(dataSets));
        _variations = variations ?? throw new ArgumentNullException(nameof(variations));
        _logger = logger ?? NullLogger<DataSetResolver>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<ResolvedDataSet?>> Resolve(string name, CancellationToken cancellationToken = default)
        => Resolve(name, ConfigurationLevels.WholeAggregate, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<ResolvedDataSet?>> Resolve(string name, int levels, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var dataSet = await _dataSets.Get(name, levels, cancellationToken).ConfigureAwait(false);
        if (dataSet.IsFailure)
            return dataSet.ToNewResult<ResolvedDataSet?>();

        var variation = await _variations.Get(name, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (variation.IsFailure)
            return variation.ToNewResult<ResolvedDataSet?>();

        if (dataSet.Value is not null && variation.Value is { } held)
            return GenericResult<ResolvedDataSet?>.Failure(DataSetResolverLog.Ambiguous(_logger, name, held.Implementation));

        if (dataSet.Value is { } found)
            return Resolved(name, new ResolvedDataSet(found, null, null, []));

        return variation.Value is { } view
            ? await ResolveVariation(name, view, levels, cancellationToken).ConfigureAwait(false)
            : GenericResult<ResolvedDataSet?>.Success(null);
    }

    private async Task<IGenericResult<ResolvedDataSet?>> ResolveVariation(
        string name, IDataSetVariationImplementationConfiguration view, int levels, CancellationToken cancellationToken)
    {
        var root = await _dataSets.Get(view.BaseDataSetId, levels, cancellationToken).ConfigureAwait(false);
        if (root.IsFailure)
            return root.ToNewResult<ResolvedDataSet?>();

        if (root.Value is not { } dataSet)
            return GenericResult<ResolvedDataSet?>.Failure(
                DataSetResolverLog.RootMissing(_logger, name, view.Implementation, view.BaseDataSetId.ToString("D", CultureInfo.InvariantCulture)));

        var parents = await Parents(name, view, cancellationToken).ConfigureAwait(false);
        if (parents.IsFailure)
            return parents.ToNewResult<ResolvedDataSet?>();

        // The nearest parent is first; a view of the data set itself has none, so it varies the data set.
        var chain = parents.Value!;
        var filters = chain.Reverse().Append(view).Select(link => link.Filters).Where(text => !string.IsNullOrWhiteSpace(text)).Select(text => text!).ToList();
        return Resolved(name, new ResolvedDataSet(dataSet, view, chain.Count == 0 ? dataSet.Name : chain[0].Name, filters));
    }

    // The views above a view, the nearest first. A chain longer than the limit, or one with a view missing, is refused and not cut short:
    // a cut chain would drop a filter and read rows the view is meant to exclude.
    private async Task<IGenericResult<IReadOnlyList<IDataSetVariationImplementationConfiguration>>> Parents(
        string name, IDataSetVariationImplementationConfiguration view, CancellationToken cancellationToken)
    {
        var parents = new List<IDataSetVariationImplementationConfiguration>();
        var next = view.ParentVariationId;
        while (next is { } parentId)
        {
            if (parents.Count + 1 >= MaxChainDepth)
                return GenericResult<IReadOnlyList<IDataSetVariationImplementationConfiguration>>.Failure(
                    DataSetResolverLog.ChainTooDeep(_logger, name, view.Implementation, MaxChainDepth));

            var parent = await _variations.Get(parentId, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
            if (parent.IsFailure)
                return parent.ToNewResult<IReadOnlyList<IDataSetVariationImplementationConfiguration>>();

            if (parent.Value is not { } link)
                return GenericResult<IReadOnlyList<IDataSetVariationImplementationConfiguration>>.Failure(
                    DataSetResolverLog.ParentMissing(_logger, name, view.Implementation, parentId.ToString("D", CultureInfo.InvariantCulture)));

            parents.Add(link);
            next = link.ParentVariationId;
        }

        return GenericResult<IReadOnlyList<IDataSetVariationImplementationConfiguration>>.Success(parents);
    }

    private IGenericResult<ResolvedDataSet?> Resolved(string name, ResolvedDataSet resolved)
    {
        DataSetResolverLog.Resolved(_logger, name, resolved.Kind);
        return GenericResult<ResolvedDataSet?>.Success(resolved);
    }
}
