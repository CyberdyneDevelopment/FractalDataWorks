using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Abstractions.Policies;
using Shouldly;
using Xunit;

namespace Fdw.Services.Resiliency.Abstractions.Tests;

/// <summary>
/// Tests for ResiliencyPolicies TypeCollection.
/// </summary>
public class ResiliencyPoliciesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsAllPolicies()
    {
        // Act
        var all = ResiliencyPolicies.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsDatabasePolicy()
    {
        // Act
        var all = ResiliencyPolicies.All();

        // Assert
        all.ShouldContain(p => p.Name == "Database");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsHttpClientPolicy()
    {
        // Act
        var all = ResiliencyPolicies.All();

        // Assert
        all.ShouldContain(p => p.Name == "HttpClient");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsCriticalPolicy()
    {
        // Act
        var all = ResiliencyPolicies.All();

        // Assert
        all.ShouldContain(p => p.Name == "Critical");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsSimplePolicy()
    {
        // Act
        var all = ResiliencyPolicies.All();

        // Assert
        all.ShouldContain(p => p.Name == "Simple");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsDatabasePolicyForIdOne()
    {
        // Act
        var result = ResiliencyPolicies.ById(1);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Database");
        result.ShouldBeOfType<DatabaseResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsHttpClientPolicyForIdTwo()
    {
        // Act
        var result = ResiliencyPolicies.ById(2);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("HttpClient");
        result.ShouldBeOfType<HttpClientResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsCriticalPolicyForIdThree()
    {
        // Act
        var result = ResiliencyPolicies.ById(3);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Critical");
        result.ShouldBeOfType<CriticalResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsSimplePolicyForIdFour()
    {
        // Act
        var result = ResiliencyPolicies.ById(4);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Simple");
        result.ShouldBeOfType<SimpleRetryResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var result = ResiliencyPolicies.ById(999);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(ResiliencyPolicies.NotFound);
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundForZero()
    {
        // Act
        var result = ResiliencyPolicies.ById(0);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(ResiliencyPolicies.NotFound);
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundForNegativeId()
    {
        // Act
        var result = ResiliencyPolicies.ById(-1);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(ResiliencyPolicies.NotFound);
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsDatabasePolicy()
    {
        // Act
        var result = ResiliencyPolicies.ByName("Database");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.ShouldBeOfType<DatabaseResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsHttpClientPolicy()
    {
        // Act
        var result = ResiliencyPolicies.ByName("HttpClient");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(2);
        result.ShouldBeOfType<HttpClientResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsCriticalPolicy()
    {
        // Act
        var result = ResiliencyPolicies.ByName("Critical");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(3);
        result.ShouldBeOfType<CriticalResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsSimplePolicy()
    {
        // Act
        var result = ResiliencyPolicies.ByName("Simple");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(4);
        result.ShouldBeOfType<SimpleRetryResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = ResiliencyPolicies.ByName("Unknown");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(ResiliencyPolicies.NotFound);
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameIsCaseSensitive()
    {
        // Act & Assert
        ResiliencyPolicies.ByName("Database").ShouldNotBeNull();
        ResiliencyPolicies.ByName("Database").ShouldNotBe(ResiliencyPolicies.NotFound);
        ResiliencyPolicies.ByName("database").ShouldBe(ResiliencyPolicies.NotFound);
        ResiliencyPolicies.ByName("DATABASE").ShouldBe(ResiliencyPolicies.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundForEmptyString()
    {
        // Act
        var result = ResiliencyPolicies.ByName(string.Empty);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(ResiliencyPolicies.NotFound);
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundForWhitespace()
    {
        // Act
        var result = ResiliencyPolicies.ByName("   ");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(ResiliencyPolicies.NotFound);
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesHaveUniqueIds()
    {
        // Arrange
        var all = ResiliencyPolicies.All();

        // Act
        var ids = all.Select(p => p.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        // Assert
        uniqueIds.Count.ShouldBe(ids.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesHaveUniqueNames()
    {
        // Arrange
        var all = ResiliencyPolicies.All();

        // Act
        var names = all.Select(p => p.Name).ToList();
        var uniqueNames = names.Distinct().ToList();

        // Assert
        uniqueNames.Count.ShouldBe(names.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesImplementIResiliencyPolicy()
    {
        // Arrange
        var all = ResiliencyPolicies.All();

        // Assert
        all.ShouldAllBe(p => p is IResiliencyPolicy);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesInheritFromResiliencyPolicyBase()
    {
        // Arrange
        var all = ResiliencyPolicies.All();

        // Assert
        all.ShouldAllBe(p => p is ResiliencyPolicyBase);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DatabaseExtensionPropertyExists()
    {
        // Arrange
        var propertyInfo = typeof(ResiliencyPolicies).GetProperty("Database");

        // Assert
        propertyInfo.ShouldNotBeNull();
        propertyInfo!.PropertyType.ShouldBe(typeof(DatabaseResiliencyPolicy));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DatabaseExtensionPropertyReturnsCorrectPolicy()
    {
        // Act
        var policy = ResiliencyPolicies.Database;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Database");
        policy.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HttpClientExtensionPropertyExists()
    {
        // Arrange
        var propertyInfo = typeof(ResiliencyPolicies).GetProperty("HttpClient");

        // Assert
        propertyInfo.ShouldNotBeNull();
        propertyInfo!.PropertyType.ShouldBe(typeof(HttpClientResiliencyPolicy));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HttpClientExtensionPropertyReturnsCorrectPolicy()
    {
        // Act
        var policy = ResiliencyPolicies.HttpClient;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("HttpClient");
        policy.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CriticalExtensionPropertyExists()
    {
        // Arrange
        var propertyInfo = typeof(ResiliencyPolicies).GetProperty("Critical");

        // Assert
        propertyInfo.ShouldNotBeNull();
        propertyInfo!.PropertyType.ShouldBe(typeof(CriticalResiliencyPolicy));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CriticalExtensionPropertyReturnsCorrectPolicy()
    {
        // Act
        var policy = ResiliencyPolicies.Critical;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Critical");
        policy.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SimpleExtensionPropertyExists()
    {
        // Arrange
        var propertyInfo = typeof(ResiliencyPolicies).GetProperty("Simple");

        // Assert
        propertyInfo.ShouldNotBeNull();
        propertyInfo!.PropertyType.ShouldBe(typeof(SimpleRetryResiliencyPolicy));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SimpleExtensionPropertyReturnsCorrectPolicy()
    {
        // Act
        var policy = ResiliencyPolicies.Simple;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Simple");
        policy.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ExtensionPropertiesReturnSameInstancesAsLookupMethods()
    {
        // Assert
        ReferenceEquals(ResiliencyPolicies.Database, ResiliencyPolicies.ByName("Database")).ShouldBeTrue();
        ReferenceEquals(ResiliencyPolicies.HttpClient, ResiliencyPolicies.ByName("HttpClient")).ShouldBeTrue();
        ReferenceEquals(ResiliencyPolicies.Critical, ResiliencyPolicies.ByName("Critical")).ShouldBeTrue();
        ReferenceEquals(ResiliencyPolicies.Simple, ResiliencyPolicies.ByName("Simple")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsConsistentResultsAcrossMultipleCalls()
    {
        // Act
        var result1 = ResiliencyPolicies.All();
        var result2 = ResiliencyPolicies.All();

        // Assert
        result1.Count.ShouldBe(result2.Count);
        result1.SequenceEqual(result2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundPropertyReturnsEmptyInstance()
    {
        // Act
        var notFound = ResiliencyPolicies.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
        notFound.Name.ShouldBe("_Empty");
        notFound.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundPropertyIsSingleton()
    {
        // Act
        var notFound1 = ResiliencyPolicies.NotFound;
        var notFound2 = ResiliencyPolicies.NotFound;

        // Assert
        ReferenceEquals(notFound1, notFound2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundHasDefaultValues()
    {
        // Act
        var notFound = ResiliencyPolicies.NotFound;

        // Assert
        notFound.MaxRetries.ShouldBe(0);
        notFound.InitialDelay.ShouldBe(TimeSpan.Zero);
        notFound.MaxDelay.ShouldBe(TimeSpan.Zero);
        notFound.BackoffMultiplier.ShouldBe(0);
        notFound.CircuitBreakerDuration.ShouldBe(TimeSpan.Zero);
        notFound.CircuitBreakerThreshold.ShouldBe(0);
    }
}
