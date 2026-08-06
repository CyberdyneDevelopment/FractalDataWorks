using Fdw.Commands.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Abstractions.Tests;

/// <summary>
/// Tests for DataCommands TypeCollection structure.
/// Note: DataCommands collection will be empty in this project since no TypeOptions are defined here.
/// The collection is populated by implementation projects (e.g., Commands.Data with Query, Insert, Update, Delete).
/// </summary>
public sealed class DataCommandsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsEmptyCollectionInAbstractionsProject()
    {
        // Act
        var all = DataCommands.All();

        // Assert
        all.ShouldNotBeNull();
        // Collection is empty because no TypeOptions are defined in the Abstractions project
        all.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundWhenEmpty()
    {
        // Act
        var query = DataCommands.ByName("Query");

        // Assert
        query.ShouldNotBeNull();
        query.ShouldBe(DataCommands.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForInvalidName()
    {
        // Act
        var result = DataCommands.ByName("InvalidDataCommand");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(DataCommands.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundIsAvailable()
    {
        // Act
        var notFound = DataCommands.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
    }
}
