using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads the category tree in one answer: the top-level categories, each the top of one named hierarchy with everything under it
/// (FDW-1465). The platform's hierarchies and the caller's tenant's are both in it.
/// </summary>
public abstract class GetCategoryTreeEndpointBase : Endpoint<CategoryTreeRequest, CategoryTreePayload>
{
    private readonly CategoryConfigurationProvider _categories;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="GetCategoryTreeEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categories">The categories.</param>
    protected GetCategoryTreeEndpointBase(ILogger<GetCategoryTreeEndpointBase> logger, CategoryConfigurationProvider categories)
    {
        _logger = logger ?? NullLogger<GetCategoryTreeEndpointBase>.Instance;
        _categories = categories;
    }

    /// <summary>Gets the policy a caller holds to read the tree.</summary>
    protected virtual string ReadPolicy => "categories:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/categories");
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Read the category tree";
            s.Description = "Answers the top-level categories in display-name order, each with the categories under it, at any depth. type limits the answer to the hierarchies of one type. A category's path is the display names from the top down, joined by \" / \".";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CategoryTreeRequest req, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(req.Type) && ReferenceEquals(CategoryTypes.ByName(req.Type), CategoryTypes.NotFound))
        {
            var (refusedStatus, refusedBody) = ResultHttpStatusMapper.Map(
                CategoryRefusal.Of<bool>($"the type '{req.Type}' is not a category type"), HttpContext);
            await Send.ResultAsync(TypedResults.Json(refusedBody, statusCode: refusedStatus)).ConfigureAwait(false);
            return;
        }

        var tree = string.IsNullOrWhiteSpace(req.Type)
            ? await CategoryTrees.Load(_categories, _logger, ct).ConfigureAwait(false)
            : await CategoryTrees.Load(_categories, req.Type, _logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null)
        {
            var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(tree, HttpContext);
            await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        var roots = tree.Value.Roots;
        CategoryEndpointLog.TreeRead(_logger, roots.Count);
        await Send.OkAsync(new CategoryTreePayload { Categories = [.. roots.Select(CategoryMapper.Branch)] }, ct).ConfigureAwait(false);
    }
}
