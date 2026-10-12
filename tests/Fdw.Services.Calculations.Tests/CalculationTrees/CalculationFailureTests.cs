using Fdw.Services.Calculations.Abstractions.Fluency;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Xunit;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;
using static Fdw.Services.Calculations.Tests.CalculationTrees.CalculationTestSupport;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// Every failure of a compiled calculation is a failed result with a code, never an exception and never a quiet zero.
/// </summary>
public class CalculationFailureTests
{
    [Fact]
    public void DivisionByZeroFailsWithItsCode()
    {
        var compiled = Compile(CalculationArray.Named("f1").Result("z", Field("x").Divide(Field("y"))));

        FailureOf(compiled, Record(("x", 6m), ("y", 0m))).ShouldBe("DivisionByZero");
        Evaluate(compiled, Record(("x", 6m), ("y", 3m)))[0].ShouldBe(2m);
    }

    [Fact]
    public void AnEmptyOperandFailsInsteadOfBeingReadAsZero()
    {
        var compiled = Compile(CalculationArray.Named("f2").Result("z", Add(Field("x"), 1)));

        FailureOf(compiled, Record(("x", null))).ShouldBe("EmptyOperand");
    }

    [Fact]
    public void TheFirstFailureWins()
    {
        var compiled = Compile(CalculationArray.Named("f3")
            .Result("first", Divide(Field("x"), 0))
            .Result("second", Require(Field("y"))));

        FailureOf(compiled, Record(("x", 1m), ("y", null))).ShouldBe("DivisionByZero");
    }

    [Fact]
    public void AFloatingPointInputIsRefused()
    {
        var compiled = Compile(CalculationArray.Named("f4").Result("z", Field("x")));

        FailureOf(compiled, Record(("x", 1.5d))).ShouldBe("FieldNotNumeric");
    }

    [Fact]
    public void AMissingFieldIsRefused()
    {
        var compiled = Compile(CalculationArray.Named("f5").Result("z", Field("x")));

        FailureOf(compiled, Record(("y", 1m))).ShouldBe("FieldNotNumeric");
    }

    [Fact]
    public void AFlagAcceptsABooleanOrZeroAndOneAndRefusesAnythingElse()
    {
        var compiled = Compile(CalculationArray.Named("f6").Result("z", When(Field("f"), 1, 2)));

        Evaluate(compiled, Record(("f", true)))[0].ShouldBe(1m);
        Evaluate(compiled, Record(("f", 0)))[0].ShouldBe(2m);
        FailureOf(compiled, Record(("f", 2))).ShouldBe("FieldNotFlag");
    }

    [Fact]
    public void ArithmeticOverflowFailsTheRecord()
    {
        var compiled = Compile(CalculationArray.Named("f7").Result("z", Multiply(Field("a"), Field("a"))));

        FailureOf(compiled, Record(("a", decimal.MaxValue))).ShouldBe("Overflow");
    }

    [Fact]
    public void AStateIsReusedAcrossRecords()
    {
        var compiled = Compile(CalculationArray.Named("f8").Result("z", Field("x").Divide(Field("y"))));
        var state = new CalculationRunState();
        var results = new decimal[1];

        compiled.TryEvaluate(Record(("x", 1m), ("y", 0m)), results, state).ShouldBeFalse();
        compiled.TryEvaluate(Record(("x", 8m), ("y", 2m)), results, state).ShouldBeTrue();

        results[0].ShouldBe(4m);
    }
}
