using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Tier 2 default endpoint to list configuration types by category.
/// </summary>
/// <remarks>
/// Why: Wave C4 replaces ConfigurationTypes.GetByServiceCategory() with
/// IConfigurationContainerLookup.ByCategory(). Returns empty until Wave A6 adds
/// SectionPath metadata to IDataContainer.
/// </remarks>
public abstract class GetConfigurationTypesByCategoryEndpointBase : CrudListEndpointBase<GetTypesByCategoryRequest, ConfigurationTypeSummaryDto>
{
    private readonly IConfigurationContainerLookup _containerLookup;

    /// <summary>Initializes a new instance of the endpoint.</summary>
    protected GetConfigurationTypesByCategoryEndpointBase(IConfigurationContainerLookup containerLookup)
    {
        _containerLookup = containerLookup;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "configuration-types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "configurations:read";

    /// <summary>Gets the route template for this endpoint.</summary>
    protected override string Route => "/configuration/types";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List configuration types by category";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns all available configuration types for a given category (Connection, DataStore, etc.). " +
        "Types are discovered automatically via the ctrl IDataStore tree.";

    /// <summary>Loads configuration types filtered by the requested category.</summary>
    protected override Task<IGenericResult<List<ConfigurationTypeSummaryDto>>> LoadItems(GetTypesByCategoryRequest request, CancellationToken ct)
    {
        var items = MapTypes(request.Category);
        return Task.FromResult(GenericResult<List<ConfigurationTypeSummaryDto>>.Success(items.ToList()));
    }

    /// <summary>Maps containers in the specified category to summary DTOs.</summary>
    protected virtual IReadOnlyList<ConfigurationTypeSummaryDto> MapTypes(string category)
    {
        return _containerLookup.ByCategory(category)
            .Select(ConfigurationTypeMapper.ToSummary)
            .OrderBy(t => t.DisplayName, StringComparer.Ordinal)
            .ToList();
    }
}
