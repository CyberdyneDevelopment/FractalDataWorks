using Fdw.Services.Resiliency.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Resiliency.Abstractions.Tests;

/// <summary>
/// Tests for ResiliencyCategories TypeCollection.
/// </summary>
public class ResiliencyCategoryTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DatabaseHasCorrectId()
    {
        // Assert
        ResiliencyCategories.Database.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HttpClientHasCorrectId()
    {
        // Assert
        ResiliencyCategories.HttpClient.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CriticalHasCorrectId()
    {
        // Assert
        ResiliencyCategories.Critical.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SimpleHasCorrectId()
    {
        // Assert
        ResiliencyCategories.Simple.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllCategoriesHaveUniqueIds()
    {
        // Arrange
        var categories = ResiliencyCategories.All();

        // Act
        var uniqueIds = categories.Select(c => c.Id).Distinct().Count();

        // Assert
        uniqueIds.ShouldBe(categories.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DatabaseHasCorrectName()
    {
        // Assert
        ResiliencyCategories.Database.Name.ShouldBe("Database");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HttpClientHasCorrectName()
    {
        // Assert
        ResiliencyCategories.HttpClient.Name.ShouldBe("HttpClient");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CriticalHasCorrectName()
    {
        // Assert
        ResiliencyCategories.Critical.Name.ShouldBe("Critical");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SimpleHasCorrectName()
    {
        // Assert
        ResiliencyCategories.Simple.Name.ShouldBe("Simple");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsDatabase()
    {
        // Act
        var category = ResiliencyCategories.ByName("Database");

        // Assert
        category.Id.ShouldBe(1);
        category.Name.ShouldBe("Database");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsHttpClient()
    {
        // Act
        var category = ResiliencyCategories.ByName("HttpClient");

        // Assert
        category.Name.ShouldBe("HttpClient");
    }
}
