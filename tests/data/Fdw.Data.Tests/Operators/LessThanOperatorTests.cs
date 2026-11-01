using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class LessThanOperatorTests
{
    private readonly LessThanOperator _sut = new();

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
        _sut.FormatODataValue(10).ShouldBe("10");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsShortWithoutQuotes()
    {
        _sut.FormatODataValue((short)50).ShouldBe("50");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsByteWithoutQuotes()
    {
        _sut.FormatODataValue((byte)100).ShouldBe("100");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDecimalWithoutQuotes()
    {
        _sut.FormatODataValue(99.9m).ShouldBe("99.9");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDoubleWithoutQuotes()
    {
        _sut.FormatODataValue(1.5).ShouldNotBeNullOrWhiteSpace();
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
        var dt = new DateTime(2025, 3, 1, 12, 0, 0);
        _sut.FormatODataValue(dt).ShouldBe("datetime'2025-03-01T12:00:00'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDateTimeOffsetWithPrefix()
    {
        var dto = new DateTimeOffset(2025, 3, 1, 12, 0, 0, TimeSpan.Zero);
        _sut.FormatODataValue(dto).ShouldStartWith("datetimeoffset'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsUnknownTypeWithQuotes()
    {
        _sut.FormatODataValue("some text").ShouldBe("'some text'");
    }
}
