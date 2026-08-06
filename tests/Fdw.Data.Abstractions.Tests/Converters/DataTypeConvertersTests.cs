using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Converters;

public sealed class DataTypeConvertersTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsConverterCollection()
    {
        // Act
        var all = DataTypeConverters.All();

        // Assert
        all.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange
        var all = DataTypeConverters.All();
        if (all.Count == 0) return; // Skip if no converter collections registered

        var first = all.First();

        // Act & Assert
        DataTypeConverters.ByName(first.Name).ShouldNotBeNull();
        DataTypeConverters.ByName(first.Name.ToLowerInvariant()).ShouldBe(DataTypeConverters.NotFound);
        DataTypeConverters.ByName(first.Name.ToUpperInvariant()).ShouldBe(DataTypeConverters.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = DataTypeConverters.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllConverterCollectionsImplementIDataTypeConverters()
    {
        // Arrange
        var all = DataTypeConverters.All();

        // Act & Assert
        foreach (var converterCollection in all)
        {
            converterCollection.ShouldBeAssignableTo<IDataTypeConverters>();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllConvertersHaveUniqueNames()
    {
        // Arrange
        var all = DataTypeConverters.All();
        if (all.Count == 0) return; // Skip if no converters registered

        // Act
        var names = all.Select(c => c.Name).ToHashSet();

        // Assert
        names.Count.ShouldBe(all.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = DataTypeConverters.ByName("NonExistentConverter");

        // Assert
        result.ShouldBe(DataTypeConverters.NotFound);
    }
}
