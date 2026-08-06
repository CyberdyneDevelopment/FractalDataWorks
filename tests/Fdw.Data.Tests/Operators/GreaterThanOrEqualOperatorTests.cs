using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class GreaterThanOrEqualOperatorTests
{
    private readonly GreaterThanOrEqualOperator _sut = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueReturnsNullForNull()
    {
        _sut.FormatODataValue(null).ShouldBe("null");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsIntWithoutQuotes()
    {
        _sut.FormatODataValue(42).ShouldBe("42");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsLongWithoutQuotes()
    {
        _sut.FormatODataValue(9999999999L).ShouldBe("9999999999");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsShortWithoutQuotes()
    {
        _sut.FormatODataValue((short)123).ShouldBe("123");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsbyteWithoutQuotes()
    {
        _sut.FormatODataValue((byte)255).ShouldBe("255");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDecimalWithoutQuotes()
    {
        _sut.FormatODataValue(3.14m).ShouldBe("3.14");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsFloatWithoutQuotes()
    {
        _sut.FormatODataValue(2.5f).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDateTimeWithPrefix()
    {
        var dt = new DateTime(2025, 6, 15, 10, 30, 0);
        _sut.FormatODataValue(dt).ShouldBe("datetime'2025-06-15T10:30:00'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDateTimeOffsetWithPrefix()
    {
        var dto = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.FromHours(5));
        _sut.FormatODataValue(dto).ShouldStartWith("datetimeoffset'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsUnknownTypeWithQuotes()
    {
        _sut.FormatODataValue("text").ShouldBe("'text'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatSqlParameterUsesDefaultFormat()
    {
        _sut.FormatSqlParameter("Score").ShouldBe("@Score");
    }
}
