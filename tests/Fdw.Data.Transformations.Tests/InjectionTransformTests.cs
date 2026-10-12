using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Fdw.Data.Transformations.Tests;

/// <summary>The injection family — values that come from configuration rather than the row.</summary>
public sealed class InjectionTransformTests
{
    [Fact]
    public async Task ConstantIgnoresTheIncomingValueAndEmitsTheConfiguredOne()
    {
        var result = await new ConstantFieldTransformer().Transform(
            "ignored", TransformTestContext.With(("value", "FIXED")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("FIXED");
    }

    [Fact]
    public async Task ParameterEmitsTheOperatingDateFromTheContext()
    {
        var result = await new ParameterFieldTransformer().Transform(
            null, TransformTestContext.With(("name", "operatingDate")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<System.DateOnly>();
    }

    [Fact]
    public async Task ParameterEmitsNowFromTheContext()
    {
        var result = await new ParameterFieldTransformer().Transform(
            null, TransformTestContext.With(("name", "now")), TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<System.DateTimeOffset>();
    }

    [Fact]
    public async Task ParameterFailsLoudForAnyOtherName()
    {
        var result = await new ParameterFieldTransformer().Transform(
            null, TransformTestContext.With(("name", "season")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Message.ShouldContain("season");
        result.Messages[0].Message.ShouldContain("operatingDate");
    }

    [Fact]
    public async Task ParameterFailsLoudWhenNoNameIsSupplied()
    {
        var result = await new ParameterFieldTransformer().Transform(
            null, TransformTestContext.With(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Message.ShouldContain("'name'");
    }
}
