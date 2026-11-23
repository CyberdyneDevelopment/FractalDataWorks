using System;
using System.Collections.Generic;
using System.Globalization;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

/// <summary>
/// The OData literal each kind of filter value is written as. The type of a filter value is the caller's, so a value of a type no
/// named kind holds is written as quoted text, as it always was.
/// </summary>
public sealed class ODataLiteralKindsTests
{
    private enum Color
    {
        Red,
    }


    public static IEnumerable<object[]> ComparisonOperators() =>
    [
        [new EqualOperator()],
        [new NotEqualOperator()],
        [new LessThanOperator()],
        [new LessThanOrEqualOperator()],
        [new GreaterThanOperator()],
        [new GreaterThanOrEqualOperator()],
    ];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NullIsTheLiteralNull() => ODataLiteralKinds.Format(null).ShouldBe("null");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TextIsQuotedWithItsQuoteDoubled() => ODataLiteralKinds.Format("it's").ShouldBe("'it''s'");

    [Theory]
    [InlineData(42, "42")]
    [InlineData(9999999999L, "9999999999")]
    [InlineData((short)123, "123")]
    [InlineData((byte)255, "255")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AWholeNumberIsWrittenAsItIs(object value, string expected) => ODataLiteralKinds.Format(value).ShouldBe(expected);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AFractionIsWrittenInTheInvariantCulture()
    {
        var before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            ODataLiteralKinds.Format(3.14m).ShouldBe("3.14");
            ODataLiteralKinds.Format(2.5).ShouldBe("2.5");
            ODataLiteralKinds.Format(1.5f).ShouldBe("1.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ABoolIsLowercase()
    {
        ODataLiteralKinds.Format(true).ShouldBe("true");
        ODataLiteralKinds.Format(false).ShouldBe("false");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADateAndTimeHasItsPrefix()
    {
        ODataLiteralKinds.Format(new DateTime(2025, 6, 15, 10, 30, 0)).ShouldBe("datetime'2025-06-15T10:30:00'");
        ODataLiteralKinds.Format(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)).ShouldBe("datetimeoffset'2025-01-01T00:00:00+00:00'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AGuidHasItsPrefix()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        ODataLiteralKinds.Format(id).ShouldBe("guid'11111111-2222-3333-4444-555555555555'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AValueOfAnyOtherTypeIsQuotedTextAsItAlwaysWas()
    {
        ODataLiteralKinds.Format(Color.Red).ShouldBe("'Red'");
        ODataLiteralKinds.Format('x').ShouldBe("'x'");
        ODataLiteralKinds.Format(new DateOnly(2026, 11, 18)).ShouldBe($"'{new DateOnly(2026, 11, 18)}'");
        ODataLiteralKinds.Format(new TimeOnly(10, 30)).ShouldBe($"'{new TimeOnly(10, 30)}'");
        ODataLiteralKinds.Format(TimeSpan.FromMinutes(90)).ShouldBe($"'{TimeSpan.FromMinutes(90)}'");
        ODataLiteralKinds.Format((sbyte)5).ShouldBe("'5'");
        ODataLiteralKinds.Format(7u).ShouldBe("'7'");
    }

    [Theory]
    [MemberData(nameof(ComparisonOperators))]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EveryComparisonOperatorWritesItsValueThroughTheKinds(FilterOperatorBase op)
    {
        op.FormatODataValue(null).ShouldBe("null");
        op.FormatODataValue("it's").ShouldBe("'it''s'");
        op.FormatODataValue(42).ShouldBe("42");
        op.FormatODataValue(true).ShouldBe("true");
        op.FormatODataValue(new DateTime(2025, 6, 15, 10, 30, 0)).ShouldBe("datetime'2025-06-15T10:30:00'");
        op.FormatODataValue(Color.Red).ShouldBe("'Red'");
        op.FormatODataValue('x').ShouldBe("'x'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsWritesAnyValueAsEscapedText()
    {
        var sut = new ContainsOperator();

        sut.FormatODataValue("it's").ShouldBe("'it''s'");
        sut.FormatODataValue(42).ShouldBe("'42'");
        sut.FormatODataValue(Color.Red).ShouldBe("'Red'");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InAndNotInWriteEachItemThroughTheKinds(bool notIn)
    {
        FilterOperatorBase sut = notIn ? new NotInOperator() : new InOperator();

        sut.FormatODataValue(new object[] { 1, "a", true, Color.Red }).ShouldBe("(1,'a',true,'Red')");
        sut.FormatODataValue(null).ShouldBe("()");
    }
}
