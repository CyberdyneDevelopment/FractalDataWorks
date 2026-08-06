using Fdw.Commands.Abstractions;

namespace Fdw.Commands.Abstractions.Tests;

/// <summary>
/// Tests for CommandTypes TypeCollection structure.
/// Note: CommandTypes collection will be empty in this project since no TypeOptions are defined here.
/// The collection is populated by implementation projects (e.g., Commands.Data).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CommandTypesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsEmptyCollectionInAbstractionsProject()
    {
        // Act
        var all = CommandTypes.All();

        // Assert
        all.ShouldNotBeNull();
        // Collection is empty because no TypeOptions are defined in the Abstractions project
        all.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundWhenEmpty()
    {
        // Act
        var result = CommandTypes.ById(1);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(CommandTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundWhenEmpty()
    {
        // Act
        var result = CommandTypes.ByName("Query");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(CommandTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundIsAvailable()
    {
        // Act
        var notFound = CommandTypes.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
    }
}
