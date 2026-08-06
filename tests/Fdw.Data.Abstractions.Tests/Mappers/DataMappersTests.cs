using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Mappers;

public sealed class DataMappersTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsMapperCollection()
    {
        // Act
        var all = DataMappers.All();

        // Assert
        all.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange
        var all = DataMappers.All();
        if (all.Count == 0) return; // Skip if no mappers registered

        var first = all.First();

        // Act & Assert
        DataMappers.ByName(first.Name).ShouldNotBeNull();
        DataMappers.ByName(first.Name.ToLowerInvariant()).ShouldBe(DataMappers.NotFound);
        DataMappers.ByName(first.Name.ToUpperInvariant()).ShouldBe(DataMappers.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = DataMappers.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllMappersAreNotNull()
    {
        // Arrange
        var all = DataMappers.All();

        // Act & Assert
        foreach (var mapper in all)
        {
            mapper.ShouldNotBeNull();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllMappersHaveUniqueNames()
    {
        // Arrange
        var all = DataMappers.All();
        if (all.Count == 0) return; // Skip if no mappers registered

        // Act
        var names = all.Select(m => m.Name).ToHashSet();

        // Assert
        names.Count.ShouldBe(all.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = DataMappers.ByName("NonExistentMapper");

        // Assert
        result.ShouldBe(DataMappers.NotFound);
    }
}
