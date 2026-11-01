using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.RateLimiting.Abstractions.Policies;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Integration tests verifying policy hierarchy and TypeCollection behavior.
/// Tests cross-cutting concerns and policy relationships.
/// </summary>
public class RateLimitPolicyIntegrationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PolicyNamesMatchExpectedValues()
    {
        // Assert
        RateLimitPolicies.Standard.Name.ShouldBe("Standard");
        RateLimitPolicies.Authenticated.Name.ShouldBe("Authenticated");
        RateLimitPolicies.Premium.Name.ShouldBe("Premium");
        RateLimitPolicies.Admin.Name.ShouldBe("Admin");
    }
}
