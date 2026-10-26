using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Tools;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests that the places a scalar setting of the catalogue is kept agree with the catalogue (FDW-1396): a kind that lists a
/// scalar setting nothing can store would be accepted by the console and lost on save.
/// </summary>
public sealed class ComponentSettingBindingsTests
{
    private static readonly string[] ListKinds = ["ColumnList", "FieldMaps", "InputMappings", "SplitOutputs", "ValueSources", "Steps", "Parameters"];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryKindWithInputsListsTheMappingsSettingWithItsOwnKindAndNoOtherSettingHasThatKind()
    {
        foreach (var kind in ComponentTypes.All().Where(kind => kind.Inputs.Count > 0))
        {
            kind.Settings.Single(setting => setting.Kind == ComponentTypeBase.MappingsKind).Name.ShouldBe(ComponentTypeBase.MappingsSetting, kind.Name);
        }

        ComponentTypes.All().Where(kind => kind.Inputs.Count == 0).SelectMany(kind => kind.Settings)
            .ShouldNotContain(setting => setting.Kind == ComponentTypeBase.MappingsKind);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryScalarSettingTheCatalogueListsHasABindingOfTheSameKind()
    {
        var scalars = ComponentTypes.All()
            .SelectMany(kind => kind.Settings
                // A tool's stage members are stored as the one stage the tool holds, not one by one.
                .Where(setting => kind is not StageToolComponentTypeBase tool || !tool.MemberNames.Contains(setting.Name))
                .Select(setting => (Kind: kind.Name, Setting: setting)))
            .Where(pair => !ListKinds.Contains(pair.Setting.Kind));

        foreach (var (kind, setting) in scalars)
        {
            var binding = ComponentSettingBindings.Find(setting.Name).ShouldNotBeNull($"{kind}: {setting.Name}");
            var expected = setting.Kind is "Text" or "SqlText" or "DataSet" or "DataStore" or "Container" or "LoadStepKinds" or "SoftDeleteModes" or "JoinKinds" or "SplitRoutings" or "RunPoints" or "Variable"
                ? "Text"
                : setting.Kind;
            binding.Kind.ShouldBe(expected, $"{kind}: {setting.Name}");
        }
    }
}
