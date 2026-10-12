using System;
using System.Globalization;
using System.Linq;
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
        var result = await new RoundFieldTransformer().Transform(
            input, TransformTestContext.With(("precision", "0")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(2.5, 2)]
    [InlineData(3.5, 4)]
    [InlineData(-2.5, -2)]
    public async Task RoundToEvenTakesTheNearestEvenResultAtTheMidpoint(decimal input, decimal expected)
    {
        var result = await new RoundFieldTransformer().Transform(
            input, TransformTestContext.With(("precision", "0"), ("mode", "ToEven")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task RoundWithoutAModeGoesAwayFromZeroAsItAlwaysDid()
    {
        var withoutMode = await new RoundFieldTransformer().Transform(
            2.5m, TransformTestContext.With(("precision", "0")), TestContext.Current.CancellationToken);
        var withBlankMode = await new RoundFieldTransformer().Transform(
            2.5m, TransformTestContext.With(("precision", "0"), ("mode", " ")), TestContext.Current.CancellationToken);

        withoutMode.Value.ShouldBe(3m);
        withBlankMode.Value.ShouldBe(3m);
    }

    [Fact]
    public async Task RoundRefusesAnUnknownModeNamingIt()
    {
        var run = async () => await new RoundFieldTransformer().Transform(
            2.5m, TransformTestContext.With(("precision", "0"), ("mode", "Banker")), TestContext.Current.CancellationToken);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(run);
        thrown.Message.ShouldContain("Banker");
    }

    [Fact]
    public void RoundDeclaresPrecisionRequiredAndModeOptional()
    {
        var declared = new RoundFieldTransformer().ExpectedParameters;

        declared.Count.ShouldBe(2);
        declared.Single(p => p.Name == "precision").IsRequired.ShouldBeTrue();
        declared.Single(p => p.Name == "mode").IsRequired.ShouldBeFalse();
    }

    [Fact]
    public void RoundModeHelpNamesEveryRoundingType()
    {
        var help = new RoundFieldTransformer().ExpectedParameters.Single(p => p.Name == "mode").HelpText;

        help.ShouldNotBeNull();
        foreach (var rounding in RoundingTypes.All())
        {
            help.ShouldContain(rounding.Name);
        }
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
    public async Task DivideFailsWhenTheConfiguredDivisorIsZero()
    {
        var result = await new DivideFieldTransformer().Transform(
            100m, TransformTestContext.With(("divisor", "0")), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Division by zero");
        result.CurrentMessage.ShouldContain("Divide");
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
        Should.Throw<System.InvalidOperationException>(() =>
        {
            _ = new RoundFieldTransformer().Transform(
                2.5m,
                TransformTestContext.With(("precision", "0"), ("mode", "Nearest")),
                TestContext.Current.CancellationToken);
        });
    }
}
