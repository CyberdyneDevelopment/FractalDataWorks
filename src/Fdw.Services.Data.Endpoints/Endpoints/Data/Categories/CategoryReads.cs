using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Reads one category as the API answers it, with its path and the name of its parent.</summary>
public static class CategoryReads
{
    /// <summary>Finds the category.</summary>
    /// <param name="categories">The categories.</param>
    /// <param name="name">The category's stable name.</param>
    /// <param name="logger">The logger a stored tree that is not sound is reported through.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The category, or null when the tree has none by that name.</returns>
    public static async Task<IGenericResult<CategoryPayload?>> Find(
        CategoryConfigurationProvider categories, string name, ILogger logger, CancellationToken ct)
    {
        var tree = await CategoryTrees.Load(categories, logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null)
        {
            return tree.ToNewResult<CategoryPayload?>();
        }

        if (tree.Value.IsAmbiguous(name))
        {
            return CategoryRefusal.Of<CategoryPayload?>($"'{name}' is a category of more than one tenant or organisation, so the name alone does not say which");
        }

        return tree.Value.FindByName(name) is { } node
            ? GenericResult<CategoryPayload?>.Success(CategoryMapper.Category(tree.Value, node))
            : GenericResult<CategoryPayload?>.Success(null);
    }

    /// <summary>Reads a category that was just saved.</summary>
    /// <param name="categories">The categories.</param>
    /// <param name="name">The category's stable name.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The category, or the failure that says it cannot be read.</returns>
    public static async Task<IGenericResult<CategoryPayload>> Read(
        CategoryConfigurationProvider categories, string name, ILogger logger, CancellationToken ct)
    {
        var found = await Find(categories, name, logger, ct).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.ToNewResult<CategoryPayload>();
        }

        return found.Value is { } payload
            ? GenericResult<CategoryPayload>.Success(payload)
            : GenericResult<CategoryPayload>.Failure(CategoryEndpointLog.SavedButUnreadable(logger, name));
    }
}
