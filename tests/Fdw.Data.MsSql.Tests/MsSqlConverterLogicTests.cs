using System;
using System.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;

namespace Fdw.Data.MsSql.Tests;

/// <summary>
/// Tests the actual conversion logic (ToClr/ToDb) of MS SQL converters.
/// Collections are source-generated and don't need testing, but conversion logic does.
/// </summary>
public class MsSqlConverterLogicTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Int32ConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new MsSqlInt32Converter();
        var dbValue = 42;

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(42);
        result.ShouldBeOfType<int>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Int64ConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new MsSqlInt64Converter();
        var dbValue = 9223372036854775807L;

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(9223372036854775807L);
        result.ShouldBeOfType<long>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void StringConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new MsSqlStringConverter();
        var dbValue = "test string";

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe("test string");
        result.ShouldBeOfType<string>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BooleanConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new MsSqlBooleanConverter();
        var dbValue = true;

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(true);
        result.ShouldBeOfType<bool>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DateTimeConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new MsSqlDateTimeConverter();
        var dbValue = new DateTime(2025, 1, 14, 10, 30, 0);

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(new DateTime(2025, 1, 14, 10, 30, 0));
        result.ShouldBeOfType<DateTime>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DateTimeOffsetConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new MsSqlDateTimeOffsetConverter();
        var dbValue = new DateTimeOffset(2025, 1, 14, 10, 30, 0, TimeSpan.Zero);

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(new DateTimeOffset(2025, 1, 14, 10, 30, 0, TimeSpan.Zero));
        result.ShouldBeOfType<DateTimeOffset>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecimalConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new MsSqlDecimalConverter();
        var dbValue = 123.45m;

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(123.45m);
        result.ShouldBeOfType<decimal>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FloatConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new MsSqlFloatConverter();
        var dbValue = 123.45;

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(123.45);
        result.ShouldBeOfType<double>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GuidConverterShouldConvertGuid()
    {
        // Arrange
        var converter = new MsSqlGuidConverter();
        var guid = Guid.NewGuid();

        // Act
        var result = converter.ToClr(guid);

        // Assert
        result.ShouldBe(guid);
        result.ShouldBeOfType<Guid>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GuidConverterShouldConvertByteArray()
    {
        // Arrange
        var converter = new MsSqlGuidConverter();
        var guid = Guid.NewGuid();
        var bytes = guid.ToByteArray();

        // Act
        var result = converter.ToClr(bytes);

        // Assert
        result.ShouldBe(guid);
        result.ShouldBeOfType<Guid>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByteArrayConverterShouldConvertToClr()
    {
        // Arrange
        var converter = new MsSqlByteArrayConverter();
        var dbValue = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = converter.ToClr(dbValue);

        // Assert
        result.ShouldBe(dbValue);
        result.ShouldBeOfType<byte[]>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllConvertersShouldReturnNullForNullInput()
    {
        // Arrange
        var converters = new IDataTypeConverter[]
        {
            new MsSqlInt32Converter(),
            new MsSqlStringConverter(),
            new MsSqlBooleanConverter(),
            new MsSqlDecimalConverter()
        };

        // Act & Assert
        foreach (var converter in converters)
        {
            var result = converter.ToClr(null);
            result.ShouldBeNull($"{converter.Name} should return null for null input");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllConvertersShouldReturnNullForDBNull()
    {
        // Arrange
        var converters = new IDataTypeConverter[]
        {
            new MsSqlInt32Converter(),
            new MsSqlStringConverter(),
            new MsSqlBooleanConverter(),
            new MsSqlDecimalConverter()
        };

        // Act & Assert
        foreach (var converter in converters)
        {
            var result = converter.ToClr(DBNull.Value);
            result.ShouldBeNull($"{converter.Name} should return null for DBNull");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDbShouldPassThroughValues()
    {
        // Arrange
        var converter = new MsSqlInt32Converter();
        var clrValue = 42;

        // Act
        var result = converter.ToDb(clrValue);

        // Assert
        result.ShouldBe(42);
    }
}
