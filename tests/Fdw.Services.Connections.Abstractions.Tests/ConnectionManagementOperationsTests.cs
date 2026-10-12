using Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests for ConnectionManagementOperations TypeCollection.
/// </summary>
public class ConnectionManagementOperationsTests
{
    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    [InlineData("ListConnections")]
    [InlineData("GetConnectionMetadata")]
    [InlineData("RefreshConnectionStatus")]
    [InlineData("TestConnection")]
    [InlineData("RemoveConnection")]
    public void ExpectedOperationsAreRegistered(string operationName)
    {
        // Act
        var result = ConnectionManagementOperations.ByName(operationName);

        // Assert
        result.ShouldNotBeNull($"Operation '{operationName}' should be registered");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ListConnectionsOperationHasCorrectProperties()
    {
        // Act
        var operation = ConnectionManagementOperations.ByName("ListConnections");

        // Assert
        operation.ShouldNotBeNull();
        operation.ModifiesState.ShouldBeFalse();
        operation.RequiresExistingConnection.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetConnectionMetadataOperationHasCorrectProperties()
    {
        // Act
        var operation = ConnectionManagementOperations.ByName("GetConnectionMetadata");

        // Assert
        operation.ShouldNotBeNull();
        operation.ModifiesState.ShouldBeFalse();
        operation.RequiresExistingConnection.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RefreshConnectionStatusOperationHasCorrectProperties()
    {
        // Act
        var operation = ConnectionManagementOperations.ByName("RefreshConnectionStatus");

        // Assert
        operation.ShouldNotBeNull();
        operation.ModifiesState.ShouldBeFalse();
        operation.RequiresExistingConnection.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TestConnectionOperationHasCorrectProperties()
    {
        // Act
        var operation = ConnectionManagementOperations.ByName("TestConnection");

        // Assert
        operation.ShouldNotBeNull();
        operation.ModifiesState.ShouldBeFalse();
        operation.RequiresExistingConnection.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RemoveConnectionOperationHasCorrectProperties()
    {
        // Act
        var operation = ConnectionManagementOperations.ByName("RemoveConnection");

        // Assert
        operation.ShouldNotBeNull();
        operation.ModifiesState.ShouldBeTrue();
        operation.RequiresExistingConnection.ShouldBeTrue();
    }
}
