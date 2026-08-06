using Fdw.Services.RateLimiting.Abstractions;
using Xunit;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

public sealed class RateLimitingTypeCollectionFixture
{
    public RateLimitingTypeCollectionFixture()
    {
        _ = RateLimitPolicies.All();
    }
}

[CollectionDefinition(nameof(RateLimitingTestCollection))]
public sealed class RateLimitingTestCollection : ICollectionFixture<RateLimitingTypeCollectionFixture>
{
}
