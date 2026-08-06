using System;
using Fdw.Data.Abstractions;
using Fdw.Data.JsonSchema;

namespace Fdw.Data.JsonSchema.Tests;

/// <summary>
/// Tests the actual conversion logic (ToClr/ToDb) of JSON Schema converters.
/// Collections are source-generated and don't need testing, but conversion logic does.
/// </summary>
public class JsonSchemaConverterLogicTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void IntegerInt32ConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new JsonSchemaIntegerInt32Converter();
        var dbValue = 42;

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(42);
        result.ShouldBeOfType<int>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void IntegerInt64ConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new JsonSchemaIntegerInt64Converter();
        var dbValue = 9223372036854775807L;

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(9223372036854775807L);
        result.ShouldBeOfType<long>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new JsonSchemaStringConverter();
        var dbValue = "test string";

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe("test string");
        result.ShouldBeOfType<string>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void BooleanConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new JsonSchemaBooleanConverter();
        var dbValue = true;

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(true);
        result.ShouldBeOfType<bool>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringDateTimeConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new JsonSchemaStringDateTimeConverter();
        var dbValue = new DateTime(2025, 1, 14, 10, 30, 0);

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(new DateTime(2025, 1, 14, 10, 30, 0));
        result.ShouldBeOfType<DateTime>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringDateTimeConverterShouldParseString()
    {
        // Arrange
        var converter = new JsonSchemaStringDateTimeConverter();
        var dbValue = "2025-01-14T10:30:00";

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBeOfType<DateTime>();
        ((DateTime)result!).Year.ShouldBe(2025);
        ((DateTime)result!).Month.ShouldBe(1);
        ((DateTime)result!).Day.ShouldBe(14);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringDateConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new JsonSchemaStringDateConverter();
        var dbValue = new DateTime(2025, 1, 14, 10, 30, 0);

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBeOfType<DateOnly>();
        ((DateOnly)result!).ShouldBe(new DateOnly(2025, 1, 14));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringUuidConverterShouldConvertGuid()
    {
        // Arrange
        var converter = new JsonSchemaStringUuidConverter();
        var guid = Guid.NewGuid();

        // Act
        var result = converter.ToClr(guid);

        // Assert
        result.ShouldBe(guid);
        result.ShouldBeOfType<Guid>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringUuidConverterShouldParseString()
    {
        // Arrange
        var converter = new JsonSchemaStringUuidConverter();
        var guidString = "123e4567-e89b-12d3-a456-426614174000";

        // Act
        var result = converter.ToClr(guidString);

        // Assert
        result.ShouldBeOfType<Guid>();
        result.ShouldBe(Guid.Parse(guidString));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NumberDecimalConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new JsonSchemaNumberDecimalConverter();
        var dbValue = 123.45m;

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(123.45m);
        result.ShouldBeOfType<decimal>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void AllConvertersShouldReturnNullForNullInput()
    {
        // Arrange
        var converters = new IDataTypeConverter[]
        {
            new JsonSchemaIntegerInt32Converter(),
            new JsonSchemaStringConverter(),
            new JsonSchemaBooleanConverter(),
            new JsonSchemaNumberDecimalConverter()
        };

        // Act & Assert
        foreach (var converter in converters)
        {
            var result = converter.ToClr(null);
            result.ShouldBeNull($"{converter.Name} should return null for null input");
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void AllConvertersShouldReturnNullForDBNull()
    {
        // Arrange
        var converters = new IDataTypeConverter[]
        {
            new JsonSchemaIntegerInt32Converter(),
            new JsonSchemaStringConverter(),
            new JsonSchemaBooleanConverter(),
            new JsonSchemaNumberDecimalConverter()
        };

        // Act & Assert
        foreach (var converter in converters)
        {
            var result = converter.ToClr(DBNull.Value);
            result.ShouldBeNull($"{converter.Name} should return null for DBNull");
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ToDbShouldPassThroughValues()
    {
        // Arrange
        var converter = new JsonSchemaIntegerInt32Converter();
        var clrValue = 42;

        // Act
        var result = converter.ToDb(clrValue);

        // Assert
        result.ShouldBe(42);
    }
}
