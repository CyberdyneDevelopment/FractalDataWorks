using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Fdw.Services.Calculations.Operations.Arithmetic;
using Fdw.Services.Calculations.Operations.Structural;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Covers the operations added to close the billing-determinant gap: Negate,
/// ProportionalAllocation, and PassThrough.
/// </summary>
/// <remarks>
/// SafeDivide and ConditionalOverride are deliberately absent: <c>Divide</c> already guards zero
/// explicitly and fails loud, and <c>Coalesce</c> already supplies override-if-present-else-base.
/// Adding same-named duplicates would give two ways to express one intent.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public class NewOperationsTests
{
    private static decimal AsDecimal(object value) =>
        decimal.Parse(value.ToString()!, CultureInfo.InvariantCulture);

    [Fact]
    public async Task NegateReversesSign()
    {
        var result = await new NegateOperation().Calculate(
            new Dictionary<string, object?> { ["Value"] = 12.5m },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        AsDecimal(result.Value!).ShouldBe(-12.5m);
    }

    [Fact]
    public async Task NegateReversesNegativeToPositive()
    {
        var result = await new NegateOperation().Calculate(
            new Dictionary<string, object?> { ["Value"] = -3m },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        AsDecimal(result.Value!).ShouldBe(3m);
    }

    [Fact]
    public async Task ProportionalAllocationDistributesTotalByShare()
    {
        // 25 of 100 of a 400 total => 100
        var result = await new ProportionalAllocationOperation().Calculate(
            new Dictionary<string, object?> { ["Part"] = 25m, ["Whole"] = 100m, ["Total"] = 400m },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        AsDecimal(result.Value!).ShouldBe(100m);
    }

    [Fact]
    public async Task ProportionalAllocationKeepsPrecisionThroughTheDivision()
    {
        // Why this case: 1/3 of 100 evaluated as (Part/Whole) first would round to 0.33 and lose
        // a third of a cent per allocation. Computing in one step keeps full decimal precision.
        var result = await new ProportionalAllocationOperation().Calculate(
            new Dictionary<string, object?> { ["Part"] = 1m, ["Whole"] = 3m, ["Total"] = 100m },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        AsDecimal(result.Value!).ShouldBe(33.333333333333333333333333333m, tolerance: 0.0000000001m);
    }

    [Fact]
    public async Task ProportionalAllocationFailsWhenWholeIsZero()
    {
        // Must fail, never allocate zero — an absent basis is not "nobody gets anything".
        var result = await new ProportionalAllocationOperation().Calculate(
            new Dictionary<string, object?> { ["Part"] = 5m, ["Whole"] = 0m, ["Total"] = 400m },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task PassThroughReturnsValueUnchanged()
    {
        var result = await new PassThroughOperation().Calculate(
            new Dictionary<string, object?> { ["Value"] = 42m },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42m);
    }

    [Fact]
    public async Task PassThroughDoesNotCoerceType()
    {
        // The operation asserts nothing about type — a string stays a string.
        var result = await new PassThroughOperation().Calculate(
            new Dictionary<string, object?> { ["Value"] = "TariffA" },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("TariffA");
    }

    [Fact]
    public async Task PassThroughFailsOnNullRatherThanPublishingNothing()
    {
        var result = await new PassThroughOperation().Calculate(
            new Dictionary<string, object?> { ["Value"] = null },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
