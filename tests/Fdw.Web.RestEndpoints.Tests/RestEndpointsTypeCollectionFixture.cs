using Fdw.Web.Http.Abstractions.EndPoints;
using Fdw.Web.Http.Abstractions.Security;
using Fdw.Web.Http.Abstractions.Policies;
using Xunit;

namespace Fdw.Web.RestEndpoints.Tests;

public sealed class RestEndpointsTypeCollectionFixture
{
    public RestEndpointsTypeCollectionFixture()
    {
        _ = SecurityMethods.All();
        _ = EndpointTypes.All();
        _ = RateLimitPolicies.All();
    }
}

[CollectionDefinition(nameof(RestEndpointsTestCollection))]
public sealed class RestEndpointsTestCollection : ICollectionFixture<RestEndpointsTypeCollectionFixture>
{
}
