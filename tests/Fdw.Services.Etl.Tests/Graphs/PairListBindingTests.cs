using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1745: the mappings on the input of a component with one input can come from a list run parameter whose items are field map pairs, <c>Source -&gt; Target</c>.
/// The rules say which parameter fits; the run puts the supplied maps on a copy of the component with rows of their own and never changes
/// the stored component, whose rows are shared and cached.
/// </summary>
public sealed class PairListBindingTests
{
    private static PipelineComponentConfiguration Map(params (string Source, string Target)[] maps) => new()
    {
        Name = "rename",
        ComponentType = "Filter",
        Fields =
        [
            .. maps.Select((map, ordinal) => new PipelineComponentFieldConfiguration { SettingName = "mappings", InputName = "input", Ordinal = ordinal, SourceField = map.Source, TargetField = map.Target }),
        ],
    };

    private static PipelineComponentSettingBindingConfiguration Bind(string setting, string parameter) => new() { SettingName = setting, ParameterName = parameter };

    private static GraphPipelineConfiguration Graph(PipelineComponentConfiguration component, params PipelineParameterConfiguration[] parameters) => new()
    {
        Name = "Renamed",
        Parameters = [.. parameters],
        Components = [component],
    };

    private static PipelineParameterConfiguration Parameter(string name, string type, bool isList) => new() { Name = name, ParameterType = type, IsList = isList };

    private static string[] Reasons(GraphPipelineConfiguration graph) => [.. SettingBindingRules.Problems(graph).Select(problem => problem.Reason)];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheMapsOfAMapBoundToAListOfTextHaveNoProblem()
    {
        var component = Map();
        component.SettingBindings = [Bind("mappings", "Pairs")];

        Reasons(Graph(component, Parameter("Pairs", "String", isList: true))).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheMapsOfAMapAreRefusedASingleValueParameterAListOfAnotherTypeAndMapsThatAreAlsoStated()
    {
        var single = Map();
        single.SettingBindings = [Bind("mappings", "One")];
        var numbers = Map();
        numbers.SettingBindings = [Bind("mappings", "Numbers")];
        var stated = Map(("Name", "BreweryName"));
        stated.SettingBindings = [Bind("mappings", "Pairs")];

        Reasons(Graph(single, Parameter("One", "String", isList: false))).ShouldHaveSingleItem().ShouldContain("takes a list parameter");
        Reasons(Graph(numbers, Parameter("Numbers", "Int32", isList: true))).ShouldHaveSingleItem().ShouldContain("declared as Int32");
        Reasons(Graph(stated, Parameter("Pairs", "String", isList: true))).ShouldHaveSingleItem().ShouldContain("states a value for 'mappings'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheBringOfALookupIsNotASettingARunCanSupplyAsAList()
    {
        var lookup = new PipelineComponentConfiguration { Name = "find", ComponentType = "Lookup", SettingBindings = [Bind("bring", "Pairs")] };

        Reasons(Graph(lookup, Parameter("Pairs", "String", isList: true))).ShouldHaveSingleItem().ShouldContain("not a single-value setting");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheRunPutsTheSuppliedMapsOnACopyAndLeavesTheStoredComponentAlone()
    {
        var stored = Map();
        stored.Fields.Add(new PipelineComponentFieldConfiguration { SettingName = "other", SourceField = "Keep", TargetField = "Kept" });
        stored.SettingBindings = [Bind("mappings", "Pairs")];
        var storedFields = stored.Fields;
        var context = GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["pairs"] = new List<object> { "Name -> BreweryName", " City->Town " } });

        var applied = BoundSettings.Apply(stored, context);

        applied.IsSuccess.ShouldBeTrue(applied.CurrentMessage);
        var maps = applied.Value!.Fields.Where(field => field.SettingName == "mappings").OrderBy(field => field.Ordinal).ToList();
        maps.Select(field => (field.SourceField, field.TargetField)).ShouldBe([("Name", "BreweryName"), ("City", "Town")]);
        maps.ShouldAllBe(field => field.InputName == "input");
        maps.ShouldAllBe(field => field.ConstantType == null && field.ConstantText == null);
        applied.Value.Fields.ShouldContain(field => field.SettingName == "other");
        stored.Fields.ShouldBeSameAs(storedFields);
        stored.Fields.ShouldHaveSingleItem().SettingName.ShouldBe("other");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AMapsParameterTheRunWasGivenNoValueForOrOfTheWrongShapeFailsTheRunNamingIt()
    {
        var stored = Map();
        stored.SettingBindings = [Bind("mappings", "Pairs")];

        BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?>())).IsFailure.ShouldBeTrue();
        BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Pairs"] = "Name -> Town" })).IsFailure.ShouldBeTrue();
        BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Pairs"] = new List<object>() })).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AComponentWithSeveralInputsRefusesMappingsFromAListBecauseTheListNamesNoInput()
    {
        var stored = new PipelineComponentConfiguration { Name = "join", ComponentType = "MergeJoin", SettingBindings = [Bind("mappings", "Pairs")] };

        var applied = BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Pairs"] = new List<object> { "A -> B" } }));

        applied.IsFailure.ShouldBeTrue();
        applied.CurrentMessage.ShouldNotBeNull().ShouldContain("one input");
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("Name ->")]
    [InlineData("-> Town")]
    [InlineData("  -> ")]
    [InlineData("A -> B -> C")]
    public void AMapItemThatIsNotASourceAnArrowAndATargetIsRefusedNamingTheParameter(string item)
    {
        var stored = Map();
        stored.SettingBindings = [Bind("mappings", "Pairs")];

        var applied = BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Pairs"] = new List<object> { item } }));

        applied.IsFailure.ShouldBeTrue();
    }
}
