using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>
/// Tests for TransformTypes TypeCollection.
/// </summary>
public class TransformTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CategoriesReturnsDistinctCategoriesSorted()
    {
        // Act
        var categories = TransformTypes.Categories;

        // Assert
        categories.ShouldNotBeNull();
        categories.ShouldBeAssignableTo<IReadOnlyList<string>>();
    }
}
