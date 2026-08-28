using Fdw.Data.Abstractions;
using Fdw.Schema;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Schema;

public sealed class FieldTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NameCanBeSet()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "TestField",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute
        };

        // Assert
        field.Name.ShouldBe("TestField");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FieldTypeCanBeSet()
    {
        // Arrange
        var fieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) };

        // Act
        var field = new Field
        {
            Name = "Id",
            FieldType = fieldType,
            Role = PropertyRoles.Surrogate
        };

        // Assert
        field.FieldType.ShouldBe(fieldType);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RoleCanBeSet()
    {
        // Arrange
        var role = PropertyRoles.Surrogate;

        // Act
        var field = new Field
        {
            Name = "Id",
            FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
            Role = role
        };

        // Assert
        field.Role.ShouldBe(role);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsNullableDefaultsToFalse()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Name",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute
        };

        // Assert
        field.IsNullable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsNullableCanBeSetToTrue()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Description",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute,
            IsNullable = true
        };

        // Assert
        field.IsNullable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsRequiredReturnsTrueWhenNotNullable()
    {
        // Arrange
        var field = new Field
        {
            Name = "Name",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute,
            IsNullable = false
        };

        // Act
        var result = field.IsRequired;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsRequiredReturnsFalseWhenNullable()
    {
        // Arrange
        var field = new Field
        {
            Name = "Description",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute,
            IsNullable = true
        };

        // Act
        var result = field.IsRequired;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void DescriptionCanBeSet()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Name",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute,
            Description = "The name of the entity"
        };

        // Assert
        field.Description.ShouldBe("The name of the entity");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void DescriptionDefaultsToNull()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Name",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute
        };

        // Assert
        field.Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MetadataCanBeSet()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["MaxLength"] = 100,
            ["Format"] = "email"
        };

        // Act
        var field = new Field
        {
            Name = "Email",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute,
            Metadata = metadata
        };

        // Assert
        field.Metadata.ShouldBe(metadata);
        field.Metadata!["MaxLength"].ShouldBe(100);
        field.Metadata["Format"].ShouldBe("email");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MetadataDefaultsToNull()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Name",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute
        };

        // Assert
        field.Metadata.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TypeSystemIdCanBeSet()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Id",
            FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
            Role = PropertyRoles.Surrogate,
            TypeSystemId = "MsSql"
        };

        // Assert
        field.TypeSystemId.ShouldBe("MsSql");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TypeSystemIdDefaultsToNull()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Name",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute
        };

        // Assert
        field.TypeSystemId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConverterTypeIdCanBeSet()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Id",
            FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
            Role = PropertyRoles.Surrogate,
            ConverterTypeId = 8
        };

        // Assert
        field.ConverterTypeId.ShouldBe(8);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConverterTypeIdDefaultsToNull()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Name",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute
        };

        // Assert
        field.ConverterTypeId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultRoleIsAttribute()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Name",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute
        };

        // Assert
        field.Role.Name.ShouldBe("Attribute");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SurrogateRoleIsPreservedOnField()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Id",
            FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
            Role = PropertyRoles.Surrogate
        };

        // Assert
        field.Role.Name.ShouldBe("Surrogate");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsIdentityDefaultsToFalse()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Id",
            FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
            Role = PropertyRoles.Surrogate
        };

        // Assert
        field.IsIdentity.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsIdentityCanBeSetToTrue()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Id",
            FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
            Role = PropertyRoles.Surrogate,
            IsIdentity = true
        };

        // Assert
        field.IsIdentity.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsComputedDefaultsToFalse()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Total",
            FieldType = new SimpleFieldType { TypeName = "decimal", ClrType = typeof(decimal) },
            Role = PropertyRoles.Measure
        };

        // Assert
        field.IsComputed.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IsComputedCanBeSetToTrue()
    {
        // Arrange & Act
        var field = new Field
        {
            Name = "Total",
            FieldType = new SimpleFieldType { TypeName = "decimal", ClrType = typeof(decimal) },
            Role = PropertyRoles.Measure,
            IsComputed = true
        };

        // Assert
        field.IsComputed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AllPropertiesCanBeSetTogether()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["MaxLength"] = 200,
            ["ComputedExpression"] = "FirstName + ' ' + LastName"
        };

        // Act
        var field = new Field
        {
            Name = "FullName",
            FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
            Role = PropertyRoles.Attribute,
            IsNullable = true,
            Description = "Computed full name",
            Metadata = metadata,
            TypeSystemId = "MsSql",
            ConverterTypeId = 12,
            IsIdentity = false,
            IsComputed = true
        };

        // Assert
        field.Name.ShouldBe("FullName");
        field.FieldType.TypeName.ShouldBe("string");
        field.Role.ShouldBe(PropertyRoles.Attribute);
        field.IsNullable.ShouldBeTrue();
        field.IsRequired.ShouldBeFalse();
        field.Description.ShouldBe("Computed full name");
        field.Metadata.ShouldBe(metadata);
        field.TypeSystemId.ShouldBe("MsSql");
        field.ConverterTypeId.ShouldBe(12);
        field.IsIdentity.ShouldBeFalse();
        field.IsComputed.ShouldBeTrue();
    }
}
