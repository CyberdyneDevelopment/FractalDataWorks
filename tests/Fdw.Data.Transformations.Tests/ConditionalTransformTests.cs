using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Fdw.Data.Transformations.Tests;

/// <summary>The conditional family — two of the three read another field.</summary>
public sealed class ConditionalTransformTests
{
    [Fact]
    public async Task NullToEmptyReplacesNullWithAnEmptyString()
    {
        var result = await new NullToEmptyFieldTransformer().Transform(
            null, TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task NullToEmptyLeavesARealValueAlone()
    {
        var result = await new NullToEmptyFieldTransformer().Transform(
            "kept", TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.Value.ShouldBe("kept");
    }

    [Fact]
    public async Task NullToDefaultSubstitutesTheConfiguredValue()
    {
        var result = await new NullToDefaultFieldTransformer().Transform(
            null,
            TransformTestContext.With(("defaultValue", "unknown"), ("type", "string")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("unknown");
    }

    [Fact]
    public async Task CoalesceTakesTheFallbackFieldWhenTheValueIsNull()
    {
        var result = await new CoalesceFieldTransformer().Transform(
            null,
            TransformTestContext.With(
                TransformTestContext.Row(("nickname", "Ace")),
                ("fallbackField", "nickname")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Ace");
    }

    [Fact]
    public async Task CoalesceKeepsTheValueWhenItIsPresent()
    {
        var result = await new CoalesceFieldTransformer().Transform(
            "Real",
            TransformTestContext.With(
                TransformTestContext.Row(("nickname", "Ace")),
                ("fallbackField", "nickname")),
            TestContext.Current.CancellationToken);

        result.Value.ShouldBe("Real");
    }

}
