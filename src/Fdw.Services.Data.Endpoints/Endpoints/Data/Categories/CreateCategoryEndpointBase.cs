using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Creates a category, a node of a category tree (FDW-1465). A tenant administrator creates it in the tenant; a platform administrator
/// acting in the global tenant creates one every tenant sees.
/// </summary>
public abstract class CreateCategoryEndpointBase : CrudCreateEndpointBase<CategoryPayload, CategoryPayload>
{
    private readonly CategoryConfigurationProvider _categories;
    private readonly ITenantContext _tenant;

    /// <summary>Initializes a new instance of the <see cref="CreateCategoryEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categories">The categories.</param>
    /// <param name="tenant">The tenant the category is created in.</param>
    protected CreateCategoryEndpointBase(ILogger<CreateCategoryEndpointBase> logger, CategoryConfigurationProvider categories, ITenantContext tenant)
        : base(logger)
    {
        _categories = categories;
        _tenant = tenant;
    }

    /// <inheritdoc />
    protected override string ResourceName => "categories";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a category";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Adds a category at the top of a new hierarchy, or under the category named in parent, which must be of the same type. The type is DataSet (a category for organising data sets), Glossary (for glossary terms) or Custom (any hierarchy an administrator makes at runtime). The name is the category's stable identifier and never changes; the display name may.";

    /// <inheritdoc />
    protected override string GetResourceName(CategoryPayload request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CategoryPayload request, CancellationToken ct)
    {
        var existing = await _categories.Get(request.Name, ct).ConfigureAwait(false);
        return existing.IsFailure ? existing.ToNewResult<bool>() : GenericResult<bool>.Success(existing.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<CategoryPayload>> Create(CategoryPayload request, CancellationToken ct)
    {
        var tree = await CategoryTrees.Load(_categories, Logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null)
        {
            return tree.ToNewResult<CategoryPayload>();
        }

        var parent = CategoryRules.Parent(request, tree.Value);
        if (parent.IsFailure)
        {
            return parent.ToNewResult<CategoryPayload>();
        }

        if (_tenant.TenantId is not { } tenantId)
        {
            return CategoryRefusal.Of<CategoryPayload>("a category is created in a tenant and this request has none");
        }

        // A category is created for its whole tenant, so its parent must be readable by the whole tenant.
        var scope = CategoryRules.Scope(tree.Value, parent.Value, tenantId, null);
        if (scope.IsFailure)
        {
            return scope.ToNewResult<CategoryPayload>();
        }

        var category = CategoryTypes.ByName(request.Type).NewConfiguration();
        category.Id = Guid.CreateVersion7();
        category.DisplayName = request.DisplayName;
        category.Description = request.Description;
        category.ParentCategoryId = parent.Value;
        category.CreatedAt = DateTimeOffset.UtcNow;
        CategoryEndpointLog.Creating(Logger, request.Type, request.Name, request.Parent);
        var saved = await _categories.Save(category, CategoryDomainConfiguration.DomainName, request.Type, request.Name, ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<CategoryPayload>()
            : await CategoryReads.Read(_categories, request.Name, Logger, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(CategoryPayload detail, CancellationToken ct) => Send.ResponseAsync(detail, 201, ct);
}
