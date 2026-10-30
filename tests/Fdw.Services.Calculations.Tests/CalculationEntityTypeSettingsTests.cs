using System.Linq;
using System.Reflection;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Tests.TestSupport;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Covers the settings a calculation entity type declares: each one names a configuration member create binds, so the
/// declaration the type-collection values list and the refusal of an undeclared member cannot drift from the configuration class.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class CalculationEntityTypeSettingsTests
{
    private static CalculationEntityTypeBase TypeOf(string name) => name switch
    {
        "Windowed" => new WindowedCalculationEntityType(),
        _ => throw new System.ArgumentOutOfRangeException(nameof(name), name, "No calculation entity type of that name is covered here")
    };

    [Theory]
    [InlineData("Windowed")]
    public void EverySettingNamesAWritableMemberOfTheKindsConfiguration(string kind)
    {
        var type = TypeOf(kind);

        type.Settings.ShouldNotBeEmpty();
        foreach (var setting in type.Settings)
        {
            var member = type.ConfigurationType.GetProperty(
                setting.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            member.ShouldNotBeNull(setting.Name);
            member.CanWrite.ShouldBeTrue(setting.Name);
        }
    }

    [Theory]
    [InlineData("Windowed")]
    public void SettingNamesAreUniqueIgnoringCase(string kind)
    {
        var names = TypeOf(kind).Settings.Select(setting => setting.Name).ToList();

        names.Distinct(System.StringComparer.OrdinalIgnoreCase).Count().ShouldBe(names.Count);
    }

    [Fact]
    public void WindowedRequiresATargetFieldAndAWindowFunctionChosenFromTheWindowedCalculationTypes()
    {
        var settings = new WindowedCalculationEntityType().Settings.ToDictionary(setting => setting.Name);

        settings["targetField"].IsRequired.ShouldBeTrue();
        settings["targetField"].Kind.ShouldBe("Field");
        settings["windowFunction"].IsRequired.ShouldBeTrue();
        settings["windowFunction"].Kind.ShouldBe("WindowedCalculationTypes");
        settings.Count.ShouldBe(2);
    }

    [Fact]
    public void WindowedReadsTheValuesOfItsDeclaredSettingsFromAConfiguration()
    {
        var configuration = new WindowedCalculationConfiguration { TargetField = "WeightLbs", WindowFunction = "Sum", OutputFieldName = "unread" };

        var values = new WindowedCalculationEntityType().SettingValues(configuration);

        values.Count.ShouldBe(2);
        values["targetField"].ShouldBe("WeightLbs");
        values["windowFunction"].ShouldBe("Sum");
    }

    [Fact]
    public void ASettingValuesReadOfAnotherKindsConfigurationIsRefused()
    {
        var type = new WindowedCalculationEntityType();

        Should.Throw<System.ArgumentException>(() => type.SettingValues(new UnregisteredCalculationConfiguration()));
    }
}
