using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class DataSchemaEdgeCasesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldsReturnsEmptyForEmptyFieldNamesList()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var result = schema.GetFields(Array.Empty<string>()).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldsIgnoresNonExistentFieldNames()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var result = schema.GetFields(new[] { "Field1", "NonExistent" }).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Field1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExtendWithEmptyCollectionReturnsOriginalFields()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var extended = schema.ExtendWith(Array.Empty<ISchemaField>());

        // Assert
        extended.Fields.Count.ShouldBe(1);
        extended.Fields[0].Name.ShouldBe("Field1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExtendWithPreservesOriginalSchema()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);
        var newField = new SchemaField("Field2", typeof(int), 1);

        // Act
        var extended = schema.ExtendWith(new[] { newField });

        // Assert
        schema.Fields.Count.ShouldBe(1);
        extended.Fields.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ProjectToWithEmptyFieldNamesReturnsEmptySchema()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var projected = schema.ProjectTo(Array.Empty<string>());

        // Assert
        projected.Fields.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ProjectToWithNonExistentFieldsReturnsEmptySchema()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var projected = schema.ProjectTo(new[] { "NonExistent1", "NonExistent2" });

        // Assert
        projected.Fields.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ProjectToPreservesOriginalSchema()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var fields = new List<ISchemaField> { field1, field2 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act
        var projected = schema.ProjectTo(new[] { "Field1" });

        // Assert
        schema.Fields.Count.ShouldBe(2);
        projected.Fields.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldIsCaseSensitive()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act & Assert
        schema.HasField("Field1").ShouldBeTrue();
        schema.HasField("field1").ShouldBeFalse();
        schema.HasField("FIELD1").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetOrdinalIsCaseSensitive()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act & Assert
        schema.GetOrdinal("Field1").ShouldBe(0);
        Should.Throw<KeyNotFoundException>(() => schema.GetOrdinal("field1"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetOrdinalIsCaseSensitive()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema = new DataSchema("id", "Name", "1.0", fields);

        // Act & Assert
        schema.TryGetOrdinal("Field1", out var ordinal1).ShouldBeTrue();
        ordinal1.ShouldBe(0);

        schema.TryGetOrdinal("field1", out _).ShouldBeFalse();
        schema.TryGetOrdinal("FIELD1", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptySchemaHasNoFields()
    {
        // Act
        var schema = DataSchema.Empty();

        // Assert
        schema.Fields.Count.ShouldBe(0);
        schema.FieldNames.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptySchemaHasNoPrimaryKeyFields()
    {
        // Act
        var schema = DataSchema.Empty();

        // Assert
        schema.PrimaryKeyFields.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptySchemaHasNoMetadata()
    {
        // Act
        var schema = DataSchema.Empty();

        // Assert
        schema.Metadata.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FromFieldsGeneratesUniqueId()
    {
        // Arrange
        var fields = new List<ISchemaField>
        {
            new SchemaField("Field1", typeof(string), 0)
        };

        // Act
        var schema1 = DataSchema.FromFields(fields);
        var schema2 = DataSchema.FromFields(fields);

        // Assert
        schema1.Id.ShouldNotBe(schema2.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FromFieldsWithEmptyCollectionCreatesSchemaWithNoFields()
    {
        // Act
        var schema = DataSchema.FromFields(Array.Empty<ISchemaField>());

        // Assert
        schema.Fields.Count.ShouldBe(0);
        schema.Name.ShouldBe("DynamicSchema");
        schema.Version.ShouldBe("1.0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateRecordDictionaryReturnsSuccessForAnyInput()
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
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateRecordGenericReturnsSuccessForAnyInput()
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
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CheckCompatibilityReturnsSuccessForAnyInput()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var fields = new List<ISchemaField> { field1 };
        var schema1 = new DataSchema("id1", "Name", "1.0", fields);
        var schema2 = new DataSchema("id2", "Name", "2.0", fields);
        var mode = Moq.Mock.Of<ISchemaCompatibilityMode>();

        // Act
        var result = schema1.CheckCompatibility(schema2, mode);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
