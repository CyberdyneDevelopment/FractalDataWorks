using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Reads one category by name (FDW-1465).</summary>
public abstract class GetCategoryEndpointBase : CrudGetEndpointBase<CategoryNameRequest, CategoryPayload>
{
    private readonly CategoryConfigurationProvider _categories;

    /// <summary>Initializes a new instance of the <see cref="GetCategoryEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categories">The categories.</param>
    protected GetCategoryEndpointBase(ILogger<GetCategoryEndpointBase> logger, CategoryConfigurationProvider categories)
        : base(logger)
    {
        _categories = categories;
    }

    /// <inheritdoc />
    protected override string ResourceName => "categories";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(CategoryNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<CategoryPayload?>> FindByIdentifier(CategoryNameRequest request, CancellationToken ct) =>
        CategoryReads.Find(_categories, request.Name, Logger, ct);
}
