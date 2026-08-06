using Fdw.Data.DataContainers.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.DataContainers;

public sealed class DataContainerTypesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsContainerTypeCollection()
    {
        // Act
        var all = DataContainerTypes.All();

        // Assert
        all.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsCorrectContainerType()
    {
        // Arrange
        var all = DataContainerTypes.All();
        if (all.Count == 0) return; // Skip if no container types registered

        var first = all.First();

        // Act
        var result = DataContainerTypes.ById(first.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(first.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsNullForUnknownId()
    {
        // Act
        var result = DataContainerTypes.ById(99999);

        // Assert
        result.ShouldBe(DataContainerTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange
        var all = DataContainerTypes.All();
        if (all.Count == 0) return; // Skip if no container types registered

        var first = all.First();

        // Act & Assert
        DataContainerTypes.ByName(first.Name).ShouldNotBeNull();
        DataContainerTypes.ByName(first.Name.ToLowerInvariant()).ShouldBe(DataContainerTypes.NotFound);
        DataContainerTypes.ByName(first.Name.ToUpperInvariant()).ShouldBe(DataContainerTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = DataContainerTypes.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AllContainerTypesImplementIDataContainerType()
    {
        // Arrange
        var all = DataContainerTypes.All();

        // Act & Assert
        foreach (var containerType in all)
        {
            containerType.ShouldBeAssignableTo<IDataContainerType>();
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AllContainerTypesHaveUniqueIds()
    {
        // Arrange
        var all = DataContainerTypes.All();
        if (all.Count == 0) return; // Skip if no container types registered

        // Act
        var ids = all.Select(c => c.Id).ToHashSet();

        // Assert
        ids.Count.ShouldBe(all.Count);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AllContainerTypesHaveUniqueNames()
    {
        // Arrange
        var all = DataContainerTypes.All();
        if (all.Count == 0) return; // Skip if no container types registered

        // Act
        var names = all.Select(c => c.Name).ToHashSet();

        // Assert
        names.Count.ShouldBe(all.Count);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = DataContainerTypes.ByName("NonExistentContainer");

        // Assert
        result.ShouldBe(DataContainerTypes.NotFound);
    }
}
