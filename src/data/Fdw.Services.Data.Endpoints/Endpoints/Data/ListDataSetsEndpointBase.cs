using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
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
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly CategoryConfigurationProvider _categories;
    private readonly ITagService? _tags;
    private readonly IDataSetWriters _writers;
    private readonly IExecutionTracker _runs;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly ITenantContext _tenant;
    private readonly TimeProvider _time;
    private CategoryTree? _tree;
    private DataSetStatusLookup? _statuses;

    /// <summary>Initializes a new instance of the <see cref="ListDataSetsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads the page of data sets.</param>
    /// <param name="categories">Reads the category tree the data sets are organised in.</param>
    /// <param name="writers">Which pipelines write each data set, for the status (FDW-4488).</param>
    /// <param name="runs">Where the latest run of each writer is read, for the status.</param>
    /// <param name="settings">Where the status window is read.</param>
    /// <param name="tenant">The caller's tenant, whose own status window applies when it has one.</param>
    /// <param name="time">The clock the status is judged by.</param>
    /// <param name="tags">The Tags service that answers <c>?tag=</c>; absent on a host that does not run it, which then refuses a request that sends <c>tag</c>.</param>
    protected ListDataSetsEndpointBase(
        ILogger<ListDataSetsEndpointBase> logger,
        DataSetConfigurationProvider dataSetProvider,
        CategoryConfigurationProvider categories,
        IDataSetWriters writers,
        IExecutionTracker runs,
        IEffectiveSettingsProvider settings,
        ITenantContext tenant,
        TimeProvider time,
        ITagService? tags = null)
        : base(logger)
    {
        _tags = tags;
        _writers = writers ?? throw new ArgumentNullException(nameof(writers));
        _runs = runs ?? throw new ArgumentNullException(nameof(runs));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        _time = time ?? throw new ArgumentNullException(nameof(time));
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

        // The writers, their latest runs and the window, read once for the whole request, so no data set costs a read of its own (FDW-4488).
        var statuses = await DataSetStatusLookup.Read(_writers, _runs, _settings, _tenant.TenantId, _time, Logger, ct).ConfigureAwait(false);
        if (statuses.IsFailure || statuses.Value is null)
        {
            return statuses.ToNewResult<ConfigurationPage<IDataSetImplementationConfiguration>>();
        }

        _statuses = statuses.Value;
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

        // A variation holds no data of its own (it runs the data set it points at), so no pipeline writes it and it has no status of its own.
        if (item is not IDataSetVariationImplementationConfiguration)
        {
            var status = (_statuses ?? throw new InvalidOperationException("The statuses are read before a page is.")).StatusOf(item.Name, Logger);
            if (status.IsFailure || status.Value is null)
            {
                return Task.FromResult(status.ToNewResult<DataSetSummaryResponse>());
            }

            summary.Status = status.Value.Name;
        }

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
