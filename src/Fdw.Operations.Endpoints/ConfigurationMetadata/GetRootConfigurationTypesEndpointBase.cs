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
/// Tier 2 default endpoint to list root configuration types (types without parents).
/// </summary>
/// <remarks>
/// Why: Wave C4 replaces ConfigurationTypes.All() with IConfigurationContainerLookup.All().
/// All containers in the ctrl tree are treated as roots — IDataNode owns hierarchy.
/// </remarks>
public abstract class GetRootConfigurationTypesEndpointBase : CrudListEndpointBase<ConfigurationTypeSummaryDto>
{
    private readonly IConfigurationContainerLookup _containerLookup;

    /// <summary>Initializes a new instance of the endpoint.</summary>
    protected GetRootConfigurationTypesEndpointBase(IConfigurationContainerLookup containerLookup)
    {
        _containerLookup = containerLookup;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "configuration-types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "configurations:read";

    /// <summary>Gets the route template for this endpoint.</summary>
    protected override string Route => "/configuration/types/roots";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List root configuration types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns configuration types that are top-level (no parent configuration).";

    /// <summary>Loads all root configuration types.</summary>
    protected override Task<IGenericResult<List<ConfigurationTypeSummaryDto>>> LoadItems(CancellationToken ct)
    {
        var items = MapRootTypes();
        return Task.FromResult(GenericResult<List<ConfigurationTypeSummaryDto>>.Success(items.ToList()));
    }

    /// <summary>Maps all containers to summary DTOs, sorted by category and display name.</summary>
    /// <remarks>
    /// Why: All containers are roots — IDataNode owns parent-child structure.
    /// IConfigurationType.GetRoots() was removed in FDW-395 Phase 6.
    /// </remarks>
    protected virtual IReadOnlyList<ConfigurationTypeSummaryDto> MapRootTypes()
    {
        return _containerLookup.All()
            .Select(ConfigurationTypeMapper.ToSummary)
            .OrderBy(t => t.Category, StringComparer.Ordinal)
            .ThenBy(t => t.DisplayName, StringComparer.Ordinal)
            .ToList();
    }
}
