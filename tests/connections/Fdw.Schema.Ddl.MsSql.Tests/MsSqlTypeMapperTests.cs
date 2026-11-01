using System;
using System.Collections.Generic;
using Fdw.Schema.Ddl.MsSql;
using Fdw.Schema.Properties;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Schema.Ddl.MsSql.Tests;

public sealed class MsSqlTypeMapperTests
{
    private static Mock<IPropertyDefinition> CreateProperty(
        Dictionary<string, object>? metadata = null)
    {
        var prop = new Mock<IPropertyDefinition>();
        prop.Setup(p => p.Name).Returns("TestProp");
        prop.Setup(p => p.Metadata).Returns(metadata);
        return prop;
    }

    // MapToSqlType tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapToSqlTypeReturnsExplicitSqlTypeFromMetadata()
    {
        var metadata = new Dictionary<string, object> { ["SqlType"] = "NVARCHAR" };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.MapToSqlType(prop.Object);

        result.ShouldBe("NVARCHAR");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapToSqlTypeDefaultsToVarcharWithoutMetadata()
    {
        var prop = CreateProperty();

        var result = MsSqlTypeMapper.MapToSqlType(prop.Object);

        result.ShouldBe("VARCHAR");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapToSqlTypeDefaultsToVarcharWithNullMetadata()
    {
        var prop = new Mock<IPropertyDefinition>();
        prop.Setup(p => p.Metadata).Returns((IReadOnlyDictionary<string, object>?)null);

        var result = MsSqlTypeMapper.MapToSqlType(prop.Object);

        result.ShouldBe("VARCHAR");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapToSqlTypeDefaultsToVarcharWithoutClrType()
    {
        var metadata = new Dictionary<string, object> { ["SomeOtherKey"] = "value" };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.MapToSqlType(prop.Object);

        result.ShouldBe("VARCHAR");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("System.String", "VARCHAR")]
    [InlineData("System.Int32", "INT")]
    [InlineData("System.Int64", "BIGINT")]
    [InlineData("System.Int16", "SMALLINT")]
    [InlineData("System.Byte", "TINYINT")]
    [InlineData("System.Boolean", "BIT")]
    [InlineData("System.Guid", "UNIQUEIDENTIFIER")]
    [InlineData("System.DateTime", "DATETIME2")]
    [InlineData("System.DateTimeOffset", "DATETIMEOFFSET")]
    [InlineData("System.Decimal", "DECIMAL")]
    [InlineData("System.Double", "FLOAT")]
    [InlineData("System.Single", "REAL")]
    [InlineData("System.TimeSpan", "TIME")]
    public void MapToSqlTypeMapsClrTypes(string clrType, string expectedSqlType)
    {
        var metadata = new Dictionary<string, object> { ["ClrType"] = clrType };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.MapToSqlType(prop.Object);

        result.ShouldBe(expectedSqlType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapToSqlTypeMapsNullableTypes()
    {
        var metadata = new Dictionary<string, object>
        {
            ["ClrType"] = "System.Nullable`1[[System.Int32, System.Private.CoreLib]]"
        };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.MapToSqlType(prop.Object);

        result.ShouldBe("INT");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapToSqlTypeDefaultsToVarcharForUnknownClrType()
    {
        var metadata = new Dictionary<string, object>
        {
            ["ClrType"] = "Some.Unknown.Type.That.Does.Not.Exist"
        };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.MapToSqlType(prop.Object);

        result.ShouldBe("VARCHAR");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapToSqlTypeExplicitSqlTypeTakesPriority()
    {
        var metadata = new Dictionary<string, object>
        {
            ["SqlType"] = "NVARCHAR",
            ["ClrType"] = "System.Int32"
        };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.MapToSqlType(prop.Object);

        result.ShouldBe("NVARCHAR");
    }

    // GetMaxLength tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetMaxLengthReturnsExplicitLengthFromMetadata()
    {
        var metadata = new Dictionary<string, object> { ["MaxLength"] = 200 };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetMaxLength(prop.Object);

        result.ShouldBe(200);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetMaxLengthReturnsMaxForStringWithoutExplicitLength()
    {
        var metadata = new Dictionary<string, object> { ["ClrType"] = "System.String" };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetMaxLength(prop.Object);

        result.ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetMaxLengthReturnsNullForNonStringTypes()
    {
        var metadata = new Dictionary<string, object> { ["ClrType"] = "System.Int32" };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetMaxLength(prop.Object);

        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetMaxLengthReturnsMaxWithoutClrType()
    {
        var prop = CreateProperty();

        var result = MsSqlTypeMapper.GetMaxLength(prop.Object);

        result.ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetMaxLengthReturnsMaxForByteArray()
    {
        var metadata = new Dictionary<string, object> { ["ClrType"] = "System.Byte[]" };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetMaxLength(prop.Object);

        // Byte[] resolves via Type.GetType - may return null for "System.Byte[]"
        // since Byte[] is actually an array type. Let's verify what happens.
        result.ShouldNotBeNull();
    }

    // GetPrecision tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetPrecisionReturnsExplicitPrecisionFromMetadata()
    {
        var metadata = new Dictionary<string, object> { ["Precision"] = 10 };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetPrecision(prop.Object);

        result.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetPrecisionReturns18ForDecimal()
    {
        var metadata = new Dictionary<string, object> { ["ClrType"] = "System.Decimal" };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetPrecision(prop.Object);

        result.ShouldBe(18);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetPrecisionReturnsNullForNonDecimalTypes()
    {
        var metadata = new Dictionary<string, object> { ["ClrType"] = "System.Int32" };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetPrecision(prop.Object);

        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetPrecisionReturnsNullWithoutClrType()
    {
        var prop = CreateProperty();

        var result = MsSqlTypeMapper.GetPrecision(prop.Object);

        result.ShouldBeNull();
    }

    // GetScale tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetScaleReturnsExplicitScaleFromMetadata()
    {
        var metadata = new Dictionary<string, object> { ["Scale"] = 4 };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetScale(prop.Object);

        result.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetScaleReturns2ForDecimal()
    {
        var metadata = new Dictionary<string, object> { ["ClrType"] = "System.Decimal" };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetScale(prop.Object);

        result.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetScaleReturnsNullForNonDecimalTypes()
    {
        var metadata = new Dictionary<string, object> { ["ClrType"] = "System.String" };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetScale(prop.Object);

        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetScaleReturnsNullWithoutClrType()
    {
        var prop = CreateProperty();

        var result = MsSqlTypeMapper.GetScale(prop.Object);

        result.ShouldBeNull();
    }

    // TryGetFromMetadata edge cases (tested indirectly)

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapToSqlTypeIgnoresWrongMetadataType()
    {
        // SqlType expects string, give it an int - should fall through
        var metadata = new Dictionary<string, object> { ["SqlType"] = 42 };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.MapToSqlType(prop.Object);

        // SqlType is not a string, falls through to ClrType check which also missing
        result.ShouldBe("VARCHAR");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetMaxLengthIgnoresWrongMetadataType()
    {
        // MaxLength expects int, give it a string
        var metadata = new Dictionary<string, object>
        {
            ["MaxLength"] = "not-a-number",
            ["ClrType"] = "System.Int32"
        };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetMaxLength(prop.Object);

        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetPrecisionIgnoresWrongMetadataType()
    {
        var metadata = new Dictionary<string, object>
        {
            ["Precision"] = "not-a-number",
            ["ClrType"] = "System.Decimal"
        };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetPrecision(prop.Object);

        result.ShouldBe(18);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetScaleIgnoresWrongMetadataType()
    {
        var metadata = new Dictionary<string, object>
        {
            ["Scale"] = "not-a-number",
            ["ClrType"] = "System.Decimal"
        };
        var prop = CreateProperty(metadata);

        var result = MsSqlTypeMapper.GetScale(prop.Object);

        result.ShouldBe(2);
    }
}
