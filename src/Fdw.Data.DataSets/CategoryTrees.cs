using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Reads the category tree (FDW-1465): every node, in one query on the domain table, built into a tree.</summary>
public static class CategoryTrees
{
    private const int AllRows = int.MaxValue - 1;

    /// <summary>Loads every category of the current tenant and the platform, of every type.</summary>
    /// <param name="categories">The categories.</param>
    /// <param name="logger">The logger a stored tree that is not sound is reported through.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tree, or the failure that says why the read failed or the stored nodes do not make a tree.</returns>
    public static Task<IGenericResult<CategoryTree>> Load(
        ICategoryConfigurationProvider categories, ILogger logger, CancellationToken cancellationToken = default) =>
        Read(categories, new DomainQuery(null, null, 0, AllRows, false), "every-type", logger, cancellationToken);

    /// <summary>
    /// Loads the categories of one type, which is the hierarchy a service for that type uses (the data set service asks for DataSet).
    /// The type is a condition of the one query, so only that type's rows are read.
    /// </summary>
    /// <param name="categories">The categories.</param>
    /// <param name="type">The category type, an implementation name from <c>CategoryTypes</c>.</param>
    /// <param name="logger">The logger a stored tree that is not sound is reported through.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tree of that type, or the failure that says why the read failed or the stored nodes do not make a tree.</returns>
    public static Task<IGenericResult<CategoryTree>> Load(
        ICategoryConfigurationProvider categories, string type, ILogger logger, CancellationToken cancellationToken = default) =>
        Read(
            categories,
            new DomainQuery(
                new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = nameof(CategoryDomainConfiguration.Implementation),
                        Operator = FilterOperators.ByName("Equal"),
                        Value = type,
                    },
                },
                null, 0, AllRows, false),
            type,
            logger,
            cancellationToken);

    private static async Task<IGenericResult<CategoryTree>> Read(
        ICategoryConfigurationProvider categories, DomainQuery query, string scope, ILogger logger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logger);
        var page = await categories.Get(query, cancellationToken).ConfigureAwait(false);
        if (page.IsFailure || page.Value is null)
        {
            return page.ToNewResult<CategoryTree>();
        }

        List<CategoryNode> nodes = [];
        await foreach (var row in page.Value.Items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(row.Name) || string.IsNullOrWhiteSpace(row.DisplayName) || string.IsNullOrWhiteSpace(row.Implementation))
            {
                return GenericResult<CategoryTree>.Failure(
                    DataSetsResultCodes.ByName("CategoryTreeUnsound"), logger,
                    ResultDetails.Create("reason", $"the stored category {row.Id} has no name, display name or type"));
            }

            nodes.Add(new CategoryNode(row.Id, row.ParentCategoryId, row.Name, row.DisplayName, row.Description, row.Implementation, row.TenantId, row.VisibilityGroupId));
        }

        if (page.Value.Outcome.IsFailure)
        {
            return page.Value.Outcome.ToNewResult<CategoryTree>();
        }

        var tree = CategoryTree.TryBuild(nodes, out var problems);
        if (tree is null)
        {
            return GenericResult<CategoryTree>.Failure(
                DataSetsResultCodes.ByName("CategoryTreeUnsound"), logger,
                ResultDetails.Create("reason", string.Join("; ", problems)));
        }

        CategoryTreeLog.TreeRead(logger, scope, nodes.Count, tree.Roots.Count);
        return GenericResult<CategoryTree>.Success(tree);
    }
}
