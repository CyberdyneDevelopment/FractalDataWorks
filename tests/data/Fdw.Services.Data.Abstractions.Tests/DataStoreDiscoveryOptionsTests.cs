using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Abstractions.Tests;

public class DataStoreDiscoveryOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExcludedSchemasCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions() { ExcludedSchemas = ["sys", "INFORMATION_SCHEMA", "guest"], DiscoverViews = true, DiscoverIndexes = true, DiscoverForeignKeys = true, DiscoverDescriptions = true };
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
        var result = new DataStoreDiscoveryOptions() { ExcludedSchemas = ["sys", "INFORMATION_SCHEMA", "guest"], DiscoverViews = true, DiscoverIndexes = true, DiscoverForeignKeys = true, DiscoverDescriptions = true };

        // Assert
        result.IncludeOnlySchemas.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IncludeOnlySchemasCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions() { ExcludedSchemas = ["sys", "INFORMATION_SCHEMA", "guest"], DiscoverViews = true, DiscoverIndexes = true, DiscoverForeignKeys = true, DiscoverDescriptions = true };
        var schemas = new List<string> { "schema1", "schema2" };

        // Act
        options.IncludeOnlySchemas = schemas;

        // Assert
        options.IncludeOnlySchemas.ShouldBe(schemas);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverViewsCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions() { ExcludedSchemas = ["sys", "INFORMATION_SCHEMA", "guest"], DiscoverViews = true, DiscoverIndexes = true, DiscoverForeignKeys = true, DiscoverDescriptions = true };

        // Act
        options.DiscoverViews = false;

        // Assert
        options.DiscoverViews.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverIndexesCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions() { ExcludedSchemas = ["sys", "INFORMATION_SCHEMA", "guest"], DiscoverViews = true, DiscoverIndexes = true, DiscoverForeignKeys = true, DiscoverDescriptions = true };

        // Act
        options.DiscoverIndexes = false;

        // Assert
        options.DiscoverIndexes.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverForeignKeysCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions() { ExcludedSchemas = ["sys", "INFORMATION_SCHEMA", "guest"], DiscoverViews = true, DiscoverIndexes = true, DiscoverForeignKeys = true, DiscoverDescriptions = true };

        // Act
        options.DiscoverForeignKeys = false;

        // Assert
        options.DiscoverForeignKeys.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoverDescriptionsCanBeSet()
    {
        // Arrange
        var options = new DataStoreDiscoveryOptions() { ExcludedSchemas = ["sys", "INFORMATION_SCHEMA", "guest"], DiscoverViews = true, DiscoverIndexes = true, DiscoverForeignKeys = true, DiscoverDescriptions = true };

        // Act
        options.DiscoverDescriptions = false;

        // Assert
        options.DiscoverDescriptions.ShouldBeFalse();
    }

}
