using Fdw.Types;

namespace Fdw.Types.Abstractions.Tests;

/// <summary>
/// Tests for TypeCollectionMetadata.
/// </summary>
public class TypeCollectionMetadataTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithRequiredProperties_CreatesInstance()
    {
        // Arrange & Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 1,
            Name = "TestCollection",
            FullName = "MyNamespace.TestCollection",
            CollectionKind = CollectionKinds.ByName("Immutable")!
        };

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Id.ShouldBe(1);
        metadata.Name.ShouldBe("TestCollection");
        metadata.FullName.ShouldBe("MyNamespace.TestCollection");
        metadata.CollectionKind.ShouldNotBeNull();
        metadata.CollectionKind.Name.ShouldBe("Immutable");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithMutableCollectionKind_CreatesInstance()
    {
        // Arrange & Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 2,
            Name = "MutableCollection",
            FullName = "MyNamespace.MutableCollection",
            CollectionKind = CollectionKinds.ByName("Mutable")!
        };

        // Assert
        metadata.CollectionKind.Name.ShouldBe("Mutable");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithServiceCollectionKind_CreatesInstance()
    {
        // Arrange & Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 3,
            Name = "ServiceCollection",
            FullName = "MyNamespace.ServiceCollection",
            CollectionKind = CollectionKinds.ByName("Service")!
        };

        // Assert
        metadata.CollectionKind.Name.ShouldBe("Service");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithServiceCategory_CreatesInstance()
    {
        // Arrange & Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 4,
            Name = "ConnectionTypes",
            FullName = "MyNamespace.ConnectionTypes",
            CollectionKind = CollectionKinds.ByName("Service")!,
            ServiceCategory = "Connection"
        };

        // Assert
        metadata.ServiceCategory.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithAssemblyQualifiedName_CreatesInstance()
    {
        // Arrange & Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 5,
            Name = "TestCollection",
            FullName = "MyNamespace.TestCollection",
            CollectionKind = CollectionKinds.ByName("Immutable")!,
            AssemblyQualifiedName = "MyNamespace.TestCollection, MyAssembly, Version=1.0.0.0"
        };

        // Assert
        metadata.AssemblyQualifiedName.ShouldBe("MyNamespace.TestCollection, MyAssembly, Version=1.0.0.0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithOptions_CreatesInstance()
    {
        // Arrange
        var options = new List<TypeOptionMetadata>
        {
            new TypeOptionMetadata
            {
                Id = 1,
                Name = "Option1",
                TypeCollectionId = 6,
                FullTypeName = "MyNamespace.Option1"
            },
            new TypeOptionMetadata
            {
                Id = 2,
                Name = "Option2",
                TypeCollectionId = 6,
                FullTypeName = "MyNamespace.Option2"
            }
        };

        // Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 6,
            Name = "TestCollection",
            FullName = "MyNamespace.TestCollection",
            CollectionKind = CollectionKinds.ByName("Immutable")!,
            Options = options
        };

        // Assert
        metadata.Options.ShouldNotBeNull();
        metadata.Options.Count.ShouldBe(2);
        metadata.Options[0].Name.ShouldBe("Option1");
        metadata.Options[1].Name.ShouldBe("Option2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Options_DefaultsToEmptyList()
    {
        // Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 7,
            Name = "TestCollection",
            FullName = "MyNamespace.TestCollection",
            CollectionKind = CollectionKinds.ByName("Immutable")!
        };

        // Assert
        metadata.Options.ShouldNotBeNull();
        metadata.Options.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ServiceCategory_IsNullByDefault()
    {
        // Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 8,
            Name = "TestCollection",
            FullName = "MyNamespace.TestCollection",
            CollectionKind = CollectionKinds.ByName("Immutable")!
        };

        // Assert
        metadata.ServiceCategory.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AssemblyQualifiedName_IsNullByDefault()
    {
        // Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 9,
            Name = "TestCollection",
            FullName = "MyNamespace.TestCollection",
            CollectionKind = CollectionKinds.ByName("Immutable")!
        };

        // Assert
        metadata.AssemblyQualifiedName.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("Immutable")]
    [InlineData("Mutable")]
    [InlineData("Instance")]
    [InlineData("Service")]
    [InlineData("MutableService")]
    [InlineData("ServiceInstance")]
    public void ObjectInitializer_WithAllCollectionKinds_CreatesInstance(string kindName)
    {
        // Arrange & Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 10,
            Name = "TestCollection",
            FullName = "MyNamespace.TestCollection",
            CollectionKind = CollectionKinds.ByName(kindName)!
        };

        // Assert
        metadata.CollectionKind.Name.ShouldBe(kindName);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithCompleteData_CreatesInstance()
    {
        // Arrange
        var options = new List<TypeOptionMetadata>
        {
            new TypeOptionMetadata
            {
                Id = 1,
                Name = "TestOption",
                TypeCollectionId = 100,
                FullTypeName = "MyNamespace.TestOption"
            }
        };

        // Act
        var metadata = new TypeCollectionMetadata
        {
            Id = 100,
            Name = "CompleteCollection",
            FullName = "MyNamespace.CompleteCollection",
            CollectionKind = CollectionKinds.ByName("Service")!,
            ServiceCategory = "TestService",
            AssemblyQualifiedName = "MyNamespace.CompleteCollection, MyAssembly",
            Options = options
        };

        // Assert
        metadata.Id.ShouldBe(100);
        metadata.Name.ShouldBe("CompleteCollection");
        metadata.FullName.ShouldBe("MyNamespace.CompleteCollection");
        metadata.CollectionKind.Name.ShouldBe("Service");
        metadata.ServiceCategory.ShouldBe("TestService");
        metadata.AssemblyQualifiedName.ShouldBe("MyNamespace.CompleteCollection, MyAssembly");
        metadata.Options.Count.ShouldBe(1);
    }
}
