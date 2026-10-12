using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Fdw.Data.Transformations.Tests;

/// <summary>The boolean family.</summary>
public sealed class BooleanTransformTests
{
    [Theory]
    [InlineData(true, "Yes")]
    [InlineData(false, "No")]
    public async Task BoolToStringUsesTheConfiguredLabels(bool input, string expected)
    {
        var result = await new BoolToStringFieldTransformer().Transform(
            input,
            TransformTestContext.With(("trueLabel", "Yes"), ("falseLabel", "No")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Y", "Y", true)]
    [InlineData("N", "Y", false)]
    [InlineData("1", "1", true)]
    public async Task StringToBoolComparesAgainstTheConfiguredTrueValue(string input, string trueValue, bool expected)
    {
        var result = await new StringToBoolFieldTransformer().Transform(
            input, TransformTestContext.With(("trueValue", trueValue)), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }
}
