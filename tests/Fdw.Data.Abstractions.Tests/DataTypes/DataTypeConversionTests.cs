using Fdw.Data.Abstractions;

namespace Fdw.Data.Abstractions.Tests.DataTypeConversion;

/// <summary>
/// A data type converts a value to its own CLR type (FDW-1109): text reads with the invariant culture, a value already of the
/// type (or one that widens to it) is taken as it is, and anything else is a failure, never a guess.
/// </summary>
public sealed class DataTypeConversionTests
{
    private static IDataType DataTypeNamed(string name) => DataTypes.ByName(name);

    [Theory]
    [InlineData("String", "hello world", "hello world")]
    [InlineData("Int16", "-7", (short)-7)]
    [InlineData("Int32", "42", 42)]
    [InlineData("Int64", "9000000000", 9000000000L)]
    [InlineData("Byte", "200", (byte)200)]
    [InlineData("Bool", "true", true)]
    [InlineData("Bool", "FALSE", false)]
    public void ConvertReadsTextAsTheTypesClrValue(string typeName, string text, object expected)
    {
        var type = DataTypeNamed(typeName);

        var result = type.Convert(text);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
        result.Value!.GetType().ShouldBe(type.ClrType);
    }

    [Fact]
    public void ConvertReadsADecimalWithAPointAndADoubleWithAnExponent()
    {
        DataTypeNamed("Decimal").Convert("1.5").Value.ShouldBe(1.5m);
        DataTypeNamed("Double").Convert("1.5e2").Value.ShouldBe(150d);
        DataTypeNamed("Float").Convert("0.25").Value.ShouldBe(0.25f);
    }

    [Fact]
    public void ConvertReadsAGuidAndATimeOfDay()
    {
        var id = Guid.NewGuid();

        DataTypeNamed("Guid").Convert(id.ToString()).Value.ShouldBe(id);
        DataTypeNamed("Time").Convert("12:30:15").Value.ShouldBe(new TimeSpan(12, 30, 15));
    }

    [Theory]
    [InlineData("Int32", "4.2")]
    [InlineData("Int32", "abc")]
    [InlineData("Int32", "")]
    [InlineData("Int32", "2147483648")]
    [InlineData("Int16", "40000")]
    [InlineData("Byte", "256")]
    [InlineData("Byte", "-1")]
    [InlineData("Int64", "1,000")]
    [InlineData("Decimal", "1,5")]
    [InlineData("Decimal", "x")]
    [InlineData("Double", "NaN")]
    [InlineData("Double", "Infinity")]
    [InlineData("Bool", "yes")]
    [InlineData("Bool", "1")]
    [InlineData("Guid", "not-a-guid")]
    [InlineData("DateTimeOffset", "garbage")]
    [InlineData("DateTimeOffset", "")]
    [InlineData("Time", "noon")]
    public void ConvertRefusesTextThatIsNotAValueOfTheType(string typeName, string text)
    {
        var result = DataTypeNamed(typeName).Convert(text);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("DataValueNotConvertible");
    }

    [Fact]
    public void ConvertRefusesNullAndAValueOfAnotherKind()
    {
        DataTypeNamed("Int32").Convert(null).IsSuccess.ShouldBeFalse();
        DataTypeNamed("Int32").Convert(new object()).IsSuccess.ShouldBeFalse();
        DataTypeNamed("String").Convert(42).IsSuccess.ShouldBeFalse();
        DataTypeNamed("Bool").Convert(1).IsSuccess.ShouldBeFalse();
        DataTypeNamed("Int32").Convert(1.5d).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void ConvertKeepsAValueAlreadyOfTheTypesClrType()
    {
        var moment = new DateTimeOffset(2026, 3, 1, 11, 0, 0, TimeSpan.FromHours(2));
        var id = Guid.NewGuid();

        DataTypeNamed("Int64").Convert(5L).Value.ShouldBe(5L);
        DataTypeNamed("Int32").Convert(5).Value.ShouldBe(5);
        DataTypeNamed("Decimal").Convert(2.5m).Value.ShouldBe(2.5m);
        DataTypeNamed("Bool").Convert(true).Value.ShouldBe(true);
        DataTypeNamed("Guid").Convert(id).Value.ShouldBe(id);
        DataTypeNamed("DateTimeOffset").Convert(moment).Value.ShouldBe(moment);
    }

    [Fact]
    public void ConvertRefusesAValueOfAnotherClrTypeRatherThanWideningOrNarrowingIt()
    {
        DataTypeNamed("Int64").Convert(5).IsSuccess.ShouldBeFalse();
        DataTypeNamed("Int32").Convert(5L).IsSuccess.ShouldBeFalse();
        DataTypeNamed("Decimal").Convert(5).IsSuccess.ShouldBeFalse();
        DataTypeNamed("Double").Convert(2.5m).IsSuccess.ShouldBeFalse();
        DataTypeNamed("DateTimeOffset").Convert(new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Unspecified)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void ConvertKeepsTheOffsetATextStatesAndReadsATextWithoutOneAsUtc()
    {
        var offset = DataTypeNamed("DateTimeOffset").Convert("2026-10-01T12:00:00+02:00");
        var utc = DataTypeNamed("DateTimeOffset").Convert("2026-10-01T12:00:00");
        var date = DataTypeNamed("DateTimeOffset").Convert("2026-10-01");

        ((DateTimeOffset)offset.Value!).Offset.ShouldBe(TimeSpan.FromHours(2));
        utc.Value.ShouldBe(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
        date.Value.ShouldBe(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void DateKeepsTheDayAndDropsTheTime()
    {
        var result = DataTypeNamed("Date").Convert("2026-10-01T23:59:59Z");

        result.Value.ShouldBe(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void BinaryTakesBytesAsTheyAreRefusesTextAndDoesNotFitAnExpression()
    {
        var type = DataTypeNamed("Binary");
        var bytes = new byte[] { 1, 2, 3 };

        type.Convert(bytes).Value.ShouldBeSameAs(bytes);
        type.Convert("AQID").IsSuccess.ShouldBeFalse();
        type.RendersInExpressions.ShouldBeFalse();
    }

    [Theory]
    [InlineData("String")]
    [InlineData("Int16")]
    [InlineData("Int32")]
    [InlineData("Int64")]
    [InlineData("Byte")]
    [InlineData("Decimal")]
    [InlineData("Double")]
    [InlineData("Float")]
    [InlineData("Bool")]
    [InlineData("Guid")]
    [InlineData("DateTimeOffset")]
    [InlineData("DateTime")]
    [InlineData("Date")]
    [InlineData("Time")]
    public void EveryOtherTypeFitsAnExpression(string typeName)
    {
        DataTypeNamed(typeName).RendersInExpressions.ShouldBeTrue();
    }

    [Theory]
    [InlineData("String", "hello")]
    [InlineData("Int32", "42")]
    [InlineData("Int64", "-9000000000")]
    [InlineData("Decimal", "1234.5")]
    [InlineData("Double", "0.5")]
    [InlineData("Bool", "true")]
    [InlineData("Bool", "false")]
    [InlineData("Guid", "7c9e6679-7425-40de-944b-e07fc1f90ae7")]
    [InlineData("DateTimeOffset", "2026-10-01T12:30:15.0000000+00:00")]
    [InlineData("DateTime", "2026-10-01T12:30:15.0000000Z")]
    [InlineData("Date", "2026-10-01")]
    [InlineData("Time", "12:30:15")]
    public void FormatWritesTheInvariantTextThatConvertReadsBack(string typeName, string text)
    {
        var type = DataTypeNamed(typeName);
        var value = type.Convert(text).Value!;

        var written = type.Format(value);

        written.ShouldBe(text);
        type.Convert(written).Value.ShouldBe(value);
    }

    [Fact]
    public void FormatWritesADecimalWithAPointWhateverTheCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            DataTypeNamed("Decimal").Format(1234.5m).ShouldBe("1234.5");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void FormatKeepsTheOffsetOfADateTimeOffset()
    {
        var moment = new DateTimeOffset(2026, 10, 1, 12, 30, 15, TimeSpan.FromHours(-5));
        var type = DataTypeNamed("DateTimeOffset");

        var back = (DateTimeOffset)type.Convert(type.Format(moment)).Value!;

        back.ShouldBe(moment);
        back.Offset.ShouldBe(TimeSpan.FromHours(-5));
    }

    [Theory]
    [InlineData("String", typeof(string))]
    [InlineData("Int16", typeof(short))]
    [InlineData("Int32", typeof(int))]
    [InlineData("Int64", typeof(long))]
    [InlineData("Byte", typeof(byte))]
    [InlineData("Decimal", typeof(decimal))]
    [InlineData("Double", typeof(double))]
    [InlineData("Float", typeof(float))]
    [InlineData("Bool", typeof(bool))]
    [InlineData("Guid", typeof(Guid))]
    [InlineData("DateTimeOffset", typeof(DateTimeOffset))]
    [InlineData("DateTime", typeof(DateTime))]
    [InlineData("Date", typeof(DateTime))]
    [InlineData("Time", typeof(TimeSpan))]
    [InlineData("Binary", typeof(byte[]))]
    public void ClrTypeNamesTheTypeOfTheValuesTheTypeReads(string typeName, Type expected)
    {
        DataTypeNamed(typeName).ClrType.ShouldBe(expected);
    }

    [Fact]
    public void TheNotFoundSentinelConvertsNothing()
    {
        var sentinel = DataTypes.NotFound;

        var result = sentinel.Convert("anything");

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("DataValueNotConvertible");
        sentinel.ClrType.ShouldBe(typeof(object));
    }
}
