using Fdw.Services.Execution.Abstractions.OptionTypes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.OptionTypes;

public class ProcessTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllReturnsProcessTypes()
    {
        // Act
        var all = ProcessTypes.All();

        // Assert
        all.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var result = ProcessTypes.ById(99999);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = ProcessTypes.ByName("NonExistent");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = ProcessTypes.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }
}
