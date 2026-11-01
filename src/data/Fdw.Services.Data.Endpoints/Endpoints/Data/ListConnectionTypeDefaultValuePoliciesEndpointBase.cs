using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Lists the default-value policies a connection type registers (API-169):
/// <c>GET /connections/types/{Type}/default-value-policies</c>.
/// </summary>
/// <remarks>
/// What a connection's <c>DefaultValuePolicy</c> can name. Omitting it on a connection means the
/// framework's built-in behaviour for that type; a name resolves against this list. A connection type
/// that registers no policy collection answers an empty list, which is the true answer. An unknown
/// connection type answers 404, not an empty list: the two must not read the same.
/// </remarks>
public abstract class ListConnectionTypeDefaultValuePoliciesEndpointBase : CrudListEndpointBase<DefaultValuePolicySummaryDto>
{
    /// <summary>Initializes a new instance of the <see cref="ListConnectionTypeDefaultValuePoliciesEndpointBase"/> class.</summary>
    protected ListConnectionTypeDefaultValuePoliciesEndpointBase(ILogger<ListConnectionTypeDefaultValuePoliciesEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override string ResourceName => "connection-types";

    /// <inheritdoc/>
    protected override string ReadPolicy => "connections:read";

    /// <inheritdoc/>
    protected override string Route => "/connections/types/{Type}/default-value-policies";

    /// <inheritdoc/>
    protected override string EndpointSummary => "List a connection type's default-value policies";

    /// <inheritdoc/>
    protected override string EndpointDescription =>
        "Returns the default-value policy names a connection of this type can use. Empty when the type registers none.";

    /// <inheritdoc/>
    protected override Task<IGenericResult<List<DefaultValuePolicySummaryDto>>> LoadItems(CancellationToken ct)
    {
        if (HttpContext.Request.RouteValues["Type"] is not string { Length: > 0 } connectionType)
            return Task.FromResult(GenericResult<List<DefaultValuePolicySummaryDto>>.Failure(
                ConnectionTypeEndpointLog.ConnectionTypeRouteValueMissing(Logger)));

        var known = Fdw.Services.Connections.ConnectionTypes.All()
            .Any(kvp => string.Equals(kvp.Value.Name, connectionType, StringComparison.Ordinal));
        if (!known)
            return Task.FromResult(GenericResult<List<DefaultValuePolicySummaryDto>>.Failure(
                ConnectionTypeEndpointLog.ConnectionTypeUnknown(Logger, connectionType)));

        // A known connection type with no DefaultValues entry registers no policies.
        var entry = DefaultValues.ByName(connectionType);
        List<DefaultValuePolicySummaryDto> policies = entry == DefaultValues.NotFound
            ? []
            : [.. entry.Policies.Select(p => new DefaultValuePolicySummaryDto { Name = p.Name }).OrderBy(p => p.Name, StringComparer.Ordinal)];

        ConnectionTypeEndpointLog.ListedDefaultValuePolicies(Logger, connectionType, policies.Count);
        return Task.FromResult(GenericResult<List<DefaultValuePolicySummaryDto>>.Success(policies));
    }
}
