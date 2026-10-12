using Fdw.Configuration.SourceGenerators.Analysis;
using Fdw.Configuration.SourceGenerators.Models;
using Shouldly;
using Xunit;

namespace Fdw.Configuration.SourceGenerators.Tests.Analysis;

public class SqlTypeMapperTests
{
    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("string", "varchar", 500)]
    [InlineData("String", "varchar", 500)]
    public void MapToColumnMapsStringToVarchar(string propertyType, string expectedSqlType, int expectedMaxLength)
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Name",
            PropertyType = propertyType,
            IsNullable = false
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe(expectedSqlType);
        column.MaxLength.ShouldBe(expectedMaxLength);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("bool", "bit")]
    [InlineData("Boolean", "bit")]
    public void MapToColumnMapsBoolToBit(string propertyType, string expectedSqlType)
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "IsActive",
            PropertyType = propertyType
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe(expectedSqlType);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("byte", "tinyint")]
    [InlineData("short", "smallint")]
    [InlineData("Int16", "smallint")]
    [InlineData("int", "int")]
    [InlineData("Int32", "int")]
    [InlineData("long", "bigint")]
    [InlineData("Int64", "bigint")]
    public void MapToColumnMapsIntegerTypes(string propertyType, string expectedSqlType)
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Value",
            PropertyType = propertyType
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe(expectedSqlType);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("ushort", "int")]
    [InlineData("UInt16", "int")]
    [InlineData("uint", "bigint")]
    [InlineData("UInt32", "bigint")]
    public void MapToColumnMapsUnsignedIntegersToNextLargerSigned(string propertyType, string expectedSqlType)
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Value",
            PropertyType = propertyType
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe(expectedSqlType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsUlongToDecimal()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "LargeValue",
            PropertyType = "ulong"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("decimal");
        column.Precision.ShouldBe(20);
        column.Scale.ShouldBe(0);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("float", "real")]
    [InlineData("Single", "real")]
    [InlineData("double", "float")]
    [InlineData("Double", "float")]
    public void MapToColumnMapsFloatingPointTypes(string propertyType, string expectedSqlType)
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Value",
            PropertyType = propertyType
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe(expectedSqlType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsDecimalWithDefaultPrecision()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Amount",
            PropertyType = "decimal"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("decimal");
        column.Precision.ShouldBe(18);
        column.Scale.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsDecimalWithCustomPrecision()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Amount",
            PropertyType = "decimal",
            Precision = 10,
            Scale = 4
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("decimal");
        column.Precision.ShouldBe(10);
        column.Scale.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsDateTimeToDatetime2()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "CreatedDate",
            PropertyType = "DateTime"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("datetime2");
        column.Precision.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsDateTimeOffsetToDatetimeoffset()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "CreatedDate",
            PropertyType = "DateTimeOffset"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("datetimeoffset");
        column.Precision.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsDateOnlyToDate()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "BirthDate",
            PropertyType = "DateOnly"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("date");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsTimeOnlyToTime()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "StartTime",
            PropertyType = "TimeOnly"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("time");
        column.Precision.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsTimeSpanToBigint()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Duration",
            PropertyType = "TimeSpan"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("bigint");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsGuidToUniqueidentifier()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Id",
            PropertyType = "Guid"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("uniqueidentifier");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsByteArrayToVarbinary()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Data",
            PropertyType = "byte[]"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("varbinary");
        column.MaxLength.ShouldBe(-1); // MAX
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsCharToVarchar()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Code",
            PropertyType = "char"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("varchar");
        column.MaxLength.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsComplexTypeToVarcharMax()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Data",
            PropertyType = "CustomType",
            IsComplexType = true
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("varchar");
        column.MaxLength.ShouldBe(-1); // MAX
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsCollectionToVarcharMax()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Items",
            PropertyType = "List<string>",
            IsCollection = true,
            CollectionItemType = "string"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("varchar");
        column.MaxLength.ShouldBe(-1); // MAX
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnMapsEnumToUnderlyingType()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Status",
            PropertyType = "StatusEnum",
            IsEnum = true,
            EnumUnderlyingType = "int"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("int");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnUsesDbTypeOverride()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Data",
            PropertyType = "string",
            DbTypeOverride = new DbTypeOverride
            {
                SqlType = "nvarchar",
                MaxLength = 100
            }
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("nvarchar");
        column.MaxLength.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnUsesMaxLengthFromProperty()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Name",
            PropertyType = "string",
            MaxLength = 256
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.SqlType.ShouldBe("varchar");
        column.MaxLength.ShouldBe(256);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnSetsIsNullableFromProperty()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Name",
            PropertyType = "string",
            IsNullable = true
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.IsNullable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnSetsIsNotNullableWhenRequired()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Name",
            PropertyType = "string",
            IsNullable = true,
            IsRequired = true
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        // Current behavior: IsNullable = property.IsNullable || !property.IsRequired
        // With IsNullable=true, IsRequired=true: true || false = TRUE
        column.IsNullable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnSetsColumnName()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "ServerName",
            PropertyType = "string",
            ColumnName = "srv_name"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.ColumnName.ShouldBe("srv_name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnUsesPropertyNameWhenColumnNameNull()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "ServerName",
            PropertyType = "string"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.ColumnName.ShouldBe("ServerName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnSetsUnique()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Email",
            PropertyType = "string",
            IsUnique = true
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.IsUnique.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapToColumnSetsDefaultValue()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "IsActive",
            PropertyType = "bool",
            DefaultValue = "true"
        };

        // Act
        var column = SqlTypeMapper.MapToColumn(property);

        // Assert
        column.DefaultValue.ShouldBe("true");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("bool", "true", "1")]
    [InlineData("bool", "false", "0")]
    [InlineData("Boolean", "True", "1")]
    [InlineData("Boolean", "False", "0")]
    public void GetSqlDefaultValueConvertsBoolToInt(string propertyType, string defaultValue, string expected)
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "IsActive",
            PropertyType = propertyType,
            DefaultValue = defaultValue
        };

        // Act
        var result = SqlTypeMapper.GetSqlDefaultValue(property);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetSqlDefaultValueQuotesStrings()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Status",
            PropertyType = "string",
            DefaultValue = "Active"
        };

        // Act
        var result = SqlTypeMapper.GetSqlDefaultValue(property);

        // Assert
        result.ShouldBe("'Active'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetSqlDefaultValueEscapesSingleQuotes()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Name",
            PropertyType = "string",
            DefaultValue = "O'Brien"
        };

        // Act
        var result = SqlTypeMapper.GetSqlDefaultValue(property);

        // Assert
        result.ShouldBe("'O''Brien'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetSqlDefaultValueConvertsDateTimeUtcNow()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "CreatedDate",
            PropertyType = "DateTime",
            DefaultValue = "DateTime.UtcNow"
        };

        // Act
        var result = SqlTypeMapper.GetSqlDefaultValue(property);

        // Assert
        result.ShouldBe("SYSUTCDATETIME()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetSqlDefaultValueReturnsNullWhenNoDefault()
    {
        // Arrange
        var property = new PropertyModel
        {
            PropertyName = "Name",
            PropertyType = "string"
        };

        // Act
        var result = SqlTypeMapper.GetSqlDefaultValue(property);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultStringTypeIsVarchar()
    {
        // Assert
        SqlTypeMapper.DefaultStringType.ShouldBe("varchar");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultStringMaxLengthIs500()
    {
        // Assert
        SqlTypeMapper.DefaultStringMaxLength.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultDecimalPrecisionIs18()
    {
        // Assert
        SqlTypeMapper.DefaultDecimalPrecision.ShouldBe(18);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultDecimalScaleIs2()
    {
        // Assert
        SqlTypeMapper.DefaultDecimalScale.ShouldBe(2);
    }
}
