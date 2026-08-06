using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests for ConnectionStates TypeCollection.
/// </summary>
public class ConnectionStatesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllReturnsAllStates()
    {
        // Act
        var all = ConnectionStates.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThan(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByIdReturnsCorrectState()
    {
        // Act
        var result = ConnectionStates.ById(3);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(3);
        result.Name.ShouldBe("Open");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByIdReturnsNullForUnknownId()
    {
        // Act
        var result = ConnectionStates.ById(99999);

        // Assert
        result.ShouldBe(ConnectionStates.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByNameReturnsCorrectState()
    {
        // Act
        var result = ConnectionStates.ByName("Open");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Open");
        result.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByNameIsCaseSensitive()
    {
        // Assert
        ConnectionStates.ByName("Open").ShouldNotBeNull();
        ConnectionStates.ByName("open").ShouldBe(ConnectionStates.NotFound);
        ConnectionStates.ByName("open").ShouldBe(ConnectionStates.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByNameReturnsNullForUnknownName()
    {
        // Act
        var result = ConnectionStates.ByName("NonExistent");

        // Assert
        result.ShouldBe(ConnectionStates.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = ConnectionStates.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
        result.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllStatesHaveUniqueIds()
    {
        // Act
        var all = ConnectionStates.All();
        var ids = all.Select(s => s.Id).ToList();

        // Assert
        ids.Count.ShouldBe(ids.Distinct().Count());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllStatesHaveUniqueNames()
    {
        // Act
        var all = ConnectionStates.All();
        var names = all.Select(s => s.Name).ToList();

        // Assert
        names.Count.ShouldBe(names.Distinct().Count());
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    [InlineData("Created")]
    [InlineData("Opening")]
    [InlineData("Open")]
    [InlineData("Executing")]
    [InlineData("Closing")]
    [InlineData("Closed")]
    [InlineData("Broken")]
    [InlineData("Disposed")]
    [InlineData("Unknown")]
    public void ExpectedStatesAreRegistered(string stateName)
    {
        // Act
        var result = ConnectionStates.ByName(stateName);

        // Assert
        result.ShouldNotBeNull($"State '{stateName}' should be registered");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void CreatedStateHasCorrectId()
    {
        // Act
        var state = ConnectionStates.ByName("Created");

        // Assert
        state.ShouldNotBeNull();
        state.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void OpeningStateHasCorrectId()
    {
        // Act
        var state = ConnectionStates.ByName("Opening");

        // Assert
        state.ShouldNotBeNull();
        state.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void OpenStateHasCorrectId()
    {
        // Act
        var state = ConnectionStates.ByName("Open");

        // Assert
        state.ShouldNotBeNull();
        state.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ExecutingStateHasCorrectId()
    {
        // Act
        var state = ConnectionStates.ByName("Executing");

        // Assert
        state.ShouldNotBeNull();
        state.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ClosingStateHasCorrectId()
    {
        // Act
        var state = ConnectionStates.ByName("Closing");

        // Assert
        state.ShouldNotBeNull();
        state.Id.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ClosedStateHasCorrectId()
    {
        // Act
        var state = ConnectionStates.ByName("Closed");

        // Assert
        state.ShouldNotBeNull();
        state.Id.ShouldBe(6);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BrokenStateHasCorrectId()
    {
        // Act
        var state = ConnectionStates.ByName("Broken");

        // Assert
        state.ShouldNotBeNull();
        state.Id.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DisposedStateHasCorrectId()
    {
        // Act
        var state = ConnectionStates.ByName("Disposed");

        // Assert
        state.ShouldNotBeNull();
        state.Id.ShouldBe(8);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void UnknownStateExists()
    {
        // Act
        var state = ConnectionStates.ByName("Unknown");

        // Assert
        state.ShouldNotBeNull();
        state.Name.ShouldBe("Unknown");
    }
}
