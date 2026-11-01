using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1745: a list of columns a component takes (a destination's key, compare and write columns) can come from a list run parameter.
/// The rules say which parameter fits; the run puts the supplied columns on a copy with a list of its own and never changes the stored
/// component, whose rows are shared and cached.
/// </summary>
public sealed class ListSettingBindingTests
{
    private static PipelineComponentConfiguration Destination() => new() { Name = "keep", ComponentType = "DataSetDestination" };

    private static PipelineComponentSettingBindingConfiguration Bind(string setting, string parameter) => new() { SettingName = setting, ParameterName = parameter };

    private static PipelineComponentColumnConfiguration Column(string setting, int ordinal, string name) => new() { SettingName = setting, Ordinal = ordinal, ColumnName = name };

    private static PipelineParameterConfiguration Parameter(string name, string type, bool isList) => new() { Name = name, ParameterType = type, IsList = isList };

    private static GraphPipelineConfiguration Graph(PipelineComponentConfiguration component, params PipelineParameterConfiguration[] parameters) => new()
    {
        Name = "Listed",
        Parameters = [.. parameters],
        Components = [component],
    };

    private static string[] Reasons(GraphPipelineConfiguration graph) => [.. SettingBindingRules.Problems(graph).Select(problem => problem.Reason)];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AColumnListBoundToAListOfTextHasNoProblem()
    {
        var component = Destination();
        component.SettingBindings = [Bind("writeColumns", "Cols")];

        Reasons(Graph(component, Parameter("Cols", "String", isList: true))).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AColumnListIsRefusedForASingleValueParameterAListOfAnotherTypeAndAListThatAlsoStatesColumns()
    {
        var component = Destination();
        component.Columns = [Column("keyColumns", 0, "Id")];
        component.SettingBindings = [Bind("writeColumns", "One"), Bind("compareColumns", "Numbers"), Bind("keyColumns", "Cols")];

        var reasons = Reasons(Graph(
            component, Parameter("One", "String", isList: false), Parameter("Numbers", "Int32", isList: true), Parameter("Cols", "String", isList: true)));

        reasons.Length.ShouldBe(3);
        reasons.ShouldContain(reason => reason.Contains("'writeColumns'") && reason.Contains("takes a list parameter") && reason.Contains("single String"));
        reasons.ShouldContain(reason => reason.Contains("'compareColumns'") && reason.Contains("type String") && reason.Contains("declared as Int32"));
        reasons.ShouldContain(reason => reason.Contains("states a value for 'keyColumns'"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ASingleValueSettingIsRefusedAListParameter()
    {
        var component = Destination();
        component.SettingBindings = [Bind("loadKind", "Cols")];

        var reasons = Reasons(Graph(component, Parameter("Cols", "String", isList: true)));

        reasons.ShouldHaveSingleItem().ShouldContain("takes a single-value parameter");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ABoundColumnListCountsAsStatedSoARequiredOneThatIsBoundIsNotMissing()
    {
        var component = Destination();
        component.SettingBindings = [Bind("writeColumns", "Cols")];

        ComponentSettings.Stated(component).ShouldContain("writeColumns");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheRunPutsTheSuppliedColumnsOnACopyWithAListOfItsOwnAndLeavesTheStoredComponentAlone()
    {
        var stored = Destination();
        stored.Columns = [Column("keyColumns", 0, "Id"), Column("writeColumns", 0, "Name")];
        stored.SettingBindings = [Bind("writeColumns", "Cols")];
        var storedRows = stored.Columns;
        var context = GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["cols"] = new List<object> { "City", "State", "Name" } });

        var applied = BoundSettings.Apply(stored, context);

        applied.IsSuccess.ShouldBeTrue(applied.CurrentMessage);
        applied.Value!.Columns.Where(column => column.SettingName == "writeColumns").OrderBy(column => column.Ordinal).Select(column => column.ColumnName)
            .ShouldBe(["City", "State", "Name"]);
        applied.Value.Columns.Where(column => column.SettingName == "keyColumns").Select(column => column.ColumnName).ShouldBe(["Id"]);
        stored.Columns.ShouldBeSameAs(storedRows);
        stored.Columns.Count.ShouldBe(2);
        stored.Columns.Single(column => column.SettingName == "writeColumns").ColumnName.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AListParameterTheRunWasGivenNoValueForOrOfTheWrongShapeFailsTheRunNamingIt()
    {
        var stored = Destination();
        stored.SettingBindings = [Bind("writeColumns", "Cols")];

        var none = BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?>()));
        var single = BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Cols"] = "Name" }));
        var empty = BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Cols"] = new List<object>() }));
        var numbers = BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Cols"] = new List<object> { 1, 2 } }));

        none.IsFailure.ShouldBeTrue();
        single.IsFailure.ShouldBeTrue();
        empty.IsFailure.ShouldBeTrue();
        numbers.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AListValueIsNeverReadAsTheTextOfASingleValueSetting()
    {
        var stored = Destination();
        stored.SettingBindings = [Bind("loadKind", "Cols")];

        var applied = BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Cols"] = new List<object> { "Upsert" } }));

        applied.IsFailure.ShouldBeTrue();
        stored.LoadKind.ShouldBeNull();
    }
}
