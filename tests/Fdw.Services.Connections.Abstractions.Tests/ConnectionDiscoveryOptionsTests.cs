using Fdw.Services.Connections.Abstractions.Commands;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests for ConnectionDiscoveryOptions.
/// </summary>
public class ConnectionDiscoveryOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultConstructorSetsIncludeMetadataToTrue()
    {
        // Act
        var options = new ConnectionDiscoveryOptions();

        // Assert
        options.IncludeMetadata.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultConstructorSetsIncludeColumnsToTrue()
    {
        // Act
        var options = new ConnectionDiscoveryOptions();

        // Assert
        options.IncludeColumns.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultConstructorSetsIncludeRelationshipsToFalse()
    {
        // Act
        var options = new ConnectionDiscoveryOptions();

        // Assert
        options.IncludeRelationships.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultConstructorSetsIncludeIndexesToFalse()
    {
        // Act
        var options = new ConnectionDiscoveryOptions();

        // Assert
        options.IncludeIndexes.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultConstructorSetsMaxDepthToThree()
    {
        // Act
        var options = new ConnectionDiscoveryOptions();

        // Assert
        options.MaxDepth.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IncludeMetadataCanBeSetToFalse()
    {
        // Arrange
        var options = new ConnectionDiscoveryOptions();

        // Act
        options.IncludeMetadata = false;

        // Assert
        options.IncludeMetadata.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IncludeColumnsCanBeSetToFalse()
    {
        // Arrange
        var options = new ConnectionDiscoveryOptions();

        // Act
        options.IncludeColumns = false;

        // Assert
        options.IncludeColumns.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IncludeRelationshipsCanBeSetToTrue()
    {
        // Arrange
        var options = new ConnectionDiscoveryOptions();

        // Act
        options.IncludeRelationships = true;

        // Assert
        options.IncludeRelationships.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IncludeIndexesCanBeSetToTrue()
    {
        // Arrange
        var options = new ConnectionDiscoveryOptions();

        // Act
        options.IncludeIndexes = true;

        // Assert
        options.IncludeIndexes.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void MaxDepthCanBeSetToCustomValue(int depth)
    {
        // Arrange
        var options = new ConnectionDiscoveryOptions();

        // Act
        options.MaxDepth = depth;

        // Assert
        options.MaxDepth.ShouldBe(depth);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TypeIsSealed()
    {
        // Act
        var type = typeof(ConnectionDiscoveryOptions);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllPropertiesHavePublicSetters()
    {
        // Act
        var type = typeof(ConnectionDiscoveryOptions);
        var properties = type.GetProperties();

        // Assert
        foreach (var property in properties)
        {
            property.CanWrite.ShouldBeTrue($"Property {property.Name} should have a public setter");
        }
    }
}
