using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.FieldTypes;

public sealed class SimpleFieldTypeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var fieldType = new SimpleFieldType
        {
            TypeName = "String",
            ClrType = typeof(string)
        };

        // Assert
        fieldType.TypeName.ShouldBe("String");
        fieldType.ClrType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsNestedReturnsFalse()
    {
        // Arrange
        var fieldType = new SimpleFieldType
        {
            TypeName = "Integer",
            ClrType = typeof(int)
        };

        // Act & Assert
        fieldType.IsNested.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsISimpleFieldTypeInterface()
    {
        // Arrange
        var fieldType = new SimpleFieldType
        {
            TypeName = "Boolean",
            ClrType = typeof(bool)
        };

        // Act & Assert
        fieldType.ShouldBeAssignableTo<ISimpleFieldType>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIFieldTypeInterface()
    {
        // Arrange
        var fieldType = new SimpleFieldType
        {
            TypeName = "Double",
            ClrType = typeof(double)
        };

        // Act & Assert
        fieldType.ShouldBeAssignableTo<IFieldType>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithValueType()
    {
        // Arrange & Act
        var fieldType = new SimpleFieldType
        {
            TypeName = "DateTime",
            ClrType = typeof(DateTime)
        };

        // Assert
        fieldType.ClrType.ShouldBe(typeof(DateTime));
        fieldType.ClrType.IsValueType.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithReferenceType()
    {
        // Arrange & Act
        var fieldType = new SimpleFieldType
        {
            TypeName = "String",
            ClrType = typeof(string)
        };

        // Assert
        fieldType.ClrType.ShouldBe(typeof(string));
        fieldType.ClrType.IsValueType.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithNullableType()
    {
        // Arrange & Act
        var fieldType = new SimpleFieldType
        {
            TypeName = "Int32?",
            ClrType = typeof(int?)
        };

        // Assert
        fieldType.ClrType.ShouldBe(typeof(int?));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsLongTypeName()
    {
        // Arrange & Act
        var fieldType = new SimpleFieldType
        {
            TypeName = "System.Collections.Generic.List`1[System.String]",
            ClrType = typeof(List<string>)
        };

        // Assert
        fieldType.TypeName.ShouldContain("System");
        fieldType.TypeName.ShouldContain("List");
    }
}
