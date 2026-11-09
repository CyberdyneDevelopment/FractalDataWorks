using System;
using Fdw.Data.MsSql;

namespace Fdw.Data.MsSql.Tests;

/// <summary>
/// The Date converter's ToDb: every way a day reaches a SQL Server date column ends as the day. Text that is not a date is passed through, not thrown: ToDb has no result to report a
/// failure in, so the bulk plan refuses it with its own code (FDW-4723, FDW-4726).
/// </summary>
public class MsSqlDateConverterTests
{
    private readonly MsSqlDateConverter _sut = new();

    [Theory]
    [InlineData("10/27/2026")]
    [InlineData("2026-10-27")]
    [InlineData(" 10/27/2026 ")]
    [InlineData("10/27/2026 00:00:00")]
    [InlineData("10/27/2026 17:45:10")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDbParsesDateTextMonthFirstWithOrWithoutATime(string text)
    {
        var result = _sut.ToDb(text);

        result.ShouldBeOfType<DateTime>();
        result.ShouldBe(new DateTime(2026, 10, 27));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDbTakesTheDayOfADateOnly()
    {
        _sut.ToDb(new DateOnly(2026, 10, 27)).ShouldBe(new DateTime(2026, 10, 27));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDbTakesTheDayOfADateTimeOffset()
    {
        _sut.ToDb(new DateTimeOffset(2026, 10, 27, 23, 30, 0, TimeSpan.FromHours(-5))).ShouldBe(new DateTime(2026, 10, 27));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDbLeavesADateTimeAndANullAsTheyAre()
    {
        var moment = new DateTime(2026, 10, 27, 8, 0, 0);

        _sut.ToDb(moment).ShouldBe(moment);
        _sut.ToDb(null).ShouldBeNull();
    }

    [Theory]
    [InlineData("not a date")]
    [InlineData("")]
    [InlineData("13/45/2026")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDbPassesTextThatIsNotADateThroughUnchanged(string text)
    {
        _sut.ToDb(text).ShouldBe(text);
    }
}
