using Fdw.Services.RateLimiting.Abstractions;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Tests for RateLimitPolicies TypeCollection.
/// Verifies that all policies are discovered and accessible via the collection.
/// </summary>
[Collection(nameof(RateLimitingTestCollection))]
public class RateLimitPoliciesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsAllDiscoveredPolicies()
    {
        // Act
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldNotBeNull();
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThanOrEqualTo(4); // Standard, Authenticated, Premium, Admin
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsStandardPolicy()
    {
        // Act
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldContain(p => p.Name == "Standard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsAuthenticatedPolicy()
    {
        // Act
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldContain(p => p.Name == "Authenticated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsPremiumPolicy()
    {
        // Act
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldContain(p => p.Name == "Premium");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsAdminPolicy()
    {
        // Act
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldContain(p => p.Name == "Admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsCorrectPolicyForStandard()
    {
        // Act
        var policy = RateLimitPolicies.ById(1);

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Standard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsCorrectPolicyForAuthenticated()
    {
        // Act
        var policy = RateLimitPolicies.ById(2);

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Authenticated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsCorrectPolicyForPremium()
    {
        // Act
        var policy = RateLimitPolicies.ById(3);

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Premium");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsCorrectPolicyForAdmin()
    {
        // Act
        var policy = RateLimitPolicies.ById(4);

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var policy = RateLimitPolicies.ById(99999);

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsCorrectPolicyForStandard()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Standard");

        // Assert
        policy.ShouldNotBeNull();
        policy.Id.ShouldBe(1);
        policy.Name.ShouldBe("Standard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsCorrectPolicyForAuthenticated()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Authenticated");

        // Assert
        policy.ShouldNotBeNull();
        policy.Id.ShouldBe(2);
        policy.Name.ShouldBe("Authenticated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsCorrectPolicyForPremium()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Premium");

        // Assert
        policy.ShouldNotBeNull();
        policy.Id.ShouldBe(3);
        policy.Name.ShouldBe("Premium");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsCorrectPolicyForAdmin()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Admin");

        // Assert
        policy.ShouldNotBeNull();
        policy.Id.ShouldBe(4);
        policy.Name.ShouldBe("Admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameIsCaseSensitive()
    {
        // Act
        var lowercase = RateLimitPolicies.ByName("standard");
        var uppercase = RateLimitPolicies.ByName("STANDARD");
        var correct = RateLimitPolicies.ByName("Standard");

        // Assert
        lowercase.Name.ShouldBe("_Empty");
        uppercase.Name.ShouldBe("_Empty");
        correct.Name.ShouldBe("Standard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var policy = RateLimitPolicies.ByName("UnknownPolicy");

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var notFound = RateLimitPolicies.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
        notFound.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StandardPropertyReturnsStandardPolicy()
    {
        // Act
        var policy = RateLimitPolicies.Standard;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Standard");
        policy.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AuthenticatedPropertyReturnsAuthenticatedPolicy()
    {
        // Act
        var policy = RateLimitPolicies.Authenticated;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Authenticated");
        policy.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PremiumPropertyReturnsPremiumPolicy()
    {
        // Act
        var policy = RateLimitPolicies.Premium;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Premium");
        policy.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AdminPropertyReturnsAdminPolicy()
    {
        // Act
        var policy = RateLimitPolicies.Admin;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Admin");
        policy.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesHaveUniqueIds()
    {
        // Act
        var all = RateLimitPolicies.All();
        var ids = all.Select(p => p.Id).ToList();

        // Assert
        ids.Distinct().Count().ShouldBe(ids.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesHaveUniqueNames()
    {
        // Act
        var all = RateLimitPolicies.All();
        var names = all.Select(p => p.Name).ToList();

        // Assert
        names.Distinct().Count().ShouldBe(names.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesHaveNonEmptyNames()
    {
        // Act
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldAllBe(p => !string.IsNullOrWhiteSpace(p.Name));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesHavePositiveIds()
    {
        // Act
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldAllBe(p => p.Id > 0);
    }
}
