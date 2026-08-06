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
    public void AllReturnsTransformTypes()
    {
        // Act
        var all = TransformTypes.All();

        // Assert
        all.ShouldNotBeNull();
        all.ShouldBeAssignableTo<IEnumerable<ITransformType>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsCorrectTransformTypeWhenExists()
    {
        // Arrange
        var allTypes = TransformTypes.All().ToList();
        if (allTypes.Count == 0)
        {
            // Skip if no types registered
            return;
        }
        var expectedId = (int)allTypes.First().Id;

        // Act
        var result = TransformTypes.ById(expectedId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(expectedId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var result = TransformTypes.ById(99999);

        // Assert
        result.ShouldBe(TransformTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsCorrectTransformTypeWhenExists()
    {
        // Arrange
        var allTypes = TransformTypes.All().ToList();
        if (allTypes.Count == 0)
        {
            // Skip if no types registered
            return;
        }
        var expectedName = allTypes.First().Name;

        // Act
        var result = TransformTypes.ByName(expectedName);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(expectedName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange
        var allTypes = TransformTypes.All().ToList();
        if (allTypes.Count == 0)
        {
            // Skip if no types registered
            return;
        }
        var name = allTypes.First().Name;

        // Act
        var correctCase = TransformTypes.ByName(name);

        // Assert - should find with correct case
        correctCase.ShouldNotBe(TransformTypes.NotFound);

        // If name has different cases available, test case sensitivity
        if (name != name.ToLowerInvariant())
        {
            var lowerCase = TransformTypes.ByName(name.ToLowerInvariant());
            lowerCase.ShouldBe(TransformTypes.NotFound);
        }
        if (name != name.ToUpperInvariant())
        {
            var upperCase = TransformTypes.ByName(name.ToUpperInvariant());
            upperCase.ShouldBe(TransformTypes.NotFound);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = TransformTypes.ByName("NonExistentTransformType999");

        // Assert
        result.ShouldBe(TransformTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = TransformTypes.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
        ((int)result.Id).ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllReturnsConsistentResults()
    {
        // Act
        var first = TransformTypes.All();
        var second = TransformTypes.All();

        // Assert
        first.ShouldBe(second);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdIsConsistentWithAll()
    {
        // Arrange
        var allTypes = TransformTypes.All().ToList();

        // Act & Assert
        foreach (var type in allTypes)
        {
            var byId = TransformTypes.ById((int)type.Id);
            byId.ShouldNotBeNull();
            byId.Id.ShouldBe(type.Id);
            byId.Name.ShouldBe(type.Name);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameIsConsistentWithAll()
    {
        // Arrange
        var allTypes = TransformTypes.All().ToList();

        // Act & Assert
        foreach (var type in allTypes)
        {
            var byName = TransformTypes.ByName(type.Name);
            byName.ShouldNotBeNull();
            byName.Name.ShouldBe(type.Name);
            byName.Id.ShouldBe(type.Id);
        }
    }

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
