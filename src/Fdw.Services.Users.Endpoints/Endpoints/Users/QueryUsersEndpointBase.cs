using Fdw.Services.Users.Configuration;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Base endpoint for querying users: <c>POST /users/query</c>.
/// </summary>
/// <remarks>
/// The users list, asked for by filter, order, page and field: a picker that wants only ids and names,
/// a name-taken check that filters on one name and takes one row, a page with its total. It answers
/// from the users domain table, so a user's account is never read. The policy is the list's own,
/// <c>users:read</c>. The fields it accepts are declared by the endpoint that registers it
/// (<see cref="QueryDomainEndpointBase{TDomain}.QueryableFields"/>): what the API exposes of a user is that
/// application's decision, so it is stated there and not derived from <see cref="UserDomainConfiguration"/>.
/// </remarks>
public abstract class QueryUsersEndpointBase : QueryDomainEndpointBase<UserDomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QueryUsersEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userProvider">Reads the page from the users domain table.</param>
    protected QueryUsersEndpointBase(ILogger logger, UserConfigurationProvider userProvider)
        : base(logger, userProvider)
    {
    }

    /// <summary>Gets the resource name used for the route and the default policy: <c>/users/query</c>, <c>users:read</c>.</summary>
    protected override string ResourceName => "users";
}
