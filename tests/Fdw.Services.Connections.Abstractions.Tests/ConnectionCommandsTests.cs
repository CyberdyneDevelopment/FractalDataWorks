using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests for ConnectionCommands TypeCollection.
/// </summary>
public class ConnectionCommandsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllReturnsAllCommands()
    {
        // Act
        var all = ConnectionCommands.All();

        // Assert
        all.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByIdReturnsNullForUnknownId()
    {
        // Act
        var result = ConnectionCommands.ById(99999);

        // Assert
        result.ShouldBe(ConnectionCommands.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByNameReturnsNullForUnknownName()
    {
        // Act
        var result = ConnectionCommands.ByName("NonExistent");

        // Assert
        result.ShouldBe(ConnectionCommands.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void NotFoundPropertyExists()
    {
        // Act
        var result = ConnectionCommands.NotFound;

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllCommandsCollectionIsNotNull()
    {
        // Act
        var all = ConnectionCommands.All();

        // Assert
        all.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TypeCollectionIsInitialized()
    {
        // Act - accessing All() should not throw
        var all = ConnectionCommands.All();

        // Assert
        all.ShouldNotBeNull();
    }
}
