using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests that the catalogue of actions and loops and the code that stores and builds them agree (FDW-1403, FDW-2041): every action kind can be built,
/// and every single-value setting a kind lists has a place to be stored, so a kind cannot be added half-wired.
/// </summary>
public sealed class ActionCatalogueWiringTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EveryActionKindHasABuilderAndNoBuilderNamesAKindThatIsNotAnAction()
    {
        var kinds = ComponentTypes.All().Where(kind => kind.IsAction).Select(kind => kind.Name).ToList();
        kinds.Where(name => ReferenceEquals(FlowStepBuilders.ByName(name), FlowStepBuilders.NotFound)).ShouldBeEmpty();
        FlowStepBuilders.All().Select(builder => builder.Name).Where(name => !kinds.Contains(name)).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EverySingleValueSettingAnActionKindListsHasABindingOfTheSameKind()
    {
        // A setting a kind declares with its own data type (FDW-6133, FDW-6134) is held as a declared setting and has no binding: it is judged by the declared-setting tests.
        foreach (var (kind, setting) in ComponentTypes.All().Where(kind => kind.IsAction)
            .SelectMany(kind => kind.Settings.Where(setting => !ComponentSettingBindings.IsDeclared(kind, setting)).Select(setting => (Kind: kind.Name, Setting: setting)))
            .Where(pair => pair.Setting.Kind != "Parameters"))
        {
            var binding = ComponentSettingBindings.Find(setting.Name).ShouldNotBeNull($"{kind}: {setting.Name}");
            var expected = setting.Kind is "Number" or "Boolean" ? setting.Kind : "Text";
            binding.Kind.ShouldBe(expected, $"{kind}: {setting.Name}");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void OnlyTheLoopsAndTheContainerHoldComponentsAndTheActionsAreTheStepKinds()
    {
        ComponentTypes.All().Where(kind => kind.HoldsComponents).Select(kind => kind.Name).OrderBy(name => name, System.StringComparer.Ordinal)
            .ShouldBe(["Container", "DoWhile", "ForEach", "ForLoop", "While"]);
        ComponentTypes.All().Where(kind => kind.IsAction).Select(kind => kind.Name).OrderBy(name => name, System.StringComparer.Ordinal)
            .ShouldBe(["Ask", "Condition", "Container", "DoWhile", "ExecutePipeline", "ExecuteSql", "ForEach", "ForLoop", "Gate", "Notify", "ReadDataOptions", "Send", "SetVariable", "Switch", "While"]);
        ComponentTypes.All().Where(kind => kind.IsAction).ShouldAllBe(kind => kind.Inputs.Count == 0 && kind.Outputs.Count == 0);
    }
}
