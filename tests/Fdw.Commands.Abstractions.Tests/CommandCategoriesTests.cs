using Fdw.Commands.Abstractions;

namespace Fdw.Commands.Abstractions.Tests;

/// <summary>
/// Tests for CommandCategories TypeCollection structure.
/// Note: CommandCategories collection will be empty in this project since no TypeOptions are defined here.
/// The collection is populated by implementation projects.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CommandCategoriesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsEmptyCollectionInAbstractionsProject()
    {
        // Act
        var all = CommandCategories.All();

        // Assert
        all.ShouldNotBeNull();
        all.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundWhenEmpty()
    {
        // Act
        var result = CommandCategories.ById(1);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(CommandCategories.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundWhenEmpty()
    {
        // Act
        var result = CommandCategories.ByName("Query");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(CommandCategories.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundIsAvailable()
    {
        // Act
        var notFound = CommandCategories.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
    }
}
