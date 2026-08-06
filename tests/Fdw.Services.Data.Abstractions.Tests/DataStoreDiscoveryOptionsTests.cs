using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Abstractions.Tests;

public class DataStoreDiscoveryOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDefaultExcludedSchemas()
    {
        // Arrange & Act
        var result = new DataStoreDiscoveryOptions();

        // Assert
        result.ExcludedSchemas.ShouldNotBeNull();
        result.ExcludedSchemas.Count.ShouldBe(3);
        result.ExcludedSchemas.ShouldContain("sys");
        result.ExcludedSchemas.ShouldContain("INFORMATION_SCHEMA");
        result.ExcludedSchemas.ShouldContain("guest");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExcludedSchemasCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions();
        var customSchemas = new List<string> { "custom1", "custom2" };

        // Act
        options.ExcludedSchemas = customSchemas;

        // Assert
        options.ExcludedSchemas.ShouldBe(customSchemas);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IncludeOnlySchemasDefaultsToNull()
    {
        // Arrange & Act
        var result = new DataStoreDiscoveryOptions();

        // Assert
        result.IncludeOnlySchemas.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IncludeOnlySchemasCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions();
        var schemas = new List<string> { "schema1", "schema2" };

        // Act
        options.IncludeOnlySchemas = schemas;

        // Assert
        options.IncludeOnlySchemas.ShouldBe(schemas);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverViewsDefaultsToTrue()
    {
        // Arrange & Act
        var result = new DataStoreDiscoveryOptions();

        // Assert
        result.DiscoverViews.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverViewsCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions();

        // Act
        options.DiscoverViews = false;

        // Assert
        options.DiscoverViews.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverIndexesDefaultsToTrue()
    {
        // Arrange & Act
        var result = new DataStoreDiscoveryOptions();

        // Assert
        result.DiscoverIndexes.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverIndexesCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions();

        // Act
        options.DiscoverIndexes = false;

        // Assert
        options.DiscoverIndexes.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverForeignKeysDefaultsToTrue()
    {
        // Arrange & Act
        var result = new DataStoreDiscoveryOptions();

        // Assert
        result.DiscoverForeignKeys.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverForeignKeysCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions();

        // Act
        options.DiscoverForeignKeys = false;

        // Assert
        options.DiscoverForeignKeys.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverDescriptionsDefaultsToTrue()
    {
        // Arrange & Act
        var result = new DataStoreDiscoveryOptions();

        // Assert
        result.DiscoverDescriptions.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverDescriptionsCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions();

        // Act
        options.DiscoverDescriptions = false;

        // Assert
        options.DiscoverDescriptions.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultPropertyReturnsNewInstance()
    {
        // Arrange & Act
        var result = DataStoreDiscoveryOptions.Default;

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<DataStoreDiscoveryOptions>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultPropertyReturnsNewInstanceEachTime()
    {
        // Arrange & Act
        var result1 = DataStoreDiscoveryOptions.Default;
        var result2 = DataStoreDiscoveryOptions.Default;

        // Assert
        result1.ShouldNotBeSameAs(result2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultPropertyReturnsInstanceWithDefaultValues()
    {
        // Arrange & Act
        var result = DataStoreDiscoveryOptions.Default;

        // Assert
        result.DiscoverViews.ShouldBeTrue();
        result.DiscoverIndexes.ShouldBeTrue();
        result.DiscoverForeignKeys.ShouldBeTrue();
        result.DiscoverDescriptions.ShouldBeTrue();
        result.ExcludedSchemas.Count.ShouldBe(3);
    }
}
