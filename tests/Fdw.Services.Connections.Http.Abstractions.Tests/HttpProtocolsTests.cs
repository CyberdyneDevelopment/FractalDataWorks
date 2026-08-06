using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests;

public class HttpProtocolsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllReturnsAllHttpProtocols()
    {
        // Act
        var all = HttpProtocols.All();

        // Assert - No implementations exist in Abstractions project, so collection is empty
        all.ShouldNotBeNull();
        all.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByIdReturnsNotFoundForAnyId()
    {
        // Act
        var protocol = HttpProtocols.ById(1);

        // Assert - Returns NotFound instance when no implementations exist
        protocol.ShouldNotBeNull();
        protocol.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsNotFoundForAnyName()
    {
        // Act
        var protocol = HttpProtocols.ByName("SomeProtocol");

        // Assert - Returns NotFound instance when no implementations exist
        protocol.ShouldNotBeNull();
        protocol.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var notFound = HttpProtocols.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
        notFound.Name.ShouldBe("_Empty");
    }
}
