using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Tags;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for listing data sets, a page at a time. <c>?category=</c> names a category of the DataSet tree and lists the
/// data sets under it and under every category below it (FDW-1473). <c>?kind=Simple,Variation</c> keeps the kinds named (a variation is a data
/// set of kind Variation, FDW-3638); the two together keep what both say. <c>?tag=</c> keeps the data sets that carry every tag named, with or
/// without the others (FDW-3816).
/// </summary>
public abstract class ListDataSetsEndpointBase
    : CrudPageEndpointBase<IDataSetImplementationConfiguration, DataSetSummaryResponse>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly ICategoryConfigurationProvider _categories;
    private readonly IDataSetStatuses _statuses;
    private readonly ITagService? _tags;
    private CategoryTree? _tree;
    private IDataSetStatusView? _statusView;

    /// <summary>Initializes a new instance of the <see cref="ListDataSetsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads the page of data sets.</param>
    /// <param name="categories">Reads the category tree the data sets are organised in.</param>
    /// <param name="statuses">The data set domain's answer to a data set's status (FDW-4488).</param>
    /// <param name="tags">The Tags service that answers <c>?tag=</c>; absent on a host that does not run it, which then refuses a request that sends <c>tag</c>.</param>
    protected ListDataSetsEndpointBase(
        ILogger<ListDataSetsEndpointBase> logger,
        IDataSetConfigurationProvider dataSetProvider,
        ICategoryConfigurationProvider categories,
        IDataSetStatuses statuses,
        ITagService? tags = null)
        : base(logger)
    {
        _tags = tags;
        _statuses = statuses;
        _dataSetProvider = dataSetProvider;
        _categories = categories;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConfigurationPage<IDataSetImplementationConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        var tree = await DataSetCategoryLinks.Tree(_categories, Logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null)
        {
            return tree.ToNewResult<ConfigurationPage<IDataSetImplementationConfiguration>>();
        }

        _tree = tree.Value;

        // What a status is judged by, read once for the whole request by the data set domain, so no data set costs a read of its own (FDW-4488).
        var statuses = await _statuses.Read(ct).ConfigureAwait(false);
        if (statuses.IsFailure || statuses.Value is null)
        {
            return statuses.ToNewResult<ConfigurationPage<IDataSetImplementationConfiguration>>();
        }

        _statusView = statuses.Value;
        var selection = await TagSelection.Read(TaggedWith, _tags, "DataSet", Logger, ct).ConfigureAwait(false);
        if (selection.IsFailure || selection.Value is null)
        {
            return selection.ToNewResult<ConfigurationPage<IDataSetImplementationConfiguration>>();
        }

        var kinds = RequestedDataSetKinds.Of(HttpContext.Request, Logger);
        if (kinds.IsFailure)
        {
            return kinds.ToNewResult<ConfigurationPage<IDataSetImplementationConfiguration>>();
        }

        var requested = HttpContext.Request.Query["category"].ToString();
        if (string.IsNullOrWhiteSpace(requested) && kinds.Value!.Count == 0)
        {
            return await selection.Value.Read(
                (skip, take, token) => _dataSetProvider.Get(skip, take, ConfigurationLevels.RecordOnly, token),
                dataSet => dataSet.Id,
                paging.Skip,
                paging.Take,
                ct).ConfigureAwait(false);
        }

        var categoryIds = CategoryIds(tree.Value, requested);
        if (categoryIds.IsFailure)
        {
            return categoryIds.ToNewResult<ConfigurationPage<IDataSetImplementationConfiguration>>();
        }

        var under = await DataSetCategoryLinks.Matching(_dataSetProvider, categoryIds.Value, kinds.Value!, ct).ConfigureAwait(false);
        if (under.IsFailure || under.Value is null)
        {
            return under.ToNewResult<ConfigurationPage<IDataSetImplementationConfiguration>>();
        }

        var tagged = selection.Value;
        var kept = under.Value.Where(dataSet => tagged.Keeps(dataSet.Id)).ToList();
        var window = kept.Skip(paging.Skip).Take(paging.Take).ToList();
        return GenericResult<ConfigurationPage<IDataSetImplementationConfiguration>>.Success(
            new ConfigurationPage<IDataSetImplementationConfiguration>(
                paging.Skip, paging.Take, paging.Skip + window.Count < kept.Count, _ => Stream(window, ct), kept.Count));
    }

    // The category asked for and every category below it, or nothing when the request names none.
    private IGenericResult<IReadOnlyCollection<Guid>?> CategoryIds(CategoryTree tree, string requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return GenericResult<IReadOnlyCollection<Guid>?>.Success(null);
        }

        var category = DataSetCategoryLinks.Resolve(tree, requested, Logger);
        return category.IsFailure || category.Value is not { } categoryId
            ? category.ToNewResult<IReadOnlyCollection<Guid>?>()
            : GenericResult<IReadOnlyCollection<Guid>?>.Success(tree.SelfAndDescendants(categoryId));
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataSetSummaryResponse>> ToSummary(
        IDataSetImplementationConfiguration item, CancellationToken ct)
    {
        var summary = DataSetQueryHelper.MapToSummary(item);
        var label = DataSetCategoryLinks.Label(
            _tree ?? throw new InvalidOperationException("The category tree is read before a page is."), item.CategoryId, Logger);
        if (label.IsFailure || label.Value is null)
        {
            return label.ToNewResult<DataSetSummaryResponse>();
        }

        summary.Category = label.Value.Name;
        summary.CategoryPath = label.Value.Path;

        // A variation has no status of its own: the view answers success with none. A Graph data set is followed through the data sets it nests (FDW-5299).
        var status = await (_statusView ?? throw new InvalidOperationException("The statuses are read before a page is.")).StatusOfNested(item, ct).ConfigureAwait(false);
        if (status.IsFailure)
        {
            return status.ToNewResult<DataSetSummaryResponse>();
        }

        if (status.Value is { } named)
        {
            summary.Status = named.Name;
        }

        return GenericResult<DataSetSummaryResponse>.Success(summary);
    }

    private static async IAsyncEnumerable<IDataSetImplementationConfiguration> Stream(
        IReadOnlyList<IDataSetImplementationConfiguration> items, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
