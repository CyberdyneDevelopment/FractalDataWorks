using Fdw.Commands.Abstractions;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Abstractions.Tests;

/// <summary>
/// Tests for TranslatorTypes collection.
/// Note: The inner loop of FindTranslators (matching translators branch) requires actual
/// TypeOption implementations discovered at compile time. Since TypeOptions are defined in
/// implementation projects (not Abstractions), the matching logic is tested via integration
/// tests in those projects. This unit test project covers the collection API and empty-collection behavior.
/// </summary>
public sealed class TranslatorTypesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FindTranslatorsReturnsEmptyArrayWhenNoMatch()
    {
        // Arrange - use IDs that don't match any registered translator
        var sourceFormat = Mock.Of<IDataFormat>(f => f.Id == 999 && f.Name == "Unknown");
        var targetFormat = Mock.Of<IDataFormat>(f => f.Id == 1000 && f.Name == "Unknown");

        // Act
        var result = TranslatorTypes.FindTranslators(sourceFormat, targetFormat);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FindTranslatorsReturnsArrayType()
    {
        // Verifies the return type is correct even with no matches

        // Arrange
        var sourceFormat = Mock.Of<IDataFormat>(f => f.Id == 1 && f.Name == "SQL");
        var targetFormat = Mock.Of<IDataFormat>(f => f.Id == 2 && f.Name == "REST");

        // Act
        var result = TranslatorTypes.FindTranslators(sourceFormat, targetFormat);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<ITranslatorType[]>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FindTranslatorsReturnsEmptyWhenCollectionEmpty()
    {
        // In the Abstractions project, no TypeOptions are registered
        // TypeOptions are defined in implementation projects

        // Arrange
        var sourceFormat = Mock.Of<IDataFormat>(f => f.Id == 1 && f.Name == "SQL");
        var targetFormat = Mock.Of<IDataFormat>(f => f.Id == 2 && f.Name == "REST");

        // Act
        var result = TranslatorTypes.FindTranslators(sourceFormat, targetFormat);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsEmptyCollectionInAbstractionsProject()
    {
        // In Abstractions project, no TypeOptions are registered
        // TypeOptions are defined in implementation projects

        // Act
        var all = TranslatorTypes.All();

        // Assert
        all.ShouldNotBeNull();
        all.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var result = TranslatorTypes.ById(99999);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(TranslatorTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundWhenCollectionEmpty()
    {
        // In Abstractions project with no TypeOptions, any ID returns NotFound

        // Act
        var result = TranslatorTypes.ById(1);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(TranslatorTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = TranslatorTypes.ByName("NonExistentTranslator");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(TranslatorTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundWhenCollectionEmpty()
    {
        // In Abstractions project with no TypeOptions, any name returns NotFound

        // Act
        var result = TranslatorTypes.ByName("SqlToRest");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(TranslatorTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundIsAvailable()
    {
        // Act
        var notFound = TranslatorTypes.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
    }
}
