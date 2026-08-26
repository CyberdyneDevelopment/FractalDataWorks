using System.Globalization;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Fdw.Data.Transformations.Tests;

/// <summary>The numeric family, including the cross-field divide.</summary>
public sealed class NumericTransformTests
{
    [Theory]
    [InlineData("1234.56", 1234.56)]
    [InlineData("-0.5", -0.5)]
    public async Task ParseDecimalReadsTheNumber(string input, decimal expected)
    {
        var result = await new ParseDecimalFieldTransformer().Transform(
            input, TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task ParseDecimalHonoursTheConfiguredCulture()
    {
        // Why this matters: "1.234,56" is one-thousand-two-hundred in de-DE and malformed in en-US.
        var result = await new ParseDecimalFieldTransformer().Transform(
            "1.234,56", TransformTestContext.With(("culture", "de-DE")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(1234.56m);
    }

    [Theory]
    [InlineData(" 42 ", 42)]
    [InlineData("42", 42)]
    public async Task ParseIntReadsTheNumber(string input, int expected)
    {
        var result = await new ParseIntFieldTransformer().Transform(
            input, TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task ParseIntStripsTheConfiguredCharactersFirst()
    {
        var result = await new ParseIntFieldTransformer().Transform(
            "$1200", TransformTestContext.With(("trimChars", "$")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(1200);
    }

    [Theory]
    [InlineData(3.14159, "2", 3.14)]
    [InlineData(1.005, "2", 1.01)]
    public async Task RoundGoesToTheConfiguredPrecision(decimal input, string precision, decimal expected)
    {
        var result = await new RoundFieldTransformer().Transform(
            input, TransformTestContext.With(("precision", precision)), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(2.5, 3)]
    [InlineData(-2.5, -3)]
    [InlineData(0.5, 1)]
    public async Task RoundGoesAwayFromZeroAtTheMidpointNotToEven(decimal input, decimal expected)
    {
        // Why pin this: .NET's Math.Round defaults to banker's rounding, which would make 2.5 -> 2
        // and 0.5 -> 0. This transformer deliberately rounds away from zero, and the difference only
        // shows on exact midpoints - the values a money column is full of.
        var result = await new RoundFieldTransformer().Transform(
            input, TransformTestContext.With(("precision", "0")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task DivideDividesByTheConfiguredDivisor()
    {
        var result = await new DivideFieldTransformer().Transform(
            100m, TransformTestContext.With(("divisor", "4")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(25m);
    }

    [Fact]
    public async Task CastDecimalWidensAnInteger()
    {
        var result = await new CastDecimalFieldTransformer().Transform(
            7, TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(7m);
    }

    // ---- cross-field ----

    [Fact]
    public async Task ConditionalDivideReadsItsDivisorFromAnotherFieldInTheRow()
    {
        var result = await new ConditionalDivideFieldTransformer().Transform(
            100m,
            TransformTestContext.With(
                TransformTestContext.Row(("games", 4m)),
                ("divisorField", "games"), ("default", "0")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(25m);
    }

    [Fact]
    public async Task ConditionalDivideTakesTheDefaultWhenTheDivisorIsZero()
    {
        // Why a default rather than an error: per-game averages over zero games are the normal case
        // in a partial season, not a fault in the data.
        var result = await new ConditionalDivideFieldTransformer().Transform(
            100m,
            TransformTestContext.With(
                TransformTestContext.Row(("games", 0m)),
                ("divisorField", "games"), ("default", "0")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0m);
    }

    [Fact]
    public async Task ConditionalDivideTakesTheDefaultWhenTheFieldIsAbsent()
    {
        var result = await new ConditionalDivideFieldTransformer().Transform(
            100m,
            TransformTestContext.With(
                TransformTestContext.Row(("other", 1m)),
                ("divisorField", "games"), ("default", "-1")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(-1m);
    }

    [Theory]
    [InlineData("AwayFromZero", 2.5, 3)]
    [InlineData("AwayFromZero", -2.5, -3)]
    [InlineData("ToEven", 2.5, 2)]
    [InlineData("ToEven", 3.5, 4)]
    [InlineData("ToZero", 2.9, 2)]
    [InlineData("ToZero", -2.9, -2)]
    [InlineData("ToPositiveInfinity", 2.1, 3)]
    [InlineData("ToPositiveInfinity", -2.9, -2)]
    [InlineData("ToNegativeInfinity", 2.9, 2)]
    [InlineData("ToNegativeInfinity", -2.1, -3)]
    public async Task RoundTakesItsModeFromTheConfiguredRoundingType(string mode, decimal input, decimal expected)
    {
        // Every mode differs from at least one other on these inputs, so a wrong lookup cannot pass.
        var result = await new RoundFieldTransformer().Transform(
            input,
            TransformTestContext.With(("precision", "0"), ("mode", mode)),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public void RoundRejectsAModeThatIsNotRegistered()
    {
        // Why Should.Throw and not ThrowAsync: the guard runs before the Task is created, so the
        // exception escapes synchronously.
        Should.Throw<System.InvalidOperationException>(() =>
        {
            _ = new RoundFieldTransformer().Transform(
                2.5m,
                TransformTestContext.With(("precision", "0"), ("mode", "Nearest")),
                TestContext.Current.CancellationToken);
        });
    }
}
