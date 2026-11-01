using Fdw.Services.Calculations.Abstractions.CalculationSources;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests.CalculationSources;

/// <summary>
/// Covers <see cref="CalculationSourceTypes"/> — the extensible registry of calculation catalog
/// origins. Built-in options are "Default" (codified) and "Configuration" (calc.CalculationEntity).
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class CalculationSourceTypesTests
{
    [Fact]
    public void AllContainsDefaultAndConfiguration()
    {
        var all = CalculationSourceTypes.All();

        all.ShouldContain(s => s.Name == "Default");
        all.ShouldContain(s => s.Name == "Configuration");
    }
}
