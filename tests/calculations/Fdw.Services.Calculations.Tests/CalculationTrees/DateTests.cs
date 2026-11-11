using System;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.Fluency;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Compilation;
using Xunit;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;
using static Fdw.Services.Calculations.Tests.CalculationTrees.CalculationTestSupport;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// A calculation over a date yields a date (<see cref="DateOnly"/>), never a moment: the Date value kind, the AddDate node, reading a
/// Date field, and the rules that keep a date, a date-time and a number apart. Every expected date was worked out by hand.
/// </summary>
public class DateTests
{
    private static CompiledCalculation Compile(CalculationNode node)
    {
        var built = CalculationArray.Named("dates").Result("when", node).Build(Limits, allowDateTimeResults: true);
        built.IsSuccess.ShouldBeTrue();
        var compiled = new CalculationCompiler().Compile(built.Value!, Limits, allowDateTimeResults: true);
        compiled.IsSuccess.ShouldBeTrue();
        return compiled.Value!;
    }

    private static object Run(CompiledCalculation compiled, params (string Field, object? Value)[] fields)
    {
        var state = new CalculationRunState();
        var results = new decimal[compiled.EmittedNames.Count];
        compiled.TryEvaluate(Record(fields), results, state).ShouldBeTrue(state.Reason);
        return compiled.EmittedKinds[0].Read(results[0]);
    }

    [Fact]
    public void ADateFieldPlusDaysYieldsADateNotAMoment()
    {
        var compiled = Compile(AddDate(Field("due"), 5, "Days"));

        Run(compiled, ("due", new DateOnly(2026, 10, 22))).ShouldBe(new DateOnly(2026, 10, 27));
        compiled.EmittedKinds[0].Name.ShouldBe("Date");
        compiled.EmittedKinds[0].DataTypeName.ShouldBe("Date");
        compiled.EmittedKinds[0].ClrType.ShouldBe(typeof(DateOnly));
    }

    [Fact]
    public void ADateDataTypeValueAtMidnightIsReadAsADate()
    {
        // The Date data type holds a DateTime with its time zeroed; a calculation reads it as a date.
        var compiled = Compile(AddDate(Field("due"), 1, "Days"));

        Run(compiled, ("due", new DateTime(2026, 10, 22))).ShouldBe(new DateOnly(2026, 10, 23));
    }

    [Theory]
    [InlineData("Days", 10, "2026-01-31", "2026-02-10")]
    [InlineData("Weeks", 1, "2026-10-22", "2026-10-29")]
    [InlineData("Months", 1, "2026-01-31", "2026-02-28")]
    [InlineData("Months", -1, "2026-03-31", "2026-02-28")]
    [InlineData("Years", 1, "2024-02-29", "2025-02-28")]
    public void EachCalendarUnitMovesTheDateAndKeepsItsPlaceInTheCalendar(string unit, int amount, string start, string expected)
    {
        var compiled = Compile(AddDate(Field("due"), amount, unit));

        Run(compiled, ("due", DateOnly.Parse(start, System.Globalization.CultureInfo.InvariantCulture)))
            .ShouldBe(DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ADateAmountCanComeFromAnotherInput()
    {
        var compiled = Compile(AddDate(Field("due"), Negate(Field("days")), "Days"));

        Run(compiled, ("due", new DateOnly(2026, 3, 1)), ("days", 1m)).ShouldBe(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void AMomentStaysADateTimeOffset()
    {
        var compiled = Compile(Now("UTC"));
        var state = new CalculationRunState { Clock = () => new DateTimeOffset(2026, 10, 22, 14, 30, 5, TimeSpan.Zero) };
        var results = new decimal[1];
        compiled.TryEvaluate(Record(), results, state).ShouldBeTrue(state.Reason);

        compiled.EmittedKinds[0].Name.ShouldBe("DateTime");
        compiled.EmittedKinds[0].DataTypeName.ShouldBe("DateTimeOffset");
        compiled.EmittedKinds[0].Read(results[0]).ShouldBeOfType<DateTimeOffset>();
    }

    [Fact]
    public void AResultKindListNamesADateAsADate()
    {
        var array = CalculationArray.Named("dates").Result("due", AddDate(Field("start"), 1, "Days")).Result("n", Constant(1)).Build(Limits, allowDateTimeResults: true);

        var kinds = CalculationArrayValidator.ResultKinds(array.Value!, Limits);

        kinds.IsSuccess.ShouldBeTrue();
        kinds.Value!["due"].Name.ShouldBe("Date");
        kinds.Value!["n"].Name.ShouldBe("Number");
    }

    [Fact]
    public void ADateMixedWithAMomentIsRefused()
    {
        var momentInDate = CalculationArray.Named("m1").Result("a", AddDate(Now("UTC"), 1, "Days")).Build(Limits, allowDateTimeResults: true);
        var dateInMoment = CalculationArray.Named("m2").Result("a", AddTime(AddDate(Field("d"), 1, "Days"), 1, "UTC", "Days")).Build(Limits, allowDateTimeResults: true);

        RefusalOf(momentInDate).ShouldBe("OperandTypeMismatch");
        RefusalOf(dateInMoment).ShouldBe("OperandTypeMismatch");
    }

    [Fact]
    public void ADateIsNeverReadAsANumber()
    {
        var built = CalculationArray.Named("n").Result("a", Add(AddDate(Field("d"), 1, "Days"), 1)).Build(Limits, allowDateTimeResults: true);

        RefusalOf(built).ShouldBe("OperandTypeMismatch");
    }

    [Fact]
    public void ACallerThatReadsOnlyNumbersRefusesADateResult()
    {
        var built = CalculationArray.Named("n").Result("a", AddDate(Field("d"), 1, "Days")).Build(Limits);

        RefusalOf(built).ShouldBe("OperandTypeMismatch");
    }

    [Theory]
    [InlineData("Hours")]
    [InlineData("Seconds")]
    [InlineData("Fortnights")]
    public void AUnitThatCannotMoveADateIsRefused(string unit)
    {
        var built = CalculationArray.Named("u").Result("a", AddDate(Field("d"), 1, unit)).Build(Limits, allowDateTimeResults: true);

        RefusalOf(built).ShouldBe("NodeMemberInvalid");
    }

    [Fact]
    public void ADateFieldWithATimeOfDayIsRefusedNotTruncated()
    {
        var compiled = Compile(AddDate(Field("due"), 1, "Days"));

        FailureOf(compiled, Record(("due", new DateTime(2026, 10, 22, 14, 0, 0)))).ShouldBe("FieldNotDate");
    }

    [Fact]
    public void ADateFieldThatIsNotADateOrIsAbsentIsRefused()
    {
        var compiled = Compile(AddDate(Field("due"), 1, "Days"));

        FailureOf(compiled, Record(("due", "2026-10-22"))).ShouldBe("FieldNotDate");
        FailureOf(compiled, Record(("due", 5m))).ShouldBe("FieldNotDate");
        FailureOf(compiled, Record()).ShouldBe("FieldNotDate");
    }

    [Fact]
    public void AnEmptyDateFieldFailsAsAnEmptyOperand()
    {
        var compiled = Compile(AddDate(Field("due"), 1, "Days"));

        FailureOf(compiled, Record(("due", null))).ShouldBe("EmptyOperand");
    }

    [Fact]
    public void ADateMovedPastTheEndOfTheCalendarFailsWithTimeOutOfRange()
    {
        var compiled = Compile(AddDate(Field("due"), 1, "Days"));

        FailureOf(compiled, Record(("due", DateOnly.MaxValue))).ShouldBe("TimeOutOfRange");
    }

    [Fact]
    public void ADateTreeRoundTripsThroughItsJson()
    {
        var array = CalculationArray.Named("j").Result("due", AddDate(Field("start"), 2, "Weeks")).Build(Limits, allowDateTimeResults: true);

        var json = CalculationTreeJson.Write(array.Value!.Results[0].Expression);
        var parsed = CalculationTreeJson.Read(json);

        parsed.IsSuccess.ShouldBeTrue();
        parsed.Value!.Op.ShouldBe("AddDate");
        parsed.Value!.Mode.ShouldBe("Weeks");
    }
}
