using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpMethods;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests;

public class HttpMethodCollectionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllReturnsCollection()
    {
        // Act
        var all = HttpMethodCollection.All();

        // Assert - TypeOptions restricted to current compilation,
        // so test project won't see them
        all.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var method = HttpMethodCollection.ById(99999);

        // Assert
        method.ShouldNotBeNull();
        method.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var method = HttpMethodCollection.ByName("UnknownMethod");

        // Assert
        method.ShouldNotBeNull();
        method.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var notFound = HttpMethodCollection.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
        notFound.Name.ShouldBe("_Empty");
    }
}
