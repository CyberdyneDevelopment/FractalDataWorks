using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class EqualOperatorTests
{
    private readonly EqualOperator _sut = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsIdToOne()
    {
        _sut.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsNameToEqual()
    {
        _sut.Name.ShouldBe("Equal");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SqlOperatorIsEqualsSign()
    {
        _sut.SqlOperator.ShouldBe("=");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ODataOperatorIsEq()
    {
        _sut.ODataOperator.ShouldBe("eq");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RequiresValueIsTrue()
    {
        _sut.RequiresValue.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatSqlParameterAddsAtPrefix()
    {
        _sut.FormatSqlParameter("Name").ShouldBe("@Name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueReturnsNullStringForNull()
    {
        _sut.FormatODataValue(null).ShouldBe("null");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueWrapsStringInSingleQuotes()
    {
        _sut.FormatODataValue("Acme").ShouldBe("'Acme'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueEscapesSingleQuotesInStrings()
    {
        _sut.FormatODataValue("O'Brien").ShouldBe("'O''Brien'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsIntWithoutQuotes()
    {
        _sut.FormatODataValue(42).ShouldBe("42");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsLongWithoutQuotes()
    {
        _sut.FormatODataValue(9999999999L).ShouldBe("9999999999");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsShortWithoutQuotes()
    {
        _sut.FormatODataValue((short)123).ShouldBe("123");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDecimalWithoutQuotes()
    {
        _sut.FormatODataValue(99.95m).ShouldBe("99.95");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDoubleWithoutQuotes()
    {
        _sut.FormatODataValue(3.14).ShouldBe("3.14");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsFloatWithoutQuotes()
    {
        _sut.FormatODataValue(2.5f).ShouldBe("2.5");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsBoolAsLowercase()
    {
        _sut.FormatODataValue(true).ShouldBe("true");
        _sut.FormatODataValue(false).ShouldBe("false");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDateTimeWithPrefix()
    {
        var dt = new DateTime(2025, 6, 15, 10, 30, 45);
        _sut.FormatODataValue(dt).ShouldBe("datetime'2025-06-15T10:30:45'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDateTimeOffsetWithPrefix()
    {
        var dto = new DateTimeOffset(2025, 6, 15, 10, 30, 45, TimeSpan.FromHours(-5));
        _sut.FormatODataValue(dto).ShouldBe("datetimeoffset'2025-06-15T10:30:45-05:00'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsGuidWithPrefix()
    {
        var guid = new Guid("12345678-1234-1234-1234-123456789abc");
        _sut.FormatODataValue(guid).ShouldBe("guid'12345678-1234-1234-1234-123456789abc'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFallsBackToQuotedToStringForUnknownTypes()
    {
        var uri = new Uri("https://example.com");
        _sut.FormatODataValue(uri).ShouldBe("'https://example.com/'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueHandlesByteWithoutQuotes()
    {
        _sut.FormatODataValue((byte)255).ShouldBe("255");
    }
}
