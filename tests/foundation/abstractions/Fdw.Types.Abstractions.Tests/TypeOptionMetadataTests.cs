using Fdw.Types;

namespace Fdw.Types.Abstractions.Tests;

/// <summary>
/// Tests for TypeOptionMetadata.
/// </summary>
public class TypeOptionMetadataTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithRequiredProperties_CreatesInstance()
    {
        // Act
        var metadata = new TypeOptionMetadata
        {
            Id = 1,
            Name = "TestOption",
            TypeCollectionId = 100,
            FullTypeName = "MyNamespace.TestOption"
        };

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Id.ShouldBe(1);
        metadata.Name.ShouldBe("TestOption");
        metadata.TypeCollectionId.ShouldBe(100);
        metadata.FullTypeName.ShouldBe("MyNamespace.TestOption");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithOptionalProperties_CreatesInstance()
    {
        // Act
        var metadata = new TypeOptionMetadata
        {
            Id = 2,
            Name = "AnotherOption",
            TypeCollectionId = 200,
            FullTypeName = "MyNamespace.AnotherOption",
            Category = "TestCategory",
            Description = "Test description"
        };

        // Assert
        metadata.Category.ShouldBe("TestCategory");
        metadata.Description.ShouldBe("Test description");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Properties_WithoutOptionals_AreNull()
    {
        // Act
        var metadata = new TypeOptionMetadata
        {
            Id = 3,
            Name = "Option",
            TypeCollectionId = 300,
            FullTypeName = "MyNamespace.Option"
        };

        // Assert
        metadata.Category.ShouldBeNull();
        metadata.Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void PropertiesCollection_DefaultsToEmptyList()
    {
        // Act
        var metadata = new TypeOptionMetadata
        {
            Id = 4,
            Name = "Option",
            TypeCollectionId = 400,
            FullTypeName = "MyNamespace.Option"
        };

        // Assert
        metadata.Properties.ShouldNotBeNull();
        metadata.Properties.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Properties_AreInitOnly()
    {
        // Arrange
        var properties = new List<TypePropertyMetadata>
        {
            new TypePropertyMetadata
            {
                Name = "TestProp",
                PropertyType = "System.String"
            }
        };

        var metadata = new TypeOptionMetadata
        {
            Id = 5,
            Name = "Option",
            TypeCollectionId = 500,
            FullTypeName = "MyNamespace.Option",
            Properties = properties
        };

        // Assert
        metadata.Properties.Count.ShouldBe(1);
        metadata.Properties[0].Name.ShouldBe("TestProp");
        metadata.Properties[0].PropertyType.ShouldBe("System.String");
    }
}
