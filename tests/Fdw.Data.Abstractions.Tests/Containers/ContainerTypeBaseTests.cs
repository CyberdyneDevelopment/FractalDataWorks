using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Containers;

public sealed class ContainerTypeBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var containerType = new TestContainerType(1, "Table");

        // Assert
        containerType.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var containerType = new TestContainerType(1, "Table");

        // Assert
        containerType.Name.ShouldBe("Table");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDisplayName()
    {
        // Arrange & Act
        var containerType = new TestContainerType(1, "Table");

        // Assert
        containerType.DisplayName.ShouldBe("Database Table");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDescription()
    {
        // Arrange & Act
        var containerType = new TestContainerType(1, "Table");

        // Assert
        containerType.Description.ShouldBe("SQL database table");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSupportsSchemaDiscovery()
    {
        // Arrange & Act
        var containerType = new TestContainerType(1, "Table", supportsSchemaDiscovery: true);

        // Assert
        containerType.SupportsSchemaDiscovery.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsConfigurationKey()
    {
        // Arrange & Act
        var containerType = new TestContainerType(1, "Table");

        // Assert
        containerType.ConfigurationKey.ShouldBe("Containers:Table");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryDefaultsToContainer()
    {
        // Arrange & Act
        var containerType = new TestContainerType(1, "Table");

        // Assert
        containerType.Category.ShouldBe("Container");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryCanBeCustomized()
    {
        // Arrange & Act
        var containerType = new TestContainerType(2, "Custom", category: "CustomCategory");

        // Assert
        containerType.Category.ShouldBe("CustomCategory");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIContainerType()
    {
        // Arrange
        var containerType = new TestContainerType(1, "Table");

        // Act & Assert
        containerType.ShouldBeAssignableTo<IContainerType>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var containerType = new TestContainerType(1, "Table");

        // Act & Assert
        containerType.ShouldBeAssignableTo<ContainerTypeBase>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void DifferentContainerTypesHaveDifferentSettings()
    {
        // Arrange
        var table = new TestContainerType(1, "Table", supportsSchemaDiscovery: true);
        var file = new TestContainerType(2, "File", supportsSchemaDiscovery: false);

        // Act & Assert
        table.SupportsSchemaDiscovery.ShouldBeTrue();
        file.SupportsSchemaDiscovery.ShouldBeFalse();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestContainerType : ContainerTypeBase
    {
        public TestContainerType(
            int id,
            string name,
            bool supportsSchemaDiscovery = true,
            string? category = null)
            : base(
                id,
                name,
                name == "Table" ? "Database Table" : "File System",
                name == "Table" ? "SQL database table" : "File-based storage",
                supportsSchemaDiscovery,
                category)
        {
        }
    }
}
