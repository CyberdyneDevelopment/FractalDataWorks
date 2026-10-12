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
}
