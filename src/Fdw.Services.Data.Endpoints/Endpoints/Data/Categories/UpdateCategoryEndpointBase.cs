using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Renames, describes or moves a category (FDW-1465). Its name and its type never change.</summary>
public abstract class UpdateCategoryEndpointBase : CrudUpdateEndpointBase<CategoryPayload, CategoryPayload>
{
    private readonly CategoryConfigurationProvider _categories;

    /// <summary>Initializes a new instance of the <see cref="UpdateCategoryEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categories">The categories.</param>
    protected UpdateCategoryEndpointBase(ILogger<UpdateCategoryEndpointBase> logger, CategoryConfigurationProvider categories)
        : base(logger)
    {
        _categories = categories;
    }

    /// <inheritdoc />
    protected override string ResourceName => "categories";

    /// <inheritdoc />
    protected override string Route => "/categories/{Name}";

    /// <inheritdoc />
    protected override HttpVerb UpdateVerb => HttpVerb.PUT;

    /// <inheritdoc />
    protected override string EndpointSummary => "Rename, describe or move a category";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Replaces the display name, description and parent of a category with those in the body, and states its type again as it is. parent names the category it moves under (of the same type), or is left out to make it the top of a hierarchy. A move that would put a category under itself or under a category below it is refused.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(CategoryPayload request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<CategoryPayload?>> FindForUpdate(CategoryPayload request, CancellationToken ct) =>
        CategoryReads.Find(_categories, request.Name, Logger, ct);

    /// <inheritdoc />
    protected override async Task<IGenericResult<CategoryPayload>> Update(CategoryPayload request, CategoryPayload existing, CancellationToken ct)
    {
        if (!string.Equals(request.Type, existing.Type, StringComparison.Ordinal))
        {
            return CategoryRefusal.Of<CategoryPayload>($"the category '{request.Name}' is a {existing.Type} category and its type never changes");
        }

        var tree = await CategoryTrees.Load(_categories, Logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null)
        {
            return tree.ToNewResult<CategoryPayload>();
        }

        var parent = CategoryRules.Parent(request, tree.Value, tree.Value.Find(existing.Id)?.Node.ParentId);
        if (parent.IsFailure)
        {
            return parent.ToNewResult<CategoryPayload>();
        }

        var scope = ParentScope(tree.Value, existing.Id, parent.Value);
        if (scope.IsFailure)
        {
            return scope.ToNewResult<CategoryPayload>();
        }

        var move = CategoryRules.Move(tree.Value, existing.Id, parent.Value);
        if (move.IsFailure)
        {
            return move.ToNewResult<CategoryPayload>();
        }

        var stored = await _categories.Get(request.Name, ct).ConfigureAwait(false);
        if (stored.IsFailure || stored.Value is not { } category)
        {
            return stored.IsFailure
                ? stored.ToNewResult<CategoryPayload>()
                : GenericResult<CategoryPayload>.Failure(ServicesResultCodes.ByName("ConfigurationNotFound"));
        }

        CategoryEndpointLog.Changing(Logger, request.Name, request.DisplayName, request.Parent);
        category.DisplayName = request.DisplayName;
        category.Description = request.Description;
        category.ParentCategoryId = parent.Value;
        var saved = await _categories.Update(category, CategoryDomainConfiguration.DomainName, existing.Type, request.Name, ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<CategoryPayload>()
            : await CategoryReads.Read(_categories, request.Name, Logger, ct).ConfigureAwait(false);
    }

    // Only a category that is given a new parent is checked: one that already sits under a parent keeps it for an edit of its other fields.
    private static IGenericResult ParentScope(CategoryTree tree, Guid categoryId, Guid? parentId)
    {
        if (tree.Find(categoryId) is not { } held)
        {
            return CategoryRefusal.Of<bool>("the category is not in the tree");
        }

        if (parentId == held.Node.ParentId)
        {
            return GenericResult.Success();
        }

        return held.Node.TenantId is { } tenantId
            ? CategoryRules.Scope(tree, parentId, tenantId, held.Node.VisibilityGroupId)
            : CategoryRefusal.Of<bool>("the category is not in a tenant");
    }
}
