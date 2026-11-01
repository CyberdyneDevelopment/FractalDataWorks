using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// How a data set and the category tree meet (FDW-1473): a data set cites a node of the <c>DataSet</c> category tree by identity, a
/// request names one by its stable name, and an answer carries the name and the path. The tree is read when a request is answered; a route reads it at most once per answer it builds.
/// </summary>
public static class DataSetCategoryLinks
{
    private const int AllRows = int.MaxValue - 1;

    /// <summary>Loads the hierarchy data sets are organised in.</summary>
    /// <param name="categories">The categories.</param>
    /// <param name="logger">The logger a stored tree that is not sound is reported through.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The tree of the DataSet type, or the failure.</returns>
    public static Task<IGenericResult<CategoryTree>> Tree(CategoryConfigurationProvider categories, ILogger logger, CancellationToken ct) =>
        CategoryTrees.Load(categories, DataSetCategoryConfiguration.ImplementationName, logger, ct);

    /// <summary>Finds the category a request names.</summary>
    /// <param name="tree">The DataSet category tree.</param>
    /// <param name="name">The category's stable name; left out for none.</param>
    /// <param name="logger">The logger the lookup is traced through.</param>
    /// <returns>The category's identity, null when the request names none, or the refusal when no such category exists.</returns>
    public static IGenericResult<Guid?> Resolve(CategoryTree tree, string? name, ILogger logger) =>
        CategoryLinks.Resolve(tree, DataSetCategoryConfiguration.ImplementationName, name, logger);

    /// <summary>Writes a data set's category as the API answers it.</summary>
    /// <param name="tree">The DataSet category tree.</param>
    /// <param name="categoryId">The identity the data set cites; null when it cites none.</param>
    /// <param name="logger">The logger a fault is written to.</param>
    /// <returns>The name and path, or the fault that says the data set cites a category that is not in the tree.</returns>
    public static IGenericResult<CategoryLabel> Label(CategoryTree tree, Guid? categoryId, ILogger logger) =>
        CategoryLinks.Label(tree, DataSetCategoryConfiguration.ImplementationName, categoryId, logger);

    /// <summary>Writes the category of a data set onto its detail answer.</summary>
    /// <param name="detail">The detail, mapped from the data set.</param>
    /// <param name="tree">The DataSet category tree.</param>
    /// <param name="categoryId">The identity the data set cites; null when it cites none.</param>
    /// <param name="logger">The logger a fault is written to.</param>
    /// <returns>The detail with its category, or the fault.</returns>
    public static IGenericResult<DataSetDetailResponse> Describe(DataSetDetailResponse detail, CategoryTree tree, Guid? categoryId, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(detail);
        var label = Label(tree, categoryId, logger);
        if (label.IsFailure || label.Value is null)
        {
            return label.ToNewResult<DataSetDetailResponse>();
        }

        detail.Category = label.Value.Name;
        detail.CategoryPath = label.Value.Path;
        return GenericResult<DataSetDetailResponse>.Success(detail);
    }

    /// <summary>Reads the tree and finds the category a request names, before anything is written.</summary>
    /// <param name="categories">The categories.</param>
    /// <param name="name">The category's stable name; left out for none.</param>
    /// <param name="logger">The logger a stored tree that is not sound is reported through.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The tree and the category's identity (null for none), or the refusal.</returns>
    public static Task<IGenericResult<(CategoryTree Tree, Guid? Id)>> Requested(
        CategoryConfigurationProvider categories, string? name, ILogger logger, CancellationToken ct) =>
        CategoryLinks.Requested(categories, DataSetCategoryConfiguration.ImplementationName, name, logger, ct);

    /// <summary>Lists the data sets that sit under any of the categories, in name order.</summary>
    /// <param name="dataSets">The data sets.</param>
    /// <param name="categoryIds">The categories asked about.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The data sets, read without their children, or the failure.</returns>
    public static async Task<IGenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>> Citing(
        DataSetConfigurationProvider dataSets, IReadOnlyCollection<Guid> categoryIds, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dataSets);
        ArgumentNullException.ThrowIfNull(categoryIds);
        var page = await dataSets.Get(0, AllRows, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (page.IsFailure || page.Value is null)
        {
            return page.ToNewResult<IReadOnlyList<IDataSetImplementationConfiguration>>();
        }

        List<IDataSetImplementationConfiguration> found = [];
        await foreach (var dataSet in page.Value.Items.WithCancellation(ct).ConfigureAwait(false))
        {
            if (dataSet.CategoryId is { } cited && categoryIds.Contains(cited))
            {
                found.Add(dataSet);
            }
        }

        return page.Value.Outcome.IsFailure
            ? page.Value.Outcome.ToNewResult<IReadOnlyList<IDataSetImplementationConfiguration>>()
            : GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Success(found);
    }
}
