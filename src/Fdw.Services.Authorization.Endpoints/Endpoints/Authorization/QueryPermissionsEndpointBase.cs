using Fdw.Services.Authorization.Configuration;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Base endpoint for querying permissions: <c>POST /permissions/query</c>.
/// </summary>
/// <remarks>
/// The permissions list, asked for by filter, order, page and field: a "limit to permission" picker that
/// wants only names and descriptions, a page with its total. It answers from the permissions domain
/// table, which holds everything a permission is. The policy is the list's own, <c>settings/role:read</c>.
/// The fields it accepts are declared by the endpoint that registers it
/// (<see cref="QueryDomainEndpointBase{TDomain}.QueryableFields"/>). Unlike the list, a query answers a
/// permission's name as stored, without the acting tenant's org prefix: the prefix qualifies a name for
/// display, and a picker filters on what is stored.
/// </remarks>
public abstract class QueryPermissionsEndpointBase : QueryDomainEndpointBase<PermissionDomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QueryPermissionsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="permissionProvider">Reads the page from the permissions domain table.</param>
    protected QueryPermissionsEndpointBase(ILogger logger, IPermissionConfigurationProvider permissionProvider)
        : base(logger, permissionProvider)
    {
    }

    /// <summary>Gets the resource name used for the route: <c>/permissions/query</c>.</summary>
    protected override string ResourceName => "permissions";

    /// <summary>Gets the read policy: the list's own, <c>settings/role:read</c>.</summary>
    protected override string ReadPolicy => "settings/role:read";
}
