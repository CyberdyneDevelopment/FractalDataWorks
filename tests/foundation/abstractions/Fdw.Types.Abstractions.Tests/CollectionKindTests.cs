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
    public void CollectionKinds_All_ReturnsAllKinds()
    {
        // Act
        var all = CollectionKinds.All().ToList();

        // Assert
        all.Count.ShouldBe(6);
    }
}
