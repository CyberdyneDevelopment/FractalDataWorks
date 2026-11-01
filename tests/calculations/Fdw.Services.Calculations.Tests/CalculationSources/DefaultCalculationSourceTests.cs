using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.CalculationSources;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests.CalculationSources;

/// <summary>
/// Covers <see cref="DefaultCalculationSource"/> — the codified source that surfaces the ship-with-code
/// scalar aggregation operators (Sum, Average, Count, Min, Max, Percentile) from
/// <c>Fdw.Calculations.Abstractions.CalculationTypeOptions.CalculationTypes</c>. No calc.CalculationEntity
/// rows are ever written for this source.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class DefaultCalculationSourceTests
{
    private static CalculationSourceContext CreateContext()
        => new(Mock.Of<ICalculationEntityService>(), NullLoggerFactory.Instance);

    [Fact]
    public async Task ListReturnsCodifiedOperatorsTaggedAsDefault()
    {
        var sut = new DefaultCalculationSource();

        var result = await sut.List(CreateContext(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeEmpty();
        var sum = result.Value!.Single(i => i.Name == "Sum");
        sum.CalculationSource.ShouldBe("Default");
        sum.OperatorId.ShouldNotBeNull();
        sum.CalculationEntityId.ShouldBeNull();
        sum.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task ResolveByNameKnownOperatorReturnsSuccess()
    {
        var sut = new DefaultCalculationSource();

        var result = await sut.Resolve("Sum", CreateContext(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Sum");
        result.Value!.CalculationSource.ShouldBe("Default");
    }

    [Fact]
    public async Task ResolveByNameUnknownOperatorReturnsFailure()
    {
        var sut = new DefaultCalculationSource();

        var result = await sut.Resolve("Bogus", CreateContext(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveByIdAlwaysReturnsFailure()
    {
        var sut = new DefaultCalculationSource();

        var result = await sut.Resolve(Guid.NewGuid(), CreateContext(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
