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
}
