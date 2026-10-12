using Fdw.Data.Abstractions;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

/// <summary>
/// Tests to complete 100% coverage for DataContainers.Abstractions.
/// </summary>
public class CoverageCompletionTests
{
    #region DataSchema.ValidateRecord(IReadOnlyDictionary) Tests

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateRecordWithDictionaryReturnsSuccess()
    {
        // Arrange
        var field1 = new SchemaField("Id", typeof(int), 0);
        var field2 = new SchemaField("Name", typeof(string), 1);
        var fields = new List<ISchemaField> { field1, field2 };
        var schema = new DataSchema("test-id", "Test", "1.0", fields);

        IReadOnlyDictionary<string, object> record = new Dictionary<string, object>
        {
            ["Id"] = 1,
            ["Name"] = "Test"
        };

        // Act
        var result = schema.ValidateRecord(record);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateRecordWithEmptyDictionaryReturnsSuccess()
    {
        // Arrange
        var schema = DataSchema.Empty();
        IReadOnlyDictionary<string, object> record = new Dictionary<string, object>();

        // Act
        var result = schema.ValidateRecord(record);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region DataSchemaExtensions.GetOrdinal Tests (Extension Method Path)

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetOrdinalExtensionReturnsCorrectPositionForBasicSchema()
    {
        // Arrange - Create a minimal IDataSchema implementation that doesn't override GetOrdinal
        var mockSchema = new MinimalDataSchema(
            "id",
            "Test",
            "1.0",
            new List<ISchemaField>
            {
                new SchemaField("Field1", typeof(string), 0),
                new SchemaField("Field2", typeof(int), 1),
                new SchemaField("Field3", typeof(bool), 2)
            }
        );

        // Act - Call extension method directly
        var ordinal = DataSchemaExtensions.GetOrdinal(mockSchema, "Field2");

        // Assert
        ordinal.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetOrdinalExtensionThrowsForUnknownField()
    {
        // Arrange
        var mockSchema = new MinimalDataSchema(
            "id",
            "Test",
            "1.0",
            new List<ISchemaField>
            {
                new SchemaField("Field1", typeof(string), 0)
            }
        );

        // Act & Assert
        var ex = Should.Throw<KeyNotFoundException>(() =>
            DataSchemaExtensions.GetOrdinal(mockSchema, "Unknown"));
        ex.Message.ShouldContain("Field 'Unknown' not found");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetOrdinalExtensionIsCaseSensitive()
    {
        // Arrange
        var mockSchema = new MinimalDataSchema(
            "id",
            "Test",
            "1.0",
            new List<ISchemaField>
            {
                new SchemaField("Field1", typeof(string), 0)
            }
        );

        // Act & Assert
        Should.Throw<KeyNotFoundException>(() =>
            DataSchemaExtensions.GetOrdinal(mockSchema, "field1"));
    }

    #endregion

    #region DataRow.TryGetValue Type Conversion Success Tests

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetValueSucceedsWithTypeConversionFromIntToLong()
    {
        // Arrange
        var schema = DataSchema.FromFields(new[]
        {
            new SchemaField("Value", typeof(int), 0)
        });
        var row = new DataRow(schema, new object[] { 42 });

        // Act - Convert int to long (compatible conversion)
        var success = row.TryGetValue<long>(0, out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBe(42L);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetValueSucceedsWithTypeConversionFromStringToInt()
    {
        // Arrange
        var schema = DataSchema.FromFields(new[]
        {
            new SchemaField("Value", typeof(string), 0)
        });
        var row = new DataRow(schema, new object[] { "123" });

        // Act - Convert string to int
        var success = row.TryGetValue<int>(0, out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBe(123);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetValueSucceedsWithTypeConversionFromDoubleToDecimal()
    {
        // Arrange
        var schema = DataSchema.FromFields(new[]
        {
            new SchemaField("Value", typeof(double), 0)
        });
        var row = new DataRow(schema, new object[] { 123.45 });

        // Act - Convert double to decimal
        var success = row.TryGetValue<decimal>(0, out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBe(123.45m);
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Minimal IDataSchema implementation for testing extension methods.
    /// Does not override GetOrdinal/TryGetOrdinal to force extension method usage.
    /// </summary>
    private class MinimalDataSchema : IDataSchema
    {
        public MinimalDataSchema(string id, string name, string version, IReadOnlyList<ISchemaField> fields)
        {
            Id = id;
            Name = name;
            Version = version;
            Fields = fields;
            PrimaryKeyFields = Array.Empty<string>();
            Metadata = new Dictionary<string, object>();
        }

        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public IReadOnlyList<ISchemaField> Fields { get; }
        public IReadOnlyList<string> PrimaryKeyFields { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }

        public IReadOnlyList<string> FieldNames => Fields.Select(f => f.Name).ToList();

        public ISchemaField? GetField(string name) => Fields.FirstOrDefault(f => f.Name == name);

        public IEnumerable<ISchemaField> GetFields(IEnumerable<string> names) =>
            names.Select(GetField).Where(f => f != null)!;

        public IGenericResult ValidateRecord(IReadOnlyDictionary<string, object> record) =>
            GenericResult.Success();

        public IGenericResult ValidateRecord<T>(T record) where T : class =>
            GenericResult.Success();

        public IGenericResult CheckCompatibility(IDataSchema otherSchema, ISchemaCompatibilityMode compatibilityMode) =>
            GenericResult.Success();

        public IDataSchema ExtendWith(IEnumerable<ISchemaField> additionalFields) =>
            throw new NotImplementedException("Not needed for test");

        public IDataSchema ProjectTo(IEnumerable<string> fieldNames) =>
            throw new NotImplementedException("Not needed for test");

        public bool HasField(string fieldName) => Fields.Any(f => f.Name == fieldName);

        // NOTE: GetOrdinal and TryGetOrdinal are NOT implemented here
        // This forces the extension methods to be called
        public int GetOrdinal(string fieldName) =>
            throw new NotImplementedException("Use extension method instead");

        public bool TryGetOrdinal(string fieldName, out int ordinal) =>
            throw new NotImplementedException("Use extension method instead");
    }

    #endregion
}
