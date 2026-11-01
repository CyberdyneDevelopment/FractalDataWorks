using System;
using System.Globalization;
using Fdw.Services.Calculations.Abstractions.Fluency;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Compilation;
using Xunit;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;
using static Fdw.Services.Calculations.Tests.CalculationTrees.CalculationTestSupport;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// The Now and AddTime nodes: a date-time computed from a clock the caller gives, in a time zone the calculation states. Every
/// expected moment below was worked out with an independent time zone library, not with the code under test.
/// </summary>
public class RelativeTimeTests
{
    private const string Chicago = "America/Chicago";
    private const string Auckland = "Pacific/Auckland";
    private const string Utc = "UTC";

    private static DateTimeOffset At(string iso) => DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    private static CompiledCalculation CompileMoment(CalculationNode node)
    {
        var built = CalculationArray.Named("moments").Result("when", node).Build(Limits, allowDateTimeResults: true);
        built.IsSuccess.ShouldBeTrue();
        var compiled = new CalculationCompiler().Compile(built.Value!, Limits, allowDateTimeResults: true);
        compiled.IsSuccess.ShouldBeTrue();
        return compiled.Value!;
    }

    private static DateTimeOffset Run(CompiledCalculation compiled, DateTimeOffset now, params (string Field, object? Value)[] fields)
    {
        var state = new CalculationRunState { Clock = () => now };
        var results = new decimal[compiled.EmittedNames.Count];
        compiled.TryEvaluate(Record(fields), results, state).ShouldBeTrue(state.Reason);
        return (DateTimeOffset)compiled.EmittedKinds[0].Read(results[0]);
    }

    [Theory]
    [InlineData(Chicago, "2026-10-22T05:00:00Z")]
    [InlineData(Auckland, "2026-10-22T11:00:00Z")]
    [InlineData(Utc, "2026-10-22T00:00:00Z")]
    public void StartOfDayDependsOnTheTimeZone(string zone, string expected)
    {
        var compiled = CompileMoment(Now(zone, "StartOfDay"));

        Run(compiled, At("2026-10-22T14:30:05Z")).ShouldBe(At(expected));
    }

    [Theory]
    [InlineData("StartOfWeek", "2026-10-19T05:00:00Z")]
    [InlineData("StartOfMonth", "2026-10-01T05:00:00Z")]
    [InlineData("StartOfYear", "2026-01-01T06:00:00Z")]
    public void ACalendarAnchorCutsBackToTheStartOfThePeriod(string anchor, string expected)
    {
        var compiled = CompileMoment(Now(Chicago, anchor));

        Run(compiled, At("2026-10-22T14:30:05Z")).ShouldBe(At(expected));
    }

    [Fact]
    public void NowWithNoAnchorIsTheClockAsItIs()
    {
        var compiled = CompileMoment(Now(Utc));

        Run(compiled, At("2026-10-22T14:30:05Z")).ShouldBe(At("2026-10-22T14:30:05Z"));
    }

    [Fact]
    public void SecondsAgoSubtractsAnInput()
    {
        var compiled = CompileMoment(AddTime(Now(Utc), Negate(Field("seconds")), Utc, "Seconds"));

        Run(compiled, At("2026-10-22T14:30:05Z"), ("seconds", 33m)).ShouldBe(At("2026-10-22T14:29:32Z"));
    }

    [Fact]
    public void YesterdayIsTheStartOfTodayMinusOneDay()
    {
        var compiled = CompileMoment(AddTime(Now(Chicago, "StartOfDay"), -1, Chicago, "Days"));

        Run(compiled, At("2026-10-22T14:30:05Z")).ShouldBe(At("2026-10-21T05:00:00Z"));
    }

    [Fact]
    public void ADayIsACalendarDayAndAnHourIsElapsedTimeAcrossTheClocksChanging()
    {
        var now = At("2026-11-02T15:00:00Z");

        var calendar = CompileMoment(AddTime(Now(Chicago, "StartOfDay"), -1, Chicago, "Days"));
        var elapsed = CompileMoment(AddTime(Now(Chicago, "StartOfDay"), -24, Chicago, "Hours"));

        Run(calendar, now).ShouldBe(At("2026-11-01T05:00:00Z"));
        Run(elapsed, now).ShouldBe(At("2026-11-01T06:00:00Z"));
    }

    [Fact]
    public void ADayThatLandsInTheHourTheClocksSkipMovesForwardAnHour()
    {
        var compiled = CompileMoment(AddTime(Now(Chicago), 1, Chicago, "Days"));

        Run(compiled, At("2027-03-13T08:30:00Z")).ShouldBe(At("2027-03-14T08:30:00Z"));
    }

    [Fact]
    public void ADayThatLandsInTheHourTheClocksRepeatTakesTheFirstOccurrence()
    {
        var compiled = CompileMoment(AddTime(Now(Chicago), 1, Chicago, "Days"));

        Run(compiled, At("2026-10-31T06:30:00Z")).ShouldBe(At("2026-11-01T06:30:00Z"));
    }

    [Theory]
    [InlineData("Seconds", 45, "2026-10-22T14:30:50Z")]
    [InlineData("Minutes", 90, "2026-10-22T16:00:05Z")]
    [InlineData("Hours", -2, "2026-10-22T12:30:05Z")]
    [InlineData("Days", 10, "2026-11-01T14:30:05Z")]
    [InlineData("Weeks", 2, "2026-11-05T14:30:05Z")]
    [InlineData("Months", 3, "2027-01-22T14:30:05Z")]
    [InlineData("Years", -1, "2025-10-22T14:30:05Z")]
    public void EveryUnitMovesTheMoment(string unit, int amount, string expected)
    {
        var compiled = CompileMoment(AddTime(Now(Utc), amount, Utc, unit));

        Run(compiled, At("2026-10-22T14:30:05Z")).ShouldBe(At(expected));
    }

    [Fact]
    public void AMonthStepStopsAtTheEndOfAShorterMonth()
    {
        var compiled = CompileMoment(AddTime(Now(Utc), 1, Utc, "Months"));

        Run(compiled, At("2026-01-31T10:00:00Z")).ShouldBe(At("2026-02-28T10:00:00Z"));
    }

    [Fact]
    public void AYearStepFromTheLeapDayLandsOnTheLastDayOfFebruary()
    {
        var compiled = CompileMoment(AddTime(Now(Utc), 1, Utc, "Years"));

        Run(compiled, At("2028-02-29T12:00:00Z")).ShouldBe(At("2029-02-28T12:00:00Z"));
    }

    [Fact]
    public void TwoNowNodesInOneEvaluationAgreeEvenWhenTheClockMoves()
    {
        var built = CalculationArray.Named("pair").Result("first", Now(Utc)).Result("second", Now(Utc)).Build(Limits, allowDateTimeResults: true);
        var compiled = new CalculationCompiler().Compile(built.Value!, Limits, allowDateTimeResults: true).Value!;
        var ticks = 0;
        var state = new CalculationRunState { Clock = () => At("2026-10-22T14:30:05Z").AddSeconds(ticks++) };
        var results = new decimal[2];

        compiled.TryEvaluate(Record(), results, state).ShouldBeTrue();

        results[0].ShouldBe(results[1]);
    }

    [Fact]
    public void AnEmittedResultSaysWhetherItIsANumberOrADateTime()
    {
        var built = CalculationArray.Named("mixed").Result("count", Add(Field("x"), 1)).Result("when", Now(Utc)).Build(Limits, allowDateTimeResults: true);
        var compiled = new CalculationCompiler().Compile(built.Value!, Limits, allowDateTimeResults: true).Value!;

        compiled.EmittedKinds[0].Name.ShouldBe("Number");
        compiled.EmittedKinds[0].DataTypeName.ShouldBe("Decimal");
        compiled.EmittedKinds[1].Name.ShouldBe("DateTime");
        compiled.EmittedKinds[1].DataTypeName.ShouldBe("DateTimeOffset");
    }

    [Fact]
    public void AnEvaluationGivenNoClockFailsInsteadOfGuessing()
    {
        var compiled = CompileMoment(Now(Utc));

        FailureOf(compiled, Record()).ShouldBe("ClockMissing");
    }

    [Fact]
    public void AFractionalAmountFails()
    {
        var compiled = CompileMoment(AddTime(Now(Utc), Field("n"), Utc, "Days"));
        var state = new CalculationRunState { Clock = () => At("2026-10-22T14:30:05Z") };

        compiled.TryEvaluate(Record(("n", 1.5m)), new decimal[1], state).ShouldBeFalse();

        compiled.Describe(state).Code!.Name.ShouldBe("AmountNotWhole");
    }

    [Fact]
    public void AMomentOutsideTheRepresentableRangeFails()
    {
        var compiled = CompileMoment(AddTime(Now(Utc), 9000, Utc, "Years"));
        var state = new CalculationRunState { Clock = () => At("2026-10-22T14:30:05Z") };

        compiled.TryEvaluate(Record(), new decimal[1], state).ShouldBeFalse();

        compiled.Describe(state).Code!.Name.ShouldBe("TimeOutOfRange");
    }

    [Fact]
    public void ADateTimeResultIsRefusedUnlessTheCallerSaysItReadsOne()
    {
        var builder = CalculationArray.Named("moments").Result("when", Now(Utc));

        RefusalOf(builder.Build(Limits)).ShouldBe("OperandTypeMismatch");
        builder.Build(Limits, allowDateTimeResults: true).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ANumberCalculationIsNotChangedByDateTimesExisting()
    {
        var compiled = Compile(CalculationArray.Named("plain").Result("z", Add(Field("x"), 1)));

        Evaluate(compiled, Record(("x", 2m)))[0].ShouldBe(3m);
        compiled.EmittedKinds[0].Name.ShouldBe("Number");
    }

    [Fact]
    public void ResultKindsNamesTheKindOfEveryResultKeptOrNot()
    {
        var array = CalculationArray.Named("mixed")
            .Result("count", Add(Field("x"), 1), emit: false)
            .Result("when", Now(Utc))
            .Build(Limits, allowDateTimeResults: true).Value!;

        var kinds = CalculationArrayValidator.ResultKinds(array, Limits);

        kinds.IsSuccess.ShouldBeTrue();
        kinds.Value!["count"].Name.ShouldBe("Number");
        kinds.Value["when"].Name.ShouldBe("DateTime");
        kinds.Value["when"].DataTypeName.ShouldBe("DateTimeOffset");
    }

    [Fact]
    public void ARefToADateTimeResultIsRefused()
    {
        var built = CalculationArray.Named("ref").Result("when", Now(Utc)).Result("again", Ref("when")).Build(Limits, allowDateTimeResults: true);

        RefusalOf(built).ShouldBe("NodeMemberInvalid");
    }

    [Fact]
    public void ADateTimeIsNeverReadAsANumberNorANumberAsADateTime()
    {
        var asNumber = CalculationArray.Named("a").Result("z", Add(Now(Utc), 1)).Build(Limits, allowDateTimeResults: true);
        var asMoment = CalculationArray.Named("b").Result("z", AddTime(Constant(1), 2, Utc, "Days")).Build(Limits, allowDateTimeResults: true);

        RefusalOf(asNumber).ShouldBe("OperandTypeMismatch");
        RefusalOf(asMoment).ShouldBe("OperandTypeMismatch");
    }

    [Theory]
    [InlineData(null, "StartOfDay")]
    [InlineData("", "StartOfDay")]
    [InlineData("Mars/Olympus", "StartOfDay")]
    [InlineData("UTC", "StartOfFortnight")]
    public void ANowNodeThatStatesNoKnownZoneOrAnUnknownAnchorIsRefusedWhenSaved(string? zone, string anchor)
    {
        var node = new CalculationNode("Now", zone, null, null, null, anchor, Array.Empty<CalculationNode>());

        RefusalOf(CalculationArray.Named("n").Result("when", node).Build(Limits, allowDateTimeResults: true)).ShouldBe("NodeMemberInvalid");
    }

    [Theory]
    [InlineData("UTC", null)]
    [InlineData("UTC", "Fortnights")]
    [InlineData(null, "Days")]
    public void AnAddTimeNodeThatStatesNoUnitOrZoneOrAnUnknownOneIsRefusedWhenSaved(string? zone, string? unit)
    {
        var node = new CalculationNode("AddTime", zone, null, null, null, unit, new[] { Now("UTC"), (CalculationNode)1 });

        RefusalOf(CalculationArray.Named("n").Result("when", node).Build(Limits, allowDateTimeResults: true)).ShouldBe("NodeMemberInvalid");
    }

    [Fact]
    public void ATimeTreeSurvivesAJsonRoundTrip()
    {
        var tree = AddTime(Now(Chicago, "StartOfDay"), Negate(Field("days")), Chicago, "Days");

        var json = CalculationTreeJson.Write(tree);
        var read = CalculationTreeJson.Read(json);

        read.IsSuccess.ShouldBeTrue();
        CalculationTreeJson.Write(read.Value!).ShouldBe(json);
        read.Value!.Operands[0].Mode.ShouldBe("StartOfDay");
        read.Value!.Name.ShouldBe(Chicago);
    }
}
