using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.FieldTypes;

public sealed class ArrayFieldTypeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        var elementType = new SimpleFieldType { TypeName = "String", ClrType = typeof(string) };

        // Act
        var arrayType = new ArrayFieldType { ElementType = elementType };

        // Assert
        arrayType.ElementType.ShouldBe(elementType);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TypeNameIsFormattedCorrectly()
    {
        // Arrange
        var elementType = new SimpleFieldType { TypeName = "Integer", ClrType = typeof(int) };

        // Act
        var arrayType = new ArrayFieldType { ElementType = elementType };

        // Assert
        arrayType.TypeName.ShouldBe("Array<Integer>");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TypeNameUpdatesWhenElementTypeChanges()
    {
        // Arrange
        var elementType1 = new SimpleFieldType { TypeName = "String", ClrType = typeof(string) };
        var elementType2 = new SimpleFieldType { TypeName = "Integer", ClrType = typeof(int) };

        var arrayType1 = new ArrayFieldType { ElementType = elementType1 };
        var arrayType2 = new ArrayFieldType { ElementType = elementType2 };

        // Act & Assert
        arrayType1.TypeName.ShouldBe("Array<String>");
        arrayType2.TypeName.ShouldBe("Array<Integer>");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ClrTypeReturnsArrayType()
    {
        // Arrange
        var elementType = new SimpleFieldType { TypeName = "String", ClrType = typeof(string) };

        // Act
        var arrayType = new ArrayFieldType { ElementType = elementType };

        // Assert
        arrayType.ClrType.ShouldBe(typeof(Array));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsNestedReturnsTrue()
    {
        // Arrange
        var elementType = new SimpleFieldType { TypeName = "Integer", ClrType = typeof(int) };

        // Act
        var arrayType = new ArrayFieldType { ElementType = elementType };

        // Assert
        arrayType.IsNested.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIArrayFieldTypeInterface()
    {
        // Arrange
        var elementType = new SimpleFieldType { TypeName = "Boolean", ClrType = typeof(bool) };

        // Act
        var arrayType = new ArrayFieldType { ElementType = elementType };

        // Assert
        arrayType.ShouldBeAssignableTo<IArrayFieldType>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIFieldTypeInterface()
    {
        // Arrange
        var elementType = new SimpleFieldType { TypeName = "Double", ClrType = typeof(double) };

        // Act
        var arrayType = new ArrayFieldType { ElementType = elementType };

        // Assert
        arrayType.ShouldBeAssignableTo<IFieldType>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateNestedArrayType()
    {
        // Arrange
        var elementType = new SimpleFieldType { TypeName = "Integer", ClrType = typeof(int) };
        var innerArrayType = new ArrayFieldType { ElementType = elementType };

        // Act
        var outerArrayType = new ArrayFieldType { ElementType = innerArrayType };

        // Assert
        outerArrayType.ElementType.ShouldBe(innerArrayType);
        outerArrayType.TypeName.ShouldBe("Array<Array<Integer>>");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateArrayOfComplexType()
    {
        // Arrange
        var complexType = new SimpleFieldType { TypeName = "CustomObject", ClrType = typeof(object) };

        // Act
        var arrayType = new ArrayFieldType { ElementType = complexType };

        // Assert
        arrayType.TypeName.ShouldBe("Array<CustomObject>");
        arrayType.ElementType.TypeName.ShouldBe("CustomObject");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TypeNameHandlesLongElementTypeNames()
    {
        // Arrange
        var elementType = new SimpleFieldType
        {
            TypeName = "VeryLongComplexTypeName",
            ClrType = typeof(string)
        };

        // Act
        var arrayType = new ArrayFieldType { ElementType = elementType };

        // Assert
        arrayType.TypeName.ShouldBe("Array<VeryLongComplexTypeName>");
    }
}
