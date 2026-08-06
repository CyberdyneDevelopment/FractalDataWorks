using Fdw.Data.Abstractions;
using Fdw.Schema;
using Fdw.Schema.Indexes;
using Fdw.Schema.Keys;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Schema;

public sealed class ContainerSchemaTests
{
    private readonly IField _idField = new Field
    {
        Name = "Id",
        FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
        Role = PropertyRoles.Surrogate,
        IsNullable = false,
    };

    private readonly IField _nameField = new Field
    {
        Name = "Name",
        FieldType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) },
        Role = PropertyRoles.Attribute,
        IsNullable = false
    };

    private readonly IField _amountField = new Field
    {
        Name = "Amount",
        FieldType = new SimpleFieldType { TypeName = "decimal", ClrType = typeof(decimal) },
        Role = PropertyRoles.Measure,
        IsNullable = true
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void PropertiesReturnsFields()
    {
        // Arrange
        var fields = new[] { _idField, _nameField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var properties = schema.Properties;

        // Assert
        properties.ShouldBe(fields);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NameDefaultsToEmptyString()
    {
        // Arrange & Act
        var schema = new ContainerSchema { Fields = [] };

        // Assert
        schema.Name.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NameCanBeSet()
    {
        // Arrange & Act
        var schema = new ContainerSchema
        {
            Fields = [],
            Name = "TestSchema"
        };

        // Assert
        schema.Name.ShouldBe("TestSchema");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void DescriptionCanBeSet()
    {
        // Arrange & Act
        var schema = new ContainerSchema
        {
            Fields = [],
            Description = "Test description"
        };

        // Assert
        schema.Description.ShouldBe("Test description");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void LayoutDefaultsToTabular()
    {
        // Arrange & Act
        var schema = new ContainerSchema { Fields = [] };

        // Assert
        schema.Layout.ShouldNotBeNull();
        schema.Layout.Name.ShouldBe("Tabular");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void IndexesDefaultsToEmptyList()
    {
        // Arrange & Act
        var schema = new ContainerSchema { Fields = [] };

        // Assert
        schema.Indexes.ShouldNotBeNull();
        schema.Indexes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetIdentityFieldsReturnsKeyRoleFields()
    {
        // Arrange
        var fields = new[] { _idField, _nameField, _amountField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var identityFields = schema.GetIdentityFields();

        // Assert
        identityFields.Count.ShouldBe(1);
        identityFields[0].Name.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetAttributeFieldsReturnsNonKeyNonMeasureFields()
    {
        // Arrange
        var fields = new[] { _idField, _nameField, _amountField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var attributeFields = schema.GetAttributeFields();

        // Assert
        attributeFields.Count.ShouldBe(1);
        attributeFields[0].Name.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetMeasureFieldsReturnsAggregatableFields()
    {
        // Arrange
        var fields = new[] { _idField, _nameField, _amountField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var measureFields = schema.GetMeasureFields();

        // Assert
        measureFields.Count.ShouldBe(1);
        measureFields[0].Name.ShouldBe("Amount");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsCaseInsensitiveMatch()
    {
        // Arrange
        var fields = new[] { _idField, _nameField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var field = schema.Get("NAME");

        // Assert
        field.ShouldNotBeNull();
        field.Name.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsNullForUnknownField()
    {
        // Arrange
        var fields = new[] { _idField, _nameField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var field = schema.Get("Unknown");

        // Assert
        field.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsEmptyForNullRole()
    {
        // Arrange
        var fields = new[] { _idField, _nameField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var result = schema.Get((IPropertyRole)null!);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsIdentityFieldsForKeyRole()
    {
        // Arrange
        var fields = new[] { _idField, _nameField, _amountField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var result = schema.Get(PropertyRoles.Surrogate);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsMeasureFieldsForAggregatableRole()
    {
        // Arrange
        var fields = new[] { _idField, _nameField, _amountField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var result = schema.Get(PropertyRoles.Measure);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Amount");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsAttributeFieldsForAttributeRole()
    {
        // Arrange
        var fields = new[] { _idField, _nameField, _amountField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var result = schema.Get(PropertyRoles.Attribute);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsNestingReturnsTrueForNestedFieldTypes()
    {
        // Arrange
        var arrayField = new Field
        {
            Name = "Tags",
            FieldType = new ArrayFieldType { ElementType = new SimpleFieldType { TypeName = "string", ClrType = typeof(string) } },
            Role = PropertyRoles.Attribute,
            IsNullable = true
        };
        var fields = new[] { _idField, arrayField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var result = schema.SupportsNesting;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsNestingReturnsFalseForSimpleFieldTypes()
    {
        // Arrange
        var fields = new[] { _idField, _nameField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var result = schema.SupportsNesting;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SurrogateKeyCanBeSet()
    {
        // Arrange & Act
        var schema = new ContainerSchema
        {
            Fields = [],
            SurrogateKey = new KeyDefinition<IField>(Array.Empty<KeyMember>())
        };

        // Assert
        schema.SurrogateKey.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NaturalKeyCanBeSet()
    {
        // Arrange & Act
        var schema = new ContainerSchema
        {
            Fields = [],
            NaturalKey = new KeyDefinition<IField>(Array.Empty<KeyMember>())
        };

        // Assert
        schema.NaturalKey.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ChildrenCanBeSet()
    {
        // Arrange
        var childSchema = new ContainerSchema { Fields = [] };

        // Act
        var schema = new ContainerSchema
        {
            Fields = [],
            Children = new[] { childSchema }
        };

        // Assert
        schema.Children.ShouldNotBeNull();
        schema.Children.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void PathExpressionCanBeSet()
    {
        // Arrange & Act
        var schema = new ContainerSchema
        {
            Fields = [],
            PathExpression = "$.data.items[*]"
        };

        // Assert
        schema.PathExpression.ShouldBe("$.data.items[*]");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetFallsBackToExactNameMatch()
    {
        // Arrange - Create a custom role that's not key or aggregatable
        var lookupField = new Field
        {
            Name = "CategoryId",
            FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
            Role = PropertyRoles.Lookup,
            IsNullable = false
        };
        var fields = new[] { _idField, lookupField, _amountField };
        var schema = new ContainerSchema { Fields = fields };

        // Act
        var result = schema.Get(PropertyRoles.Lookup);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("CategoryId");
    }
}
