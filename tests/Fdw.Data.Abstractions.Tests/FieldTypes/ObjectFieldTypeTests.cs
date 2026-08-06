using Fdw.Data.Abstractions;
using Fdw.Schema;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.FieldTypes;

public sealed class ObjectFieldTypeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        var fields = new List<IField>
        {
            new Field
            {
                Name = "Name",
                FieldType = new SimpleFieldType { TypeName = "String", ClrType = typeof(string) },
                Role = PropertyRoles.Attribute
            }
        };

        // Act
        var objectType = new ObjectFieldType
        {
            TypeName = "Person",
            Fields = fields,
            ClrType = typeof(object)
        };

        // Assert
        objectType.TypeName.ShouldBe("Person");
        objectType.Fields.ShouldBe(fields);
        objectType.ClrType.ShouldBe(typeof(object));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsNestedReturnsTrue()
    {
        // Arrange
        var objectType = new ObjectFieldType
        {
            TypeName = "Address",
            Fields = Array.Empty<IField>(),
            ClrType = typeof(object)
        };

        // Act & Assert
        objectType.IsNested.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIObjectFieldTypeInterface()
    {
        // Arrange
        var objectType = new ObjectFieldType
        {
            TypeName = "User",
            Fields = Array.Empty<IField>(),
            ClrType = typeof(object)
        };

        // Act & Assert
        objectType.ShouldBeAssignableTo<IObjectFieldType>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIFieldTypeInterface()
    {
        // Arrange
        var objectType = new ObjectFieldType
        {
            TypeName = "Product",
            Fields = Array.Empty<IField>(),
            ClrType = typeof(object)
        };

        // Act & Assert
        objectType.ShouldBeAssignableTo<IFieldType>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithEmptyFields()
    {
        // Arrange & Act
        var objectType = new ObjectFieldType
        {
            TypeName = "EmptyObject",
            Fields = Array.Empty<IField>(),
            ClrType = typeof(object)
        };

        // Assert
        objectType.Fields.ShouldBeEmpty();
        objectType.TypeName.ShouldBe("EmptyObject");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithMultipleFields()
    {
        // Arrange
        var fields = new List<IField>
        {
            new Field
            {
                Name = "Id",
                FieldType = new SimpleFieldType { TypeName = "Integer", ClrType = typeof(int) },
                Role = PropertyRoles.Surrogate
            },
            new Field
            {
                Name = "Name",
                FieldType = new SimpleFieldType { TypeName = "String", ClrType = typeof(string) },
                Role = PropertyRoles.Attribute
            },
            new Field
            {
                Name = "Active",
                FieldType = new SimpleFieldType { TypeName = "Boolean", ClrType = typeof(bool) },
                Role = PropertyRoles.Attribute
            }
        };

        // Act
        var objectType = new ObjectFieldType
        {
            TypeName = "Customer",
            Fields = fields,
            ClrType = typeof(object)
        };

        // Assert
        objectType.Fields.Count.ShouldBe(3);
        objectType.Fields[0].Name.ShouldBe("Id");
        objectType.Fields[1].Name.ShouldBe("Name");
        objectType.Fields[2].Name.ShouldBe("Active");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateNestedObjectType()
    {
        // Arrange
        var addressFields = new List<IField>
        {
            new Field
            {
                Name = "Street",
                FieldType = new SimpleFieldType { TypeName = "String", ClrType = typeof(string) },
                Role = PropertyRoles.Attribute
            }
        };

        var addressType = new ObjectFieldType
        {
            TypeName = "Address",
            Fields = addressFields,
            ClrType = typeof(object)
        };

        var personFields = new List<IField>
        {
            new Field
            {
                Name = "Name",
                FieldType = new SimpleFieldType { TypeName = "String", ClrType = typeof(string) },
                Role = PropertyRoles.Attribute
            },
            new Field
            {
                Name = "Address",
                FieldType = addressType,
                Role = PropertyRoles.Attribute
            }
        };

        // Act
        var personType = new ObjectFieldType
        {
            TypeName = "Person",
            Fields = personFields,
            ClrType = typeof(object)
        };

        // Assert
        personType.Fields.Count.ShouldBe(2);
        personType.Fields[1].FieldType.ShouldBeOfType<ObjectFieldType>();
        ((IObjectFieldType)personType.Fields[1].FieldType).TypeName.ShouldBe("Address");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsLongTypeName()
    {
        // Arrange & Act
        var objectType = new ObjectFieldType
        {
            TypeName = "VeryLongComplexObjectTypeName",
            Fields = Array.Empty<IField>(),
            ClrType = typeof(object)
        };

        // Assert
        objectType.TypeName.ShouldBe("VeryLongComplexObjectTypeName");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithDifferentClrTypes()
    {
        // Arrange & Act
        var objectType1 = new ObjectFieldType
        {
            TypeName = "Type1",
            Fields = Array.Empty<IField>(),
            ClrType = typeof(string)
        };

        var objectType2 = new ObjectFieldType
        {
            TypeName = "Type2",
            Fields = Array.Empty<IField>(),
            ClrType = typeof(int)
        };

        // Assert
        objectType1.ClrType.ShouldBe(typeof(string));
        objectType2.ClrType.ShouldBe(typeof(int));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FieldsIsReadOnlyList()
    {
        // Arrange
        var fields = new List<IField>
        {
            new Field
            {
                Name = "TestField",
                FieldType = new SimpleFieldType { TypeName = "String", ClrType = typeof(string) },
                Role = PropertyRoles.Attribute
            }
        };

        var objectType = new ObjectFieldType
        {
            TypeName = "TestObject",
            Fields = fields,
            ClrType = typeof(object)
        };

        // Act & Assert
        objectType.Fields.ShouldBeAssignableTo<IReadOnlyList<IField>>();
    }
}
