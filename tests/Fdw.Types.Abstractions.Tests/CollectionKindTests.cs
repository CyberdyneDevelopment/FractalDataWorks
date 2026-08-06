using Fdw.Types;

namespace Fdw.Types.Abstractions.Tests;

/// <summary>
/// Tests for CollectionKinds TypeCollection.
/// </summary>
public class CollectionKindTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CollectionKinds_HasExpectedTypeOptions()
    {
        // Assert - Ensure all expected type options exist
        CollectionKinds.ByName("Immutable").ShouldNotBeNull();
        CollectionKinds.ByName("Mutable").ShouldNotBeNull();
        CollectionKinds.ByName("Instance").ShouldNotBeNull();
        CollectionKinds.ByName("Service").ShouldNotBeNull();
        CollectionKinds.ByName("MutableService").ShouldNotBeNull();
        CollectionKinds.ByName("ServiceInstance").ShouldNotBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("Immutable", 0)]
    [InlineData("Mutable", 1)]
    [InlineData("Instance", 2)]
    [InlineData("Service", 3)]
    [InlineData("MutableService", 4)]
    [InlineData("ServiceInstance", 5)]
    public void CollectionKinds_HasExpectedIds(string name, int expectedId)
    {
        // Act
        var kind = CollectionKinds.ByName(name);

        // Assert
        kind.ShouldNotBeNull();
        kind.Id.ShouldBe(expectedId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CollectionKinds_ById_ReturnsCorrectKind()
    {
        // Act
        var kind = CollectionKinds.ById(0);

        // Assert
        kind.ShouldNotBeNull();
        kind.Name.ShouldBe("Immutable");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CollectionKinds_All_ReturnsAllKinds()
    {
        // Act
        var all = CollectionKinds.All().ToList();

        // Assert
        all.Count.ShouldBe(6);
    }
}
