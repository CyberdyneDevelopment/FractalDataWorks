using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class LessThanOrEqualOperatorTests
{
    private readonly LessThanOrEqualOperator _sut = new();

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
        _sut.FormatODataValue(100L).ShouldBe("100");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsShortWithoutQuotes()
    {
        _sut.FormatODataValue((short)10).ShouldBe("10");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsByteWithoutQuotes()
    {
        _sut.FormatODataValue((byte)5).ShouldBe("5");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDecimalWithoutQuotes()
    {
        _sut.FormatODataValue(7.77m).ShouldBe("7.77");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDoubleWithoutQuotes()
    {
        _sut.FormatODataValue(3.14).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsFloatWithoutQuotes()
    {
        _sut.FormatODataValue(1.1f).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDateTimeWithPrefix()
    {
        var dt = new DateTime(2025, 12, 31, 23, 59, 59);
        _sut.FormatODataValue(dt).ShouldBe("datetime'2025-12-31T23:59:59'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDateTimeOffsetWithPrefix()
    {
        var dto = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.FromHours(-5));
        _sut.FormatODataValue(dto).ShouldStartWith("datetimeoffset'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsUnknownTypeWithQuotes()
    {
        _sut.FormatODataValue(new object()).ShouldStartWith("'");
    }
}
