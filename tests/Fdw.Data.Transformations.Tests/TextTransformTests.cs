using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Fdw.Data.Transformations.Tests;

/// <summary>The text family, each fed a real value.</summary>
public sealed class TextTransformTests
{
    [Theory]
    [InlineData("  hello  ", "hello")]
    [InlineData("hello", "hello")]
    [InlineData("   ", "")]
    public async Task TrimRemovesSurroundingWhitespace(string input, string expected)
    {
        var result = await new TrimFieldTransformer().Transform(
            input, TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task TrimTakesTheConfiguredCharactersInsteadOfWhitespace()
    {
        var result = await new TrimFieldTransformer().Transform(
            "**value**", TransformTestContext.With(("chars", "*")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("value");
    }

    [Fact]
    public async Task TrimStartLeavesTheTrailingEnd()
    {
        var result = await new TrimStartFieldTransformer().Transform(
            "00123 ", TransformTestContext.With(("chars", "0")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("123 ");
    }

    [Theory]
    [InlineData("MiXeD", "mixed")]
    [InlineData("ÀÉÎ", "àéî")]
    public async Task ToLowerLowersEveryCharacter(string input, string expected)
    {
        var result = await new ToLowerFieldTransformer().Transform(
            input, TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("MiXeD", "MIXED")]
    [InlineData("àéî", "ÀÉÎ")]
    public async Task ToUpperRaisesEveryCharacter(string input, string expected)
    {
        var result = await new ToUpperFieldTransformer().Transform(
            input, TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("a,b,c", 0, "a")]
    [InlineData("a,b,c", 2, "c")]
    public async Task SplitTakesThePartAtTheIndex(string input, int index, string expected)
    {
        var result = await new SplitFieldTransformer().Transform(
            input,
            TransformTestContext.With(("delimiter", ","), ("index", index.ToString(System.Globalization.CultureInfo.InvariantCulture))),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task SplitPastTheEndIsEmptyRatherThanAnError()
    {
        var result = await new SplitFieldTransformer().Transform(
            "a,b", TransformTestContext.With(("delimiter", ","), ("index", "9")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task SplitWithoutADelimiterReturnsTheValueUnchanged()
    {
        var result = await new SplitFieldTransformer().Transform(
            "a,b", TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.Value.ShouldBe("a,b");
    }
}
