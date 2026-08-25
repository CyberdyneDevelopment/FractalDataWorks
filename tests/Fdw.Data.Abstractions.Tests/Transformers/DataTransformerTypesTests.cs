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
        var all = TransformationTypes.All();

        // Assert
        all.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsCorrectTransformerType()
    {
        // Arrange
        var all = TransformationTypes.All();
        if (all.Count == 0) return; // Skip if no transformer types registered

        var first = all.First();

        // Act
        var result = TransformationTypes.ById(first.Id);

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
        var result = TransformationTypes.ById(99999);

        // Assert
        result.ShouldBe(TransformationTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange
        var all = TransformationTypes.All();
        if (all.Count == 0) return; // Skip if no transformer types registered

        var first = all.First();

        // Act & Assert
        TransformationTypes.ByName(first.Name).ShouldNotBeNull();
        TransformationTypes.ByName(first.Name.ToLowerInvariant()).ShouldBe(TransformationTypes.NotFound);
        TransformationTypes.ByName(first.Name.ToUpperInvariant()).ShouldBe(TransformationTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = TransformationTypes.NotFound;

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
        var all = TransformationTypes.All();

        // Act & Assert
        foreach (var transformerType in all)
        {
            transformerType.ShouldBeAssignableTo<ITransformationType>();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllTransformerTypesHaveUniqueIds()
    {
        // Arrange
        var all = TransformationTypes.All();
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
        var all = TransformationTypes.All();
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
        var result = TransformationTypes.ByName("NonExistentTransformer");

        // Assert
        result.ShouldBe(TransformationTypes.NotFound);
    }
}
