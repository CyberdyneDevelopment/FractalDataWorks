using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Fdw.Data.Transformations.Tests;

/// <summary>The date/time family, including the two that read other fields.</summary>
public sealed class DateTimeTransformTests
{
    [Fact]
    public async Task ParseDateOnlyReadsAnIsoDate()
    {
        var result = await new ParseDateOnlyFieldTransformer().Transform(
            "2026-08-26", TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new DateOnly(2026, 8, 26));
    }

    [Fact]
    public async Task ParseDateOnlyHonoursTheConfiguredFormat()
    {
        // Why this matters: 03/04 is 3 April or 4 March depending on the format, and both parse.
        var result = await new ParseDateOnlyFieldTransformer().Transform(
            "04/03/2026", TransformTestContext.With(("format", "dd/MM/yyyy")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new DateOnly(2026, 3, 4));
    }

    [Fact]
    public async Task FromUnixMillisecondsConvertsTheEpochOffset()
    {
        // 1,000,000,000,000 ms after the epoch is 2001-09-09T01:46:40Z.
        var result = await new FromUnixMillisecondsFieldTransformer().Transform(
            1_000_000_000_000L, TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var actual = result.Value.ShouldBeOfType<DateTimeOffset>();
        actual.ToUniversalTime().ShouldBe(new DateTimeOffset(2001, 9, 9, 1, 46, 40, TimeSpan.Zero));
    }

    [Fact]
    public async Task ParseDateTimeOffsetReadsAnOffsetAndKeepsIt()
    {
        var result = await new ParseDateTimeOffsetFieldTransformer().Transform(
            "2026-08-26T10:00:00-05:00", TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var actual = result.Value.ShouldBeOfType<DateTimeOffset>();
        actual.Offset.ShouldBe(TimeSpan.FromHours(-5));
        actual.ToUniversalTime().Hour.ShouldBe(15);
    }

    [Fact]
    public async Task AddDurationAddsTheConfiguredAmount()
    {
        // "Minutes" is a DurationUnitTypes option — the unit vocabulary is a collection, not free text.
        var result = await new AddDurationFieldTransformer().Transform(
            new DateTimeOffset(2026, 8, 26, 10, 0, 0, TimeSpan.Zero),
            TransformTestContext.With(("amount", "90"), ("unit", "Minutes")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var actual = result.Value.ShouldBeOfType<DateTimeOffset>();
        actual.ToUniversalTime().ShouldBe(new DateTimeOffset(2026, 8, 26, 11, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task TimezoneMovesTheInstantIntoTheNamedZone()
    {
        // Same instant, expressed in Eastern — August is daylight time, so UTC-4. The zone name is a
        // TimeZoneTypes option, not an IANA id: the collection is the vocabulary.
        var result = await new TimezoneFieldTransformer().Transform(
            new DateTimeOffset(2026, 8, 26, 16, 0, 0, TimeSpan.Zero),
            TransformTestContext.With(("zone", "Eastern")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var actual = result.Value.ShouldBeOfType<DateTimeOffset>();
        actual.ToUniversalTime().ShouldBe(new DateTimeOffset(2026, 8, 26, 16, 0, 0, TimeSpan.Zero));
        actual.Offset.ShouldBe(TimeSpan.FromHours(-4));
    }

    [Fact]
    public async Task FallbackFromFieldTakesAnotherFieldAndAddsTheDuration()
    {
        // Not a plain fallback despite the name: it reads the named field AND applies amount/unit.
        var result = await new FallbackFromFieldFieldTransformer().Transform(
            null,
            TransformTestContext.With(
                TransformTestContext.Row(("kickoff", new DateTimeOffset(2026, 8, 26, 17, 0, 0, TimeSpan.Zero))),
                ("sourceField", "kickoff"), ("amount", "3"), ("unit", "Hours")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var actual = result.Value.ShouldBeOfType<DateTimeOffset>();
        actual.ToUniversalTime().ShouldBe(new DateTimeOffset(2026, 8, 26, 20, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task FallbackFromFieldKeepsAValueThatIsAlreadyThere()
    {
        var present = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var result = await new FallbackFromFieldFieldTransformer().Transform(
            present,
            TransformTestContext.With(
                TransformTestContext.Row(("kickoff", new DateTimeOffset(2026, 8, 26, 17, 0, 0, TimeSpan.Zero))),
                ("sourceField", "kickoff"), ("amount", "3"), ("unit", "Hours")),
            TestContext.Current.CancellationToken);

        result.Value.ShouldBe(present);
    }

    [Fact]
    public async Task CompositeDateTimeBuildsAnInstantFromSeparateDateAndHourFields()
    {
        var result = await new CompositeDateTimeFieldTransformer().Transform(
            null,
            TransformTestContext.With(
                TransformTestContext.Row(("gameDate", "2026-08-26"), ("gameHour", "19")),
                ("dateField", "gameDate"), ("hourField", "gameHour"), ("zone", "UTC")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }
}
