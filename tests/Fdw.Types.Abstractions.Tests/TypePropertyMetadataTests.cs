using Fdw.Types;

namespace Fdw.Types.Abstractions.Tests;

/// <summary>
/// Tests for TypePropertyMetadata.
/// </summary>
public class TypePropertyMetadataTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithRequiredProperties_CreatesInstance()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "TestProperty",
            PropertyType = "System.String"
        };

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Name.ShouldBe("TestProperty");
        metadata.PropertyType.ShouldBe("System.String");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithPropertyRole_CreatesInstance()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "TestProperty",
            PropertyType = "System.Int32",
            PropertyRole = "Identifier"
        };

        // Assert
        metadata.PropertyRole.ShouldBe("Identifier");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithSqlType_CreatesInstance()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "TestProperty",
            PropertyType = "System.String",
            SqlType = "NVARCHAR(100)"
        };

        // Assert
        metadata.SqlType.ShouldBe("NVARCHAR(100)");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithMaxLength_CreatesInstance()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "Name",
            PropertyType = "System.String",
            MaxLength = 100
        };

        // Assert
        metadata.MaxLength.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithIsNullableTrue_CreatesInstance()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "OptionalProperty",
            PropertyType = "System.String",
            IsNullable = true
        };

        // Assert
        metadata.IsNullable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithIsNullableFalse_CreatesInstance()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "RequiredProperty",
            PropertyType = "System.String",
            IsNullable = false
        };

        // Assert
        metadata.IsNullable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithIsCollectionTrue_CreatesInstance()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "Items",
            PropertyType = "System.Collections.Generic.List`1",
            IsCollection = true
        };

        // Assert
        metadata.IsCollection.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithIsCollectionFalse_CreatesInstance()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "SingleValue",
            PropertyType = "System.String",
            IsCollection = false
        };

        // Assert
        metadata.IsCollection.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void PropertyRole_IsNullByDefault()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "TestProperty",
            PropertyType = "System.String"
        };

        // Assert
        metadata.PropertyRole.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SqlType_IsNullByDefault()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "TestProperty",
            PropertyType = "System.String"
        };

        // Assert
        metadata.SqlType.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MaxLength_IsNullByDefault()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "TestProperty",
            PropertyType = "System.String"
        };

        // Assert
        metadata.MaxLength.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IsNullable_IsFalseByDefault()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "TestProperty",
            PropertyType = "System.String"
        };

        // Assert
        metadata.IsNullable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IsCollection_IsFalseByDefault()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "TestProperty",
            PropertyType = "System.String"
        };

        // Assert
        metadata.IsCollection.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("System.String", "VARCHAR(MAX)")]
    [InlineData("System.Int32", "INT")]
    [InlineData("System.DateTime", "DATETIME2")]
    [InlineData("System.Boolean", "BIT")]
    [InlineData("System.Decimal", "DECIMAL(18,2)")]
    public void ObjectInitializer_WithCommonSqlTypes_CreatesInstance(string propertyType, string sqlType)
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "TestProperty",
            PropertyType = propertyType,
            SqlType = sqlType
        };

        // Assert
        metadata.PropertyType.ShouldBe(propertyType);
        metadata.SqlType.ShouldBe(sqlType);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    [InlineData(1000)]
    public void ObjectInitializer_WithVariousMaxLengths_CreatesInstance(int maxLength)
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "TestProperty",
            PropertyType = "System.String",
            MaxLength = maxLength
        };

        // Assert
        metadata.MaxLength.ShouldBe(maxLength);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithCompleteData_CreatesInstance()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "CompleteProperty",
            PropertyType = "System.String",
            PropertyRole = "Description",
            SqlType = "NVARCHAR(500)",
            MaxLength = 500,
            IsNullable = true,
            IsCollection = false
        };

        // Assert
        metadata.Name.ShouldBe("CompleteProperty");
        metadata.PropertyType.ShouldBe("System.String");
        metadata.PropertyRole.ShouldBe("Description");
        metadata.SqlType.ShouldBe("NVARCHAR(500)");
        metadata.MaxLength.ShouldBe(500);
        metadata.IsNullable.ShouldBeTrue();
        metadata.IsCollection.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ObjectInitializer_WithCollectionProperty_CreatesInstance()
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = "Tags",
            PropertyType = "System.Collections.Generic.IReadOnlyList`1[[System.String]]",
            IsCollection = true,
            IsNullable = false
        };

        // Assert
        metadata.Name.ShouldBe("Tags");
        metadata.IsCollection.ShouldBeTrue();
        metadata.IsNullable.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("Id", "Identifier")]
    [InlineData("Name", "Label")]
    [InlineData("Description", "Description")]
    [InlineData("Value", "Data")]
    [InlineData("Category", "Grouping")]
    public void ObjectInitializer_WithCommonPropertyRoles_CreatesInstance(string propertyName, string propertyRole)
    {
        // Act
        var metadata = new TypePropertyMetadata
        {
            Name = propertyName,
            PropertyType = "System.String",
            PropertyRole = propertyRole
        };

        // Assert
        metadata.Name.ShouldBe(propertyName);
        metadata.PropertyRole.ShouldBe(propertyRole);
    }
}
