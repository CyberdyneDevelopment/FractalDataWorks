using Fdw.Services.Authorization.Configuration;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Base endpoint for querying roles: <c>POST /roles/query</c>.
/// </summary>
/// <remarks>
/// The roles list, asked for by filter, order, page and field: a picker that wants only ids and names, a
/// name-taken check that filters on one name and takes one row, a page with its total. It answers from the
/// roles domain table, so no role's permissions are read. The policy is the list's own,
/// <c>settings/role:read</c>. The fields it accepts are declared by the endpoint that registers it
/// (<see cref="QueryDomainEndpointBase{TDomain}.QueryableFields"/>): what the API exposes of a role is that
/// application's decision.
/// </remarks>
public abstract class QueryRolesEndpointBase : QueryDomainEndpointBase<RoleDomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QueryRolesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="roleProvider">Reads the page from the roles domain table.</param>
    protected QueryRolesEndpointBase(ILogger logger, IRoleConfigurationProvider roleProvider)
        : base(logger, roleProvider)
    {
    }

    /// <summary>Gets the resource name used for the route: <c>/roles/query</c>.</summary>
    protected override string ResourceName => "roles";

    /// <summary>Gets the read policy: the list's own, <c>settings/role:read</c>.</summary>
    protected override string ReadPolicy => "settings/role:read";
}
