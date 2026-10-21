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
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for listing data sets, a page at a time. <c>?category=</c> names a category of the DataSet tree and lists the
/// data sets under it and under every category below it (FDW-1473).
/// </summary>
public abstract class ListDataSetsEndpointBase
    : CrudPageEndpointBase<IDataSetImplementationConfiguration, DataSetSummaryResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly CategoryConfigurationProvider _categories;
    private CategoryTree? _tree;

    /// <summary>Initializes a new instance of the <see cref="ListDataSetsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads the page of data sets.</param>
    /// <param name="categories">Reads the category tree the data sets are organised in.</param>
    protected ListDataSetsEndpointBase(
        ILogger<ListDataSetsEndpointBase> logger, DataSetConfigurationProvider dataSetProvider, CategoryConfigurationProvider categories)
        : base(logger)
    {
        _dataSetProvider = dataSetProvider ?? throw new ArgumentNullException(nameof(dataSetProvider));
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
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
        var requested = HttpContext.Request.Query["category"].ToString();
        if (string.IsNullOrWhiteSpace(requested))
        {
            return await _dataSetProvider.Get(paging.Skip, paging.Take, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        }

        var category = DataSetCategoryLinks.Resolve(tree.Value, requested, Logger);
        if (category.IsFailure || category.Value is not { } categoryId)
        {
            return category.ToNewResult<ConfigurationPage<IDataSetImplementationConfiguration>>();
        }

        var under = await DataSetCategoryLinks.Citing(_dataSetProvider, tree.Value.SelfAndDescendants(categoryId), ct).ConfigureAwait(false);
        if (under.IsFailure || under.Value is null)
        {
            return under.ToNewResult<ConfigurationPage<IDataSetImplementationConfiguration>>();
        }

        var window = under.Value.Skip(paging.Skip).Take(paging.Take).ToList();
        return GenericResult<ConfigurationPage<IDataSetImplementationConfiguration>>.Success(
            new ConfigurationPage<IDataSetImplementationConfiguration>(
                paging.Skip, paging.Take, paging.Skip + window.Count < under.Value.Count, _ => Stream(window), under.Value.Count));
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<DataSetSummaryResponse>> ToSummary(
        IDataSetImplementationConfiguration item, CancellationToken ct)
    {
        var summary = DataSetQueryHelper.MapToSummary(item);
        var label = DataSetCategoryLinks.Label(
            _tree ?? throw new InvalidOperationException("The category tree is read before a page is."), item.CategoryId, Logger);
        if (label.IsFailure || label.Value is null)
        {
            return Task.FromResult(label.ToNewResult<DataSetSummaryResponse>());
        }

        summary.Category = label.Value.Name;
        summary.CategoryPath = label.Value.Path;
        return Task.FromResult(GenericResult<DataSetSummaryResponse>.Success(summary));
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
