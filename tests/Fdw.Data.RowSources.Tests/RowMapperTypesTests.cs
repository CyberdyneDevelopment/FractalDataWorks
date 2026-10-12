using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Mappers;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Tests for the RowMapperTypes TypeCollection.
/// </summary>
public class RowMapperTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsAllRegisteredTypes()
    {
        // Act
        var all = RowMapperTypes.All();

        // Assert
        all.ShouldNotBeNull();
        all.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PooledMapperTypeHasCorrectProperties()
    {
        // Act
        var type = RowMapperTypes.ByName("Pooled");

        // Assert
        type.EstimatedAllocationsPerRow.ShouldBe(0);
        type.SupportsPooling.ShouldBeTrue();
        type.SupportsDynamicAccess.ShouldBeFalse();
    }
}
