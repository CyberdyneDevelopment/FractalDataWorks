using System;
using Fdw.Data.Transformations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// Pins the behaviour that having two calling conventions cost: a transform reached through the
/// parameterless one ran with an empty parameter bag, so it silently produced a value nobody
/// configured. There is now a single entry point that cannot be called without a context.
/// </summary>
public sealed class ConfiguredTransformTests
{
    private static TransformationContext ContextWith(params (string Key, string Value)[] parameters)
    {
        var bag = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in parameters)
        {
            bag[key] = value;
        }

        return new TransformationContext { Parameters = bag };
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ConfiguredLabelsAreUsed()
    {
        var result = await new BoolToStringFieldTransformer().Transform(
            true,
            ContextWith(("trueLabel", "Yes"), ("falseLabel", "No")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Yes");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FalseTakesTheOtherConfiguredLabel()
    {
        var result = await new BoolToStringFieldTransformer().Transform(
            false,
            ContextWith(("trueLabel", "Yes"), ("falseLabel", "No")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("No");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task WithNoParametersTheResultIsEmptyRatherThanEitherLabel()
    {
        var result = await new BoolToStringFieldTransformer().Transform(
            true,
            new TransformationContext(),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RequiredParametersAreDeclaredSoCallersCanCheckThem()
    {
        // The ETL Map transform reads exactly this to decide whether a step is runnable.
        var transformer = new BoolToStringFieldTransformer();

        transformer.ExpectedParameters.Count.ShouldBe(2);
        transformer.ExpectedParameters.ShouldAllBe(p => p.IsRequired);
    }
}
