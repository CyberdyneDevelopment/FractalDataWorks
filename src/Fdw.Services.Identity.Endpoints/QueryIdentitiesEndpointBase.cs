using Fdw.Configuration;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// Base endpoint for querying identities: <c>POST /identities/query</c>.
/// </summary>
/// <remarks>
/// The identities list, asked for by filter, order, page and field. It answers from the identities domain table,
/// so no identity is composed and no credential is read. The policy is the list's own, <c>identities:read</c>. The
/// fields it accepts are declared by the endpoint that registers it
/// (<see cref="QueryDomainEndpointBase{TDomain}.QueryableFields"/>).
/// </remarks>
public abstract class QueryIdentitiesEndpointBase : QueryDomainEndpointBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QueryIdentitiesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="identityProvider">Reads the page from the identities domain table.</param>
    protected QueryIdentitiesEndpointBase(ILogger logger, IdentityServiceConfigurationProvider identityProvider)
        : base(logger, identityProvider)
    {
    }

    /// <summary>Gets the resource name used for the route and the default policy: <c>/identities/query</c>, <c>identities:read</c>.</summary>
    protected override string ResourceName => "identities";
}
