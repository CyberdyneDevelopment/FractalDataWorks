using System;
using System.Collections.Generic;
using Fdw.Services.Calculations.Abstractions.Fluency;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Compilation;
using Xunit;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;
using static Fdw.Services.Calculations.Tests.CalculationTrees.CalculationTestSupport;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// The compiled delegate agrees with a direct decimal evaluation of the same array on thousands of random records: the same
/// values when every result succeeds, the same first failure when one does not. This is the acceptance test the settlement
/// team ran on its 184 stages.
/// </summary>
public class CalculationEquivalenceTests
{
    private const int Records = 3000;

    [Fact]
    public void ACompiledArrayThatCannotFailEqualsTheDirectEvaluation()
    {
        var array = Build(canFail: false);
        var compiled = CompileArray(array);
        var random = new Random(20261008);
        var state = new CalculationRunState();
        var results = new decimal[compiled.EmittedNames.Count];

        for (var i = 0; i < Records; i++)
        {
            var record = RandomRecord(random);
            var expected = ReferenceEvaluator.Run(array, record);

            expected.Failure.ShouldBeNull();
            compiled.TryEvaluate(record, results, state).ShouldBeTrue();
            AssertSame(results, expected.Emitted);
        }
    }

    [Fact]
    public void ACompiledArrayThatCanFailReportsTheSameFirstFailure()
    {
        var array = Build(canFail: true);
        var compiled = CompileArray(array);
        var random = new Random(20261009);
        var state = new CalculationRunState();
        var results = new decimal[compiled.EmittedNames.Count];
        var failures = 0;

        for (var i = 0; i < Records; i++)
        {
            var record = RandomRecord(random);
            var expected = ReferenceEvaluator.Run(array, record);
            var succeeded = compiled.TryEvaluate(record, results, state);

            succeeded.ShouldBe(expected.Failure is null);
            if (succeeded)
            {
                AssertSame(results, expected.Emitted);
                continue;
            }

            failures++;
            CalculationTreeResultCodes.ById(state.Code).Name.ShouldBe(expected.Failure);
        }

        failures.ShouldBeGreaterThan(0);
        failures.ShouldBeLessThan(Records);
    }

    private static void AssertSame(decimal[] actual, List<decimal> expected)
    {
        actual.Length.ShouldBe(expected.Count);
        for (var i = 0; i < actual.Length; i++)
        {
            actual[i].ShouldBe(expected[i]);
        }
    }

    private static CompiledCalculation CompileArray(CalculationArrayDefinition array)
    {
        var compiled = new CalculationCompiler().Compile(array, Limits);
        compiled.IsSuccess.ShouldBeTrue();
        return compiled.Value!;
    }

    // With canFail false every result succeeds on every generated record; with true the array holds a Guard that fails when the
    // count is neither one nor two-without-the-flag, and a Require that fails when xm is empty.
    private static CalculationArrayDefinition Build(bool canFail)
    {
        var condition = Or(Equal(Field("n"), 1), And(Equal(Field("n"), 2), Not(Field("f"))));
        var rounded = Round(Ref("p"), 2, "ToEven");
        var builder = CalculationArray.Named("settlement")
            .Result("h", Divide(60, Field("m")), emit: false)
            .Result("z1", Field("x"))
            .Result("z2", Field("x").Divide(Ref("h")))
            .Result("z3", Multiply(Field("x"), Field("y")).Divide(Ref("h")))
            .Result("z4", Add(Field("x1"), Field("x2"), Field("x3"), Field("x4"), Field("x5"))
                .Add(Subtract(Field("y1"), Field("y2")), Field("w"))
                .Subtract(Field("u1"), Field("u2"), Field("u3"), Field("u4"), 0, Field("u5")))
            .Result("z5", Add(
                Multiply(Field("x1"), Field("y1")),
                Multiply(Field("x2"), Field("y2")),
                Multiply(Field("x3"), Field("y3")),
                Multiply(Field("x4"), Field("y4")),
                Multiply(Field("x5"), Field("y5"))))
            .Result("z6", When(Field("f"), Field("x"), Field("y")))
            .Result("z7", When(
                Field("f"),
                When(NotEqual(Field("x"), 0), Field("x"), Field("y")),
                When(NotEqual(Field("u1"), 0), Field("u1"), Field("v"))))
            .Result("z8", When(
                NotEqual(Field("q"), 0),
                Add(Field("x"), Field("y"), Field("w"), Field("u1"), Field("v")).Divide(Field("q")),
                0))
            .Result("z9", When(Equal(Field("n"), 1), Field("x"), 0))
            .Result("z10", Field("x").Add(When(Greater(Field("y"), 0), Multiply(Field("x2"), Field("y")), 0)))
            .Result("z11", Coalesce(Field("xm"), Field("ym"), 0))
            .Result("z12", Multiply(Coalesce(Field("xm"), Field("ym"), 0), Field("s")))
            .Result("z13", Max(0, Coalesce(Field("xm"), Field("ym"), 0)))
            .Result("z14", Negate(Field("x")).Divide(Ref("h")))
            .Result("z15", Multiply(-1, Field("x")))
            .Result("p", Add(Multiply(Field("x"), Constant(0.125m)), Field("y")), emit: false)
            .Result("z16", canFail ? Guard(condition, rounded) : When(condition, rounded, 0))
            .Result("z17", Ref("z16").Add(1))
            .Result("z18", When(IsEmpty(Field("xm")), 0, Field("xm")));
        if (canFail)
        {
            builder.Result("z19", Require(Field("xm")));
        }

        var built = builder.Build(Limits);
        built.IsSuccess.ShouldBeTrue();
        return built.Value!;
    }

    private static Dictionary<string, object?> RandomRecord(Random random)
    {
        var record = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["m"] = new[] { 15m, 30m, 60m }[random.Next(0, 3)],
            ["x"] = Amount(random),
            ["y"] = Amount(random),
            ["w"] = Amount(random),
            ["v"] = Amount(random),
            ["s"] = Math.Round((decimal)random.NextDouble(), 4),
            ["q"] = random.Next(0, 4) == 0 ? 0m : Amount(random),
            ["n"] = random.Next(0, 3),
            ["f"] = random.Next(0, 2) == 0,
            ["xm"] = random.Next(0, 5) == 0 ? null : Amount(random),
            ["ym"] = random.Next(0, 5) == 0 ? null : Amount(random),
        };
        for (var i = 1; i <= 5; i++)
        {
            record["x" + i] = Amount(random);
            record["y" + i] = Amount(random);
            record["u" + i] = Amount(random);
        }

        return record;
    }

    private static decimal Amount(Random random) => Math.Round((decimal)(random.NextDouble() * 400 - 100), 4);
}
