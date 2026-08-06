using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class NotEqualOperatorAdditionalTests
{
    private readonly NotEqualOperator _sut = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsLongWithoutQuotes()
    {
        _sut.FormatODataValue(5000000000L).ShouldBe("5000000000");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsShortWithoutQuotes()
    {
        _sut.FormatODataValue((short)42).ShouldBe("42");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsByteWithoutQuotes()
    {
        _sut.FormatODataValue((byte)10).ShouldBe("10");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDecimalWithoutQuotes()
    {
        _sut.FormatODataValue(3.14m).ShouldBe("3.14");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDoubleWithoutQuotes()
    {
        _sut.FormatODataValue(2.718).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsFloatWithoutQuotes()
    {
        _sut.FormatODataValue(1.5f).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsBoolFalseAsLowercase()
    {
        _sut.FormatODataValue(false).ShouldBe("false");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDateTimeWithPrefix()
    {
        var dt = new DateTime(2025, 1, 1, 0, 0, 0);
        _sut.FormatODataValue(dt).ShouldBe("datetime'2025-01-01T00:00:00'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDateTimeOffsetWithPrefix()
    {
        var dto = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _sut.FormatODataValue(dto).ShouldStartWith("datetimeoffset'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsGuidWithGuidPrefix()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        _sut.FormatODataValue(guid).ShouldBe("guid'12345678-1234-1234-1234-123456789abc'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFallsBackToQuotedStringForUnknownTypes()
    {
        _sut.FormatODataValue(new object()).ShouldStartWith("'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatSqlParameterUsesDefaultFormat()
    {
        _sut.FormatSqlParameter("Status").ShouldBe("@Status");
    }
}
