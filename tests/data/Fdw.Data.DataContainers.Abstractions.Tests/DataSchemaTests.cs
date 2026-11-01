using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class DataSchemaTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var fields = new List<ISchemaField> { field1, field2 };

        // Act
        var schema = new DataSchema("schema-id", "TestSchema", "1.0", fields);

        // Assert
        schema.Id.ShouldBe("schema-id");
        schema.Name.ShouldBe("TestSchema");
        schema.Version.ShouldBe("1.0");
        schema.Fields.Count.ShouldBe(2);
        schema.PrimaryKeyFields.ShouldBeEmpty();
        schema.Metadata.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenIdIsNull()
    {
        // Arrange
        var fields = new List<ISchemaField>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DataSchema(null!, "Name", "1.0", fields));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsNull()
    {
        // Arrange
        var fields = new List<ISchemaField>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DataSchema("id", null!, "1.0", fields));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenVersionIsNull()
    {
        // Arrange
        var fields = new List<ISchemaField>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DataSchema("id", "Name", null!, fields));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenFieldsIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DataSchema("id", "Name", "1.0", null!));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FieldNamesReturnsAllFieldNames()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var fields = new List<ISchemaField> { field1, field2 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var names = schema.FieldNames;

        // Assert
        names.Count.ShouldBe(2);
        names[0].ShouldBe("Field1");
        names[1].ShouldBe("Field2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldReturnsCorrectField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var fields = new List<ISchemaField> { field1, field2 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var result = schema.GetField("Field2");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Field2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldReturnsNullForUnknownField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var result = schema.GetField("Unknown");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldIsCaseSensitive()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var result = schema.GetField("field1");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldsReturnsMatchingFields()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var field3 = new SchemaField("Field3", typeof(bool), 2);
        var fields = new List<ISchemaField> { field1, field2, field3 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var result = schema.GetFields(new[] { "Field1", "Field3" }).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Field1");
        result[1].Name.ShouldBe("Field3");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateRecordReturnsSuccess()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);
        var record = new Dictionary<string, object> { ["Field1"] = "value" };

        // Act
        var result = schema.ValidateRecord(record);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateRecordGenericReturnsSuccess()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);
        var record = new { Field1 = "value" };

        // Act
        var result = schema.ValidateRecord(record);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CheckCompatibilityReturnsSuccess()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema1 = new DataSchema("id1", "Name", "1.0", fields);
        var schema2 = new DataSchema("id2", "Name", "1.0", fields);
        var mode = Mock.Of<ISchemaCompatibilityMode>();

        // Act
        var result = schema1.CheckCompatibility(schema2, mode);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ExtendWithAddsNewFields()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);
        var newField = new SchemaField("Field2", typeof(int), 1);

        // Act
        var extended = schema.ExtendWith(new[] { newField });

        // Assert
        extended.Fields.Count.ShouldBe(2);
        extended.Fields[0].Name.ShouldBe("Field1");
        extended.Fields[1].Name.ShouldBe("Field2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ProjectToReturnsSchemaWithSelectedFields()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var field3 = new SchemaField("Field3", typeof(bool), 2);
        var fields = new List<ISchemaField> { field1, field2, field3 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var projected = schema.ProjectTo(new[] { "Field1", "Field3" });

        // Assert
        projected.Fields.Count.ShouldBe(2);
        projected.Fields[0].Name.ShouldBe("Field1");
        projected.Fields[1].Name.ShouldBe("Field3");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldReturnsTrueForExistingField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var result = schema.HasField("Field1");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldReturnsFalseForNonExistingField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var result = schema.HasField("Unknown");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetOrdinalReturnsCorrectPosition()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var fields = new List<ISchemaField> { field1, field2 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var ordinal = schema.GetOrdinal("Field2");

        // Assert
        ordinal.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetOrdinalThrowsForUnknownField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act & Assert
        var ex = Should.Throw<KeyNotFoundException>(() => schema.GetOrdinal("Unknown"));
        ex.Message.ShouldContain("Field 'Unknown' not found");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetOrdinalReturnsTrueForExistingField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var result = schema.TryGetOrdinal("Field1", out var ordinal);

        // Assert
        result.ShouldBeTrue();
        ordinal.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetOrdinalReturnsFalseForUnknownField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var result = schema.TryGetOrdinal("Unknown", out int ordinal);

        // Assert
        result.ShouldBeFalse();
        // Dictionary.TryGetValue sets out parameter to default(int) which is 0, not -1
        ordinal.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyCreatesEmptySchema()
    {
        // Act
        var schema = DataSchema.Empty();

        // Assert
        schema.Id.ShouldBe("empty");
        schema.Name.ShouldBe("Empty");
        schema.Version.ShouldBe("1.0");
        schema.Fields.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FromFieldsCreatesSchemaFromFields()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var fields = new List<ISchemaField> { field1, field2 };

        // Act
        var schema = DataSchema.FromFields(fields);

        // Assert
        schema.Name.ShouldBe("DynamicSchema");
        schema.Version.ShouldBe("1.0");
        schema.Fields.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateRecordReturnsSuccessWithDictionary()
    {
        // Arrange
        var field1 = new SchemaField("Id", typeof(int), 0);
        var field2 = new SchemaField("Name", typeof(string), 1);
        var fields = new List<ISchemaField> { field1, field2 };
        var schema = new DataSchema("test-id", "Test", "1.0", fields);
        var record = new Dictionary<string, object>
        {
            ["Id"] = 1,
            ["Name"] = "Test"
        };

        // Act
        var result = schema.ValidateRecord(record);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
