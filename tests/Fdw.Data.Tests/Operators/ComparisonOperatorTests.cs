using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class ComparisonOperatorTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GreaterThanHasCorrectProperties()
    {
        var sut = new GreaterThanOperator();
        sut.Id.ShouldBe(6);
        sut.Name.ShouldBe("GreaterThan");
        sut.SqlOperator.ShouldBe(">");
        sut.ODataOperator.ShouldBe("gt");
        sut.RequiresValue.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GreaterThanOrEqualHasCorrectProperties()
    {
        var sut = new GreaterThanOrEqualOperator();
        sut.Id.ShouldBe(7);
        sut.Name.ShouldBe("GreaterThanOrEqual");
        sut.SqlOperator.ShouldBe(">=");
        sut.ODataOperator.ShouldBe("ge");
        sut.RequiresValue.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void LessThanHasCorrectProperties()
    {
        var sut = new LessThanOperator();
        sut.Id.ShouldBe(8);
        sut.Name.ShouldBe("LessThan");
        sut.SqlOperator.ShouldBe("<");
        sut.ODataOperator.ShouldBe("lt");
        sut.RequiresValue.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void LessThanOrEqualHasCorrectProperties()
    {
        var sut = new LessThanOrEqualOperator();
        sut.Id.ShouldBe(9);
        sut.Name.ShouldBe("LessThanOrEqual");
        sut.SqlOperator.ShouldBe("<=");
        sut.ODataOperator.ShouldBe("le");
        sut.RequiresValue.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GreaterThanFormatODataValueReturnsNullForNull()
    {
        new GreaterThanOperator().FormatODataValue(null).ShouldBe("null");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GreaterThanFormatODataValueFormatsIntWithoutQuotes()
    {
        new GreaterThanOperator().FormatODataValue(10).ShouldBe("10");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GreaterThanFormatODataValueFormatsDecimalWithoutQuotes()
    {
        new GreaterThanOperator().FormatODataValue(99.5m).ShouldBe("99.5");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GreaterThanFormatODataValueFormatsDateTimeWithPrefix()
    {
        var dt = new DateTime(2025, 1, 1, 0, 0, 0);
        new GreaterThanOperator().FormatODataValue(dt).ShouldBe("datetime'2025-01-01T00:00:00'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GreaterThanFormatODataValueFormatsDateTimeOffsetWithPrefix()
    {
        var dto = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        new GreaterThanOperator().FormatODataValue(dto).ShouldBe("datetimeoffset'2025-01-01T00:00:00+00:00'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void LessThanFormatODataValueFormatsLongWithoutQuotes()
    {
        new LessThanOperator().FormatODataValue(5000000000L).ShouldBe("5000000000");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void LessThanOrEqualFormatODataValueFallsBackToQuotedForUnknownTypes()
    {
        new LessThanOrEqualOperator().FormatODataValue("text").ShouldBe("'text'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GreaterThanOrEqualFormatODataValueFormatsDoubleWithoutQuotes()
    {
        new GreaterThanOrEqualOperator().FormatODataValue(3.14).ShouldBe("3.14");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AllComparisonOperatorsUseDefaultFormatSqlParameter()
    {
        new GreaterThanOperator().FormatSqlParameter("Age").ShouldBe("@Age");
        new GreaterThanOrEqualOperator().FormatSqlParameter("Age").ShouldBe("@Age");
        new LessThanOperator().FormatSqlParameter("Age").ShouldBe("@Age");
        new LessThanOrEqualOperator().FormatSqlParameter("Age").ShouldBe("@Age");
    }
}
