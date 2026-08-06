using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Transformers;

public sealed class DataTransformerTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsTransformerTypeCollection()
    {
        // Act
        var all = DataTransformerTypes.All();

        // Assert
        all.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsCorrectTransformerType()
    {
        // Arrange
        var all = DataTransformerTypes.All();
        if (all.Count == 0) return; // Skip if no transformer types registered

        var first = all.First();

        // Act
        var result = DataTransformerTypes.ById(first.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(first.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsNullForUnknownId()
    {
        // Act
        var result = DataTransformerTypes.ById(99999);

        // Assert
        result.ShouldBe(DataTransformerTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange
        var all = DataTransformerTypes.All();
        if (all.Count == 0) return; // Skip if no transformer types registered

        var first = all.First();

        // Act & Assert
        DataTransformerTypes.ByName(first.Name).ShouldNotBeNull();
        DataTransformerTypes.ByName(first.Name.ToLowerInvariant()).ShouldBe(DataTransformerTypes.NotFound);
        DataTransformerTypes.ByName(first.Name.ToUpperInvariant()).ShouldBe(DataTransformerTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = DataTransformerTypes.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllTransformerTypesImplementIDataTransformerType()
    {
        // Arrange
        var all = DataTransformerTypes.All();

        // Act & Assert
        foreach (var transformerType in all)
        {
            transformerType.ShouldBeAssignableTo<IDataTransformerType>();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllTransformerTypesHaveUniqueIds()
    {
        // Arrange
        var all = DataTransformerTypes.All();
        if (all.Count == 0) return; // Skip if no transformer types registered

        // Act
        var ids = all.Select(t => t.Id).ToHashSet();

        // Assert
        ids.Count.ShouldBe(all.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllTransformerTypesHaveUniqueNames()
    {
        // Arrange
        var all = DataTransformerTypes.All();
        if (all.Count == 0) return; // Skip if no transformer types registered

        // Act
        var names = all.Select(t => t.Name).ToHashSet();

        // Assert
        names.Count.ShouldBe(all.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = DataTransformerTypes.ByName("NonExistentTransformer");

        // Assert
        result.ShouldBe(DataTransformerTypes.NotFound);
    }
}
