using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Calculations.Operations.Arithmetic;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// A zero divisor fails the composable arithmetic operations with a coded message that names the operation and its
/// operands, never an empty value or a zero (FDW-1189).
/// </summary>
public class DivisionByZeroOperationTests
{
    [Fact]
    public async Task DivideFailsNamingTheOperandsWhenTheDivisorIsZero()
    {
        var result = await new DivideOperation().Calculate(
            new Dictionary<string, object?> { ["Left"] = 9m, ["Right"] = 0m },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Division by zero");
        result.CurrentMessage.ShouldContain("Left / Right");
    }

    [Fact]
    public async Task ModuloFailsNamingTheOperandsWhenTheDivisorIsZero()
    {
        var result = await new ModuloOperation().Calculate(
            new Dictionary<string, object?> { ["Left"] = 9m, ["Right"] = 0m },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Left % Right");
    }

    [Fact]
    public async Task ProportionalAllocationFailsNamingTheOperandsWhenTheWholeIsZero()
    {
        var result = await new ProportionalAllocationOperation().Calculate(
            new Dictionary<string, object?> { ["Part"] = 3m, ["Whole"] = 0m, ["Total"] = 100m },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Part / Whole");
    }

    [Fact]
    public async Task DivideStillDividesWhenTheDivisorIsNotZero()
    {
        var result = await new DivideOperation().Calculate(
            new Dictionary<string, object?> { ["Left"] = 9m, ["Right"] = 4m },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(2.25m);
    }
}
