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
    public void ByNameReturnsPooledMapperType()
    {
        // Act
        var type = RowMapperTypes.ByName("Pooled");

        // Assert
        type.ShouldNotBeNull();
        type.Name.ShouldBe("Pooled");
        type.ShouldBeAssignableTo<IRowMapperType>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsEmptyForUnknownType()
    {
        // Act
        var type = RowMapperTypes.ByName("Unknown");

        // Assert
        type.ShouldNotBeNull();
        type.Name.ShouldBe("_Empty");
        type.ShouldBeSameAs(RowMapperTypes.NotFound);
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
