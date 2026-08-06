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
    public void PolicyHierarchyIsCorrect()
    {
        // Arrange
        var standard = new StandardRateLimitPolicy();
        var authenticated = new AuthenticatedRateLimitPolicy();
        var premium = new PremiumRateLimitPolicy();
        var admin = new AdminRateLimitPolicy();

        // Assert - Request limits increase with tier
        standard.RequestsPerWindow.ShouldBeLessThan(authenticated.RequestsPerWindow);
        authenticated.RequestsPerWindow.ShouldBeLessThan(premium.RequestsPerWindow);
        premium.RequestsPerWindow.ShouldBeLessThan(admin.RequestsPerWindow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BurstLimitsIncreaseWithTier()
    {
        // Arrange
        var standard = new StandardRateLimitPolicy();
        var authenticated = new AuthenticatedRateLimitPolicy();
        var premium = new PremiumRateLimitPolicy();
        var admin = new AdminRateLimitPolicy();

        // Assert
        standard.BurstLimit.ShouldBeLessThan(authenticated.BurstLimit);
        authenticated.BurstLimit.ShouldBeLessThan(premium.BurstLimit);
        premium.BurstLimit.ShouldBeLessThan(admin.BurstLimit);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesUseOneMinuteWindow()
    {
        // Arrange
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldAllBe(p => p.Window == TimeSpan.FromMinutes(1));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HigherTierPoliciesAllowBurst()
    {
        // Arrange
        var standard = new StandardRateLimitPolicy();
        var authenticated = new AuthenticatedRateLimitPolicy();
        var premium = new PremiumRateLimitPolicy();
        var admin = new AdminRateLimitPolicy();

        // Assert
        standard.AllowBurst.ShouldBeFalse();
        authenticated.AllowBurst.ShouldBeTrue();
        premium.AllowBurst.ShouldBeTrue();
        admin.AllowBurst.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NoPoliciesUseQueueing()
    {
        // Arrange
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldAllBe(p => !p.QueueExceededRequests);
        all.ShouldAllBe(p => p.QueueLimit == 0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StandardAndAuthenticatedUseSlidingWindow()
    {
        // Arrange
        var standard = RateLimitPolicies.Standard;
        var authenticated = RateLimitPolicies.Authenticated;

        // Assert
        standard.Algorithm.Name.ShouldBe("SlidingWindow");
        authenticated.Algorithm.Name.ShouldBe("SlidingWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PremiumAndAdminUseTokenBucket()
    {
        // Arrange
        var premium = RateLimitPolicies.Premium;
        var admin = RateLimitPolicies.Admin;

        // Assert
        premium.Algorithm.Name.ShouldBe("TokenBucket");
        admin.Algorithm.Name.ShouldBe("TokenBucket");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SlidingWindowPoliciesHave10Segments()
    {
        // Arrange
        var standard = RateLimitPolicies.Standard;
        var authenticated = RateLimitPolicies.Authenticated;

        // Assert
        standard.SegmentsPerWindow.ShouldBe(10);
        authenticated.SegmentsPerWindow.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TokenBucketPoliciesHave1Segment()
    {
        // Arrange
        var premium = RateLimitPolicies.Premium;
        var admin = RateLimitPolicies.Admin;

        // Assert
        premium.SegmentsPerWindow.ShouldBe(1);
        admin.SegmentsPerWindow.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BurstPoliciesHave1Point5Multiplier()
    {
        // Arrange
        var authenticated = RateLimitPolicies.Authenticated;
        var premium = RateLimitPolicies.Premium;
        var admin = RateLimitPolicies.Admin;

        // Assert
        authenticated.BurstLimit.ShouldBe((int)(authenticated.RequestsPerWindow * 1.5));
        premium.BurstLimit.ShouldBe((int)(premium.RequestsPerWindow * 1.5));
        admin.BurstLimit.ShouldBe((int)(admin.RequestsPerWindow * 1.5));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StandardPolicyBurstLimitEqualNormalLimit()
    {
        // Arrange
        var standard = RateLimitPolicies.Standard;

        // Assert
        standard.BurstLimit.ShouldBe(standard.RequestsPerWindow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TypeCollectionLookupsReturnConsistentInstances()
    {
        // Act
        var byId = RateLimitPolicies.ById(1);
        var byName = RateLimitPolicies.ByName("Standard");
        var property = RateLimitPolicies.Standard;

        // Assert
        byId.ShouldNotBeNull();
        byName.ShouldNotBeNull();
        property.ShouldNotBeNull();

        byId.Id.ShouldBe(byName.Id);
        byId.Name.ShouldBe(byName.Name);
        byId.Id.ShouldBe(property.Id);
        byId.Name.ShouldBe(property.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesImplementRequiredInterfaces()
    {
        // Arrange
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldAllBe(p => p is IRateLimitPolicy);
        all.ShouldAllBe(p => p is RateLimitPolicyBase);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PolicyIdsMatchExpectedValues()
    {
        // Assert
        RateLimitPolicies.Standard.Id.ShouldBe(1);
        RateLimitPolicies.Authenticated.Id.ShouldBe(2);
        RateLimitPolicies.Premium.Id.ShouldBe(3);
        RateLimitPolicies.Admin.Id.ShouldBe(4);
    }

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

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData(1, "Standard", 100)]
    [InlineData(2, "Authenticated", 500)]
    [InlineData(3, "Premium", 2000)]
    [InlineData(4, "Admin", 10000)]
    public void PolicyHasExpectedConfiguration(int id, string name, int requestsPerWindow)
    {
        // Act
        var policy = RateLimitPolicies.ById(id);

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe(name);
        policy.RequestsPerWindow.ShouldBe(requestsPerWindow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesHavePositiveConfiguration()
    {
        // Arrange
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldAllBe(p => p.RequestsPerWindow > 0);
        all.ShouldAllBe(p => p.BurstLimit > 0);
        all.ShouldAllBe(p => p.Window > TimeSpan.Zero);
        all.ShouldAllBe(p => p.SegmentsPerWindow > 0);
        all.ShouldAllBe(p => p.QueueLimit >= 0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BurstLimitNeverLessThanNormalLimit()
    {
        // Arrange
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldAllBe(p => p.BurstLimit >= p.RequestsPerWindow);
    }
}
