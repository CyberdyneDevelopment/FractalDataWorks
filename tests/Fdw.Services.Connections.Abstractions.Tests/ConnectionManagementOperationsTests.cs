using Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests for ConnectionManagementOperations TypeCollection.
/// </summary>
public class ConnectionManagementOperationsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllReturnsAllOperations()
    {
        // Act
        var all = ConnectionManagementOperations.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThan(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByIdReturnsCorrectOperation()
    {
        // Act
        var result = ConnectionManagementOperations.ById(4);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(4);
        result.Name.ShouldBe("TestConnection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByIdReturnsNullForUnknownId()
    {
        // Act
        var result = ConnectionManagementOperations.ById(99999);

        // Assert
        result.ShouldBe(ConnectionManagementOperations.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByNameReturnsCorrectOperation()
    {
        // Act
        var result = ConnectionManagementOperations.ByName("TestConnection");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestConnection");
        result.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByNameIsCaseSensitive()
    {
        // Assert
        ConnectionManagementOperations.ByName("TestConnection").ShouldNotBeNull();
        ConnectionManagementOperations.ByName("testconnection").ShouldBe(ConnectionManagementOperations.NotFound);
        ConnectionManagementOperations.ByName("testconnection").ShouldBe(ConnectionManagementOperations.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ByNameReturnsNullForUnknownName()
    {
        // Act
        var result = ConnectionManagementOperations.ByName("NonExistent");

        // Assert
        result.ShouldBe(ConnectionManagementOperations.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = ConnectionManagementOperations.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
        result.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllOperationsHaveUniqueIds()
    {
        // Act
        var all = ConnectionManagementOperations.All();
        var ids = all.Select(o => o.Id).ToList();

        // Assert
        ids.Count.ShouldBe(ids.Distinct().Count());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllOperationsHaveUniqueNames()
    {
        // Act
        var all = ConnectionManagementOperations.All();
        var names = all.Select(o => o.Name).ToList();

        // Assert
        names.Count.ShouldBe(names.Distinct().Count());
    }

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
        operation.Id.ShouldBe(0);
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
        operation.Id.ShouldBe(2);
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
        operation.Id.ShouldBe(3);
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
        operation.Id.ShouldBe(4);
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
        operation.Id.ShouldBe(1);
        operation.ModifiesState.ShouldBeTrue();
        operation.RequiresExistingConnection.ShouldBeTrue();
    }
}
