using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests for ConnectionStates TypeCollection.
/// </summary>
public class ConnectionStatesTests
{
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
}
