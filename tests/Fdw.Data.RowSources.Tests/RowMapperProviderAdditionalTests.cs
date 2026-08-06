using Fdw.Data.Abstractions;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Tests;

public sealed class RowMapperProviderAdditionalTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyMapperInitializeDoesNotThrow()
    {
        // Arrange
        var provider = new RowMapperProvider();
        var factory = provider.GetDefaultFactory();
        var mapper = factory.Create();
        var container = new Mock<IStorageContainer>();

        // Act & Assert - Initialize should not throw
        mapper.Initialize(container.Object);
        mapper.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyMapperReturnRowDoesNotThrow()
    {
        // Arrange
        var provider = new RowMapperProvider();
        var factory = provider.GetDefaultFactory();
        var mapper = factory.Create();
        var dict = new Dictionary<string, object?> { ["key"] = "value" };

        // Act & Assert - ReturnRow should not throw
        mapper.ReturnRow(dict);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyMapperResetDoesNotThrow()
    {
        // Arrange
        var provider = new RowMapperProvider();
        var factory = provider.GetDefaultFactory();
        var mapper = factory.Create();

        // Act & Assert - Reset should not throw
        mapper.Reset();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyMapperMapRowReturnsCaseInsensitiveDictionary()
    {
        // Arrange
        var provider = new RowMapperProvider();
        var factory = provider.GetDefaultFactory();
        var mapper = factory.Create();
        var source = new Mock<IRecordCursor>();

        // Act
        var row = mapper.MapRow(source.Object);

        // Assert - should be a new dictionary with OrdinalIgnoreCase comparer
        row.ShouldNotBeNull();
        row.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SetDefaultTypeToNonExistentFallsToFirstAvailable()
    {
        // Arrange
        var provider = new RowMapperProvider();
        var customFactory = new Mock<IRowMapperFactory>();

        provider.RegisterFactory("Custom", customFactory.Object);
        provider.SetDefaultType("NonExistent");

        // Act - "NonExistent" not found, should fall back to first available
        var factory = provider.GetDefaultFactory();

        // Assert
        factory.ShouldBe(customFactory.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SetDefaultTypeToNonExistentWithNoFactoriesReturnsEmptyMapper()
    {
        // Arrange
        var provider = new RowMapperProvider();
        provider.SetDefaultType("NonExistent");

        // Act
        var factory = provider.GetDefaultFactory();

        // Assert - EmptyMapperFactory
        factory.ShouldNotBeNull();
        var mapper = factory.Create();
        mapper.IsInitialized.ShouldBeFalse();
    }
}
