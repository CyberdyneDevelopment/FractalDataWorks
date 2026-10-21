using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Data;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// How a thing that sits under a category and the category tree of its type meet (FDW-1473, FDW-1466): a request names a category by
/// its stable name, an answer carries the name and the path. A data set, a glossary term and whatever follows cite a category by
/// identity and use this with their own category type.
/// </summary>
public static class CategoryLinks
{
    /// <summary>Reads the tree of one category type and finds the category a request names, before anything is written.</summary>
    /// <param name="categories">The categories.</param>
    /// <param name="type">The category type the thing sits under, an implementation name from <c>CategoryTypes</c>.</param>
    /// <param name="name">The category's stable name; left out for none.</param>
    /// <param name="logger">The logger a stored tree that is not sound is reported through.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tree and the category's identity (null for none), or the refusal.</returns>
    public static async Task<IGenericResult<(CategoryTree Tree, Guid? Id)>> Requested(
        ICategoryConfigurationProvider categories, string type, string? name, ILogger logger, CancellationToken cancellationToken = default)
    {
        var tree = await CategoryTrees.Load(categories, type, logger, cancellationToken).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null)
        {
            return tree.ToNewResult<(CategoryTree, Guid?)>();
        }

        var id = Resolve(tree.Value, type, name, logger);
        return id.IsFailure
            ? id.ToNewResult<(CategoryTree, Guid?)>()
            : GenericResult<(CategoryTree Tree, Guid? Id)>.Success((tree.Value, id.Value));
    }

    /// <summary>Finds the category a request names.</summary>
    /// <param name="tree">The tree of the type.</param>
    /// <param name="type">The category type, which a refusal names.</param>
    /// <param name="name">The category's stable name; left out for none.</param>
    /// <param name="logger">The logger the lookup is traced through.</param>
    /// <returns>The category's identity, null when the request names none, or the refusal when no such category exists.</returns>
    public static IGenericResult<Guid?> Resolve(CategoryTree tree, string type, string? name, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(logger);
        // Left out or empty means none; whitespace is a name like any other and is refused when no category has it.
        if (string.IsNullOrEmpty(name))
        {
            CategoryTreeLog.NoneNamed(logger, type);
            return GenericResult<Guid?>.Success(null);
        }

        CategoryTreeLog.Resolving(logger, type, name);

        if (tree.IsAmbiguous(name))
        {
            return GenericResult<Guid?>.Failure(
                DataSetsResultCodes.ByName("CategoryInvalid"),
                ResultDetails.Create("reason", $"'{name}' is a category of more than one tenant or organisation, so the name alone does not say which"));
        }

        if (tree.FindByName(name) is { } found)
        {
            CategoryTreeLog.Resolved(logger, type, name, found.Node.Id);
            return GenericResult<Guid?>.Success(found.Node.Id);
        }

        return GenericResult<Guid?>.Failure(
                DataSetsResultCodes.ByName("CategoryInvalid"), ResultDetails.Create("reason", $"'{name}' is not a category of the {type} type"));
    }

    /// <summary>Writes the category a thing cites as the API answers it.</summary>
    /// <param name="tree">The tree of the type.</param>
    /// <param name="type">The category type, which a fault names.</param>
    /// <param name="categoryId">The identity the thing cites; null when it cites none.</param>
    /// <param name="logger">The logger a fault is written to.</param>
    /// <returns>The name and path, or the fault that says the thing cites a category that is not in the tree.</returns>
    public static IGenericResult<CategoryLabel> Label(CategoryTree tree, string type, Guid? categoryId, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(logger);
        if (categoryId is not { } id)
        {
            return GenericResult<CategoryLabel>.Success(new CategoryLabel(null, null));
        }

        // A thing that cites a category that is gone is a fault in the store, not in the request: it is the stored tree that is not
        // sound, so it answers the unsound-tree code (HTTP 500) and is logged.
        if (tree.Find(id) is not { } node)
        {
            return GenericResult<CategoryLabel>.Failure(
                DataSetsResultCodes.ByName("CategoryTreeUnsound"), logger,
                ResultDetails.Create("reason", $"something sits under the category {id}, which is not in the {type} category tree"));
        }

        CategoryTreeLog.Labelled(logger, type, id, node.Path);
        return GenericResult<CategoryLabel>.Success(new CategoryLabel(node.Node.Name, node.Path));
    }
}
