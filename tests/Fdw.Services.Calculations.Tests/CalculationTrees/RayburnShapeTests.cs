using System.Collections.Generic;
using Fdw.Services.Calculations.Abstractions.Fluency;
using Xunit;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;
using static Fdw.Services.Calculations.Tests.CalculationTrees.CalculationTestSupport;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// The first thirteen settlement shapes (FDW-1187), each written with the fluent builder, compiled, and evaluated on known
/// inputs. Expected values are worked out by hand.
/// </summary>
public class RayburnShapeTests
{
    [Fact]
    public void Shape1CopiesAValue()
    {
        var compiled = Compile(CalculationArray.Named("s1").Result("z", Field("x")));

        Evaluate(compiled, Record(("x", 12.5m)))[0].ShouldBe(12.5m);
    }

    [Fact]
    public void Shape2ScalesToTheInterval()
    {
        var compiled = Compile(CalculationArray.Named("s2")
            .Result("h", Divide(60, Field("m")), emit: false)
            .Result("z", Field("x").Divide(Ref("h"))));

        Evaluate(compiled, Record(("m", 15m), ("x", 100m)))[0].ShouldBe(25m);
    }

    [Fact]
    public void Shape3MultipliesValuesAndShares()
    {
        var compiled = Compile(CalculationArray.Named("s3")
            .Result("h", Divide(60, Field("m")), emit: false)
            .Result("z1", Multiply(Field("x"), Field("y")).Divide(Ref("h")))
            .Result("z2", Multiply(Field("x"), Field("s"))));

        var results = Evaluate(compiled, Record(("m", 15), ("x", 8m), ("y", 3m), ("s", 0.25m)));

        results[0].ShouldBe(6m);
        results[1].ShouldBe(2m);
    }

    [Fact]
    public void Shape4AddsAndSubtractsManyTermsLeftToRight()
    {
        var compiled = Compile(CalculationArray.Named("s4").Result(
            "z",
            Add(Field("x1"), Field("x2"), Field("x3"), Field("x4"), Field("x5"))
                .Add(Subtract(Field("y1"), Field("y2")), Field("w"))
                .Subtract(Field("u1"), Field("u2"), Field("u3"), Field("u4"), 0, Field("u5"))));
        var record = Record(
            ("x1", 1m), ("x2", 2m), ("x3", 3m), ("x4", 4m), ("x5", 5m), ("y1", 10m), ("y2", 3m), ("w", 2m),
            ("u1", 1m), ("u2", 1m), ("u3", 1m), ("u4", 1m), ("u5", 1m));

        Evaluate(compiled, record)[0].ShouldBe(19m);
    }

    [Fact]
    public void Shape5SumsProducts()
    {
        var compiled = Compile(CalculationArray.Named("s5").Result(
            "z",
            Add(
                Multiply(Field("x1"), Field("y1")),
                Multiply(Field("x2"), Field("y2")),
                Multiply(Field("x3"), Field("y3")),
                Multiply(Field("x4"), Field("y4")),
                Multiply(Field("x5"), Field("y5")))));
        var record = Record(
            ("x1", 1m), ("x2", 2m), ("x3", 3m), ("x4", 4m), ("x5", 5m),
            ("y1", 2m), ("y2", 2m), ("y3", 2m), ("y4", 2m), ("y5", 2m));

        Evaluate(compiled, record)[0].ShouldBe(30m);
    }

    [Fact]
    public void Shape6ChoosesByFlagAndNests()
    {
        var compiled = Compile(CalculationArray.Named("s6")
            .Result("simple", When(Field("f"), Field("x"), Field("y")))
            .Result("nested", When(
                Field("f"),
                When(NotEqual(Field("x"), 0), Field("x"), Field("y")),
                When(NotEqual(Field("u"), 0), Field("u"), Field("v")))));

        var whenTrue = Evaluate(compiled, Record(("f", true), ("x", 0m), ("y", 7m), ("u", 3m), ("v", 4m)));
        var whenFalse = Evaluate(compiled, Record(("f", false), ("x", 0m), ("y", 7m), ("u", 0m), ("v", 4m)));

        whenTrue[0].ShouldBe(0m);
        whenTrue[1].ShouldBe(7m);
        whenFalse[0].ShouldBe(7m);
        whenFalse[1].ShouldBe(4m);
    }

    [Fact]
    public void Shape7GuardedDivisionAvoidsTheFailure()
    {
        var compiled = Compile(CalculationArray.Named("s7").Result(
            "z",
            When(
                NotEqual(Field("q"), 0),
                Add(Field("x"), Field("y"), Field("w"), Field("u"), Field("v")).Divide(Field("q")),
                0)));
        var values = new (string, object?)[] { ("x", 1m), ("y", 2m), ("w", 3m), ("u", 4m), ("v", 5m) };

        Evaluate(compiled, Record(Join(values, ("q", 0m))))[0].ShouldBe(0m);
        Evaluate(compiled, Record(Join(values, ("q", 4m))))[0].ShouldBe(3.75m);
    }

    [Fact]
    public void Shape8ChecksCountsAndSigns()
    {
        var compiled = Compile(CalculationArray.Named("s8")
            .Result("count", When(Equal(Field("n"), 1), Field("x"), 0))
            .Result("sign", Field("x").Add(When(Greater(Field("y"), 0), Multiply(Field("x2"), Field("y")), 0))));

        var one = Evaluate(compiled, Record(("n", 1), ("x", 10m), ("y", 3m), ("x2", 2m)));
        var two = Evaluate(compiled, Record(("n", 2), ("x", 10m), ("y", 0m), ("x2", 2m)));

        one[0].ShouldBe(10m);
        one[1].ShouldBe(16m);
        two[0].ShouldBe(0m);
        two[1].ShouldBe(10m);
    }

    [Fact]
    public void Shape9DefaultsAnEmptyValue()
    {
        var compiled = Compile(CalculationArray.Named("s9")
            .Result("chain", Coalesce(Field("x"), Field("y"), 0))
            .Result("scaled", Multiply(Coalesce(Field("x"), Field("y"), 0), Field("s"))));

        var second = Evaluate(compiled, Record(("x", null), ("y", 5m), ("s", 2m)));
        var neither = Evaluate(compiled, Record(("x", null), ("y", null), ("s", 2m)));

        second[0].ShouldBe(5m);
        second[1].ShouldBe(10m);
        neither[0].ShouldBe(0m);
        neither[1].ShouldBe(0m);
    }

    [Fact]
    public void Shape10RequiredValueFailsWhenEmpty()
    {
        var compiled = Compile(CalculationArray.Named("s10").Result("z", Require(Field("x"))));

        Evaluate(compiled, Record(("x", 3m)))[0].ShouldBe(3m);
        FailureOf(compiled, Record(("x", null))).ShouldBe("RequiredValueEmpty");
    }

    [Fact]
    public void Shape11FloorsAtZero()
    {
        var compiled = Compile(CalculationArray.Named("s11").Result("z", Max(0, Coalesce(Field("x"), Field("y"), 0))));

        Evaluate(compiled, Record(("x", -4m), ("y", null)))[0].ShouldBe(0m);
        Evaluate(compiled, Record(("x", null), ("y", 9m)))[0].ShouldBe(9m);
    }

    [Fact]
    public void Shape12NegatesAndScales()
    {
        var compiled = Compile(CalculationArray.Named("s12")
            .Result("h", Divide(60, Field("m")), emit: false)
            .Result("negative", Multiply(-1, Field("x")))
            .Result("scaled", Negate(Field("x")).Divide(Ref("h"))));

        var results = Evaluate(compiled, Record(("m", 15m), ("x", 8m)));

        results[0].ShouldBe(-8m);
        results[1].ShouldBe(-2m);
    }

    [Fact]
    public void Shape13RoundsHalfToEvenOnlyWhenTheCountIsOne()
    {
        var compiled = Compile(CalculationArray.Named("s13")
            .Result("p", Add(Multiply(Field("x"), Constant(0.125m)), Field("y")), emit: false)
            .Result("rounded", Guard(Equal(Field("n"), 1), Round(Ref("p"), 2, "ToEven")))
            .Result("next", Ref("rounded").Add(1)));

        var results = Evaluate(compiled, Record(("n", 1), ("x", 1m), ("y", 0m)));

        results[0].ShouldBe(0.12m);
        results[1].ShouldBe(1.12m);
        FailureOf(compiled, Record(("n", 2), ("x", 1m), ("y", 0m))).ShouldBe("GuardFailed");
    }

    [Fact]
    public void ResultsChainSixDeep()
    {
        var compiled = Compile(CalculationArray.Named("chain")
            .Result("a", Field("x"), emit: false)
            .Result("b", Ref("a").Add(1), emit: false)
            .Result("c", Ref("b").Add(1), emit: false)
            .Result("d", Ref("c").Add(1), emit: false)
            .Result("e", Ref("d").Add(1), emit: false)
            .Result("z", Ref("e").Add(1)));

        Evaluate(compiled, Record(("x", 10m)))[0].ShouldBe(15m);
    }

    private static (string, object?)[] Join((string, object?)[] values, (string, object?) extra)
    {
        var all = new List<(string, object?)>(values) { extra };
        return all.ToArray();
    }
}
