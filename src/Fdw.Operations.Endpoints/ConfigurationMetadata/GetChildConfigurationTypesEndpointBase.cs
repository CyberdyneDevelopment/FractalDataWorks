using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Tier 2 default endpoint to list child configuration types for a parent.
/// </summary>
public abstract class GetChildConfigurationTypesEndpointBase : CrudListEndpointBase<GetChildTypesRequest, ConfigurationTypeSummaryDto>
{
    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "configuration-types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "configurations:read";

    /// <summary>Gets the route template for this endpoint.</summary>
    protected override string Route => "/configuration/types/children";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List child configuration types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns configuration types that are children of a specified parent table.";

    /// <summary>Loads child configuration types for the specified parent table.</summary>
    protected override Task<IGenericResult<List<ConfigurationTypeSummaryDto>>> LoadItems(GetChildTypesRequest request, CancellationToken ct)
    {
        var items = MapChildTypes(request.Parent);
        return Task.FromResult(GenericResult<List<ConfigurationTypeSummaryDto>>.Success(items.ToList()));
    }

    /// <summary>Maps child configuration types of the specified parent to summary DTOs.</summary>
    /// <remarks>
    /// Why: GetByParentTable was removed in FDW-395 Phase 6 — IDataNode owns parent-child structure.
    /// Configuration types no longer expose ParentTableName. This endpoint now returns empty;
    /// callers should use IDataNode hierarchy for parent-child navigation.
    /// </remarks>
    protected virtual IReadOnlyList<ConfigurationTypeSummaryDto> MapChildTypes(string parent)
    {
        // Why: ParentTableName removed from IConfigurationType — IDataNode owns hierarchy.
        // Returns empty; parent-child navigation goes through IDataNode, not ConfigurationTypes.
        return [];
    }
}
