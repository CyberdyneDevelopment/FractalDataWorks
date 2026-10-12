using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class DataSchemaExtensionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FieldNamesReturnsAllFieldNames()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var schema = new DataSchema("id", "Test", "1.0", new[] { field1, field2 });

        // Act
        var names = schema.FieldNames();

        // Assert
        names.Count.ShouldBe(2);
        names[0].ShouldBe("Field1");
        names[1].ShouldBe("Field2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetOrdinalReturnsCorrectPosition()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var schema = new DataSchema("id", "Test", "1.0", new[] { field1, field2 });

        // Act
        var ordinal = schema.GetOrdinal("Field2");

        // Assert
        ordinal.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetOrdinalThrowsForUnknownField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var schema = new DataSchema("id", "Test", "1.0", new[] { field1 });

        // Act & Assert
        var ex = Should.Throw<KeyNotFoundException>(() => schema.GetOrdinal("Unknown"));
        ex.Message.ShouldContain("Field 'Unknown' not found");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetOrdinalIsCaseSensitive()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var schema = new DataSchema("id", "Test", "1.0", new[] { field1 });

        // Act & Assert
        Should.Throw<KeyNotFoundException>(() => schema.GetOrdinal("field1"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetOrdinalReturnsTrueForExistingField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var field2 = new SchemaField("Field2", typeof(int), 1);
        var schema = new DataSchema("id", "Test", "1.0", new[] { field1, field2 });

        // Act
        var result = schema.TryGetOrdinal("Field2", out var ordinal);

        // Assert
        result.ShouldBeTrue();
        ordinal.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetOrdinalReturnsFalseForUnknownField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var schema = new DataSchema("id", "Test", "1.0", new[] { field1 });

        // Act
        var result = DataSchemaExtensions.TryGetOrdinal(schema, "Unknown", out int ordinal);

        // Assert
        result.ShouldBeFalse();
        ordinal.ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetOrdinalIsCaseSensitive()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var schema = new DataSchema("id", "Test", "1.0", new[] { field1 });

        // Act
        var result = schema.TryGetOrdinal("field1", out var ordinal);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldReturnsTrueForExistingField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var schema = new DataSchema("id", "Test", "1.0", new[] { field1 });

        // Act
        var result = schema.HasField("Field1");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldReturnsFalseForNonExistingField()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var schema = new DataSchema("id", "Test", "1.0", new[] { field1 });

        // Act
        var result = schema.HasField("Unknown");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldIsCaseSensitive()
    {
        // Arrange
        var field1 = new SchemaField("Field1", typeof(string), 0);
        var schema = new DataSchema("id", "Test", "1.0", new[] { field1 });

        // Act
        var result = schema.HasField("field1");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FieldNamesReturnsEmptyListForEmptySchema()
    {
        // Arrange
        var schema = DataSchema.Empty();

        // Act
        var names = schema.FieldNames();

        // Assert
        names.ShouldBeEmpty();
    }
}
