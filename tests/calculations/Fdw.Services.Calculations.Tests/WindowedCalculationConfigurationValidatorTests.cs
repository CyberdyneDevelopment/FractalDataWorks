using System.Linq;
using Fdw.Services.Calculations.Validators;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// The Windowed configuration validator checks what the kind declares (its settings) and the one member the runtime
/// reads for the result column (the entity's <c>ResultFieldName</c>), and nothing a client cannot send (FDW-4426).
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class WindowedCalculationConfigurationValidatorTests
{
    private readonly WindowedCalculationConfigurationValidator _validator = new();

    private static WindowedCalculationConfiguration DeclaredSettingsOnly() => new()
    {
        Name = "WeightTotal",
        TargetField = "WeightLbs",
        WindowFunction = "RunningTotal",
        ResultFieldName = "WeightTotal"
    };

    [Fact]
    public void AConfigurationMadeFromTheDeclaredSettingsAndTheEntitysResultFieldIsAccepted()
    {
        var result = _validator.Validate(DeclaredSettingsOnly());

        result.IsValid.ShouldBeTrue(string.Join("; ", result.Errors.Select(error => error.ErrorMessage)));
    }

    [Fact]
    public void EverySettingTheKindDeclaresAsRequiredIsRequiredByTheValidator()
    {
        var declared = new WindowedCalculationEntityType().Settings.Where(setting => setting.IsRequired).Select(setting => setting.Name);

        foreach (var name in declared)
        {
            var configuration = DeclaredSettingsOnly();
            if (string.Equals(name, "targetField", System.StringComparison.Ordinal)) configuration.TargetField = string.Empty;
            if (string.Equals(name, "windowFunction", System.StringComparison.Ordinal)) configuration.WindowFunction = string.Empty;

            _validator.Validate(configuration).Errors
                .ShouldContain(error => string.Equals(error.PropertyName, name, System.StringComparison.OrdinalIgnoreCase), name);
        }
    }

    [Fact]
    public void ARequestWithoutTheResultFieldIsRefusedBecauseTheRuntimeNamesNoOutputWithoutIt()
    {
        var configuration = DeclaredSettingsOnly();
        configuration.ResultFieldName = null;

        _validator.Validate(configuration).Errors.Select(error => error.PropertyName).ShouldBe([nameof(WindowedCalculationConfiguration.ResultFieldName)]);
    }

    [Fact]
    public void TheValidatorChecksNoMemberTheConfigurationDoesNotHave()
    {
        typeof(WindowedCalculationConfiguration).GetProperty("OutputFieldName").ShouldBeNull();
    }
}
