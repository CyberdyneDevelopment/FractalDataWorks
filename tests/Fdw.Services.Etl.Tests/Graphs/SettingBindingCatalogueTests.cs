using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests that the catalogue's statements about which settings a run can supply and which read <c>{{Name}}</c> (FDW-1768) agree with the
/// code that checks and renders them, so a form built from the catalogue offers what the save accepts.
/// </summary>
public sealed class SettingBindingCatalogueTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheSettingsThatShapeTheGraphAreTheThreeTheBindingRulesAlwaysRefused()
    {
        var shaping = ComponentTypes.All().SelectMany(kind => kind.Settings).Where(setting => setting.ShapesGraph).Select(setting => setting.Name).Distinct().Order();

        shaping.ShouldBe(["errorOutput", "stage", "unmatchedOutput"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheSettingsThatReadParameterTextAreTheNamesOfASourceOrDestination()
    {
        var kinds = ComponentTypes.All().Where(kind => kind.Settings.Any(setting => setting.TakesParameterText)).ToList();

        kinds.ShouldAllBe(kind => kind.Category == "Source" || kind.Category == "Destination");
        kinds.SelectMany(kind => kind.Settings).Where(setting => setting.TakesParameterText).Select(setting => setting.Name).Distinct().Order()
            .ShouldBe(["container", "dataSet", "dataStore", "path"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EverySettingTheCatalogueFlagsIsOneAComponentKeeps()
    {
        var flagged = ComponentTypes.All().SelectMany(kind => kind.Settings).Where(setting => setting.ShapesGraph || setting.TakesParameterText);

        flagged.Where(setting => ComponentSettingBindings.Find(setting.Name) is null).Select(setting => setting.Name).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheBindingRuleRefusesExactlyTheSettingsTheCatalogueFlags()
    {
        foreach (var kind in ComponentTypes.All())
        {
            foreach (var setting in kind.Settings)
            {
                SettingBindingRules.CanBindComponentSetting(kind, setting.Name).ShouldBe(!setting.ShapesGraph, $"{kind.Name}.{setting.Name}");
            }
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AKindThatDoesNotExistHasNothingToRefuse()
    {
        SettingBindingRules.CanBindComponentSetting(ComponentTypes.NotFound, "stage").ShouldBeTrue();
    }
}
