using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1745: the key columns of a Distinct tool can come from a list run parameter. The rules say which parameter fits; the run puts the
/// supplied columns on a copy of the stage with a list of its own and never changes the stored stage, which is shared and cached.
/// </summary>
public sealed class StageListBindingTests
{
    private static PipelineComponentConfiguration Distinct(params string[] keys) => new()
    {
        Name = "once",
        ComponentType = "Distinct",
        BatchSize = 100,
        Stages =
        [
            new PipelineTransformConfiguration
            {
                Name = "once",
                OperationType = "Distinct",
                GroupByFields = [.. keys.Select((key, ordinal) => new PipelineTransformGroupByFieldConfiguration { Name = key, FieldName = key, Ordinal = ordinal })],
            },
        ],
    };

    private static PipelineComponentConfiguration Sort(params (string Field, string Direction)[] fields) => new()
    {
        Name = "ordered",
        ComponentType = "Sort",
        BatchSize = 100,
        Stages =
        [
            new PipelineTransformConfiguration
            {
                Name = "ordered",
                OperationType = "Sort",
                SortFields = [.. fields.Select((field, ordinal) => new PipelineTransformSortFieldConfiguration { Name = field.Field, FieldName = field.Field, Direction = field.Direction, Ordinal = ordinal })],
            },
        ],
    };

    private static PipelineComponentSettingBindingConfiguration Bind(string setting, string parameter) => new() { SettingName = setting, ParameterName = parameter };

    private static GraphPipelineConfiguration Graph(PipelineComponentConfiguration component, params PipelineParameterConfiguration[] parameters) => new()
    {
        Name = "Distinctly",
        Parameters = [.. parameters],
        Components = [component],
    };

    private static PipelineParameterConfiguration Parameter(string name, string type, bool isList) => new() { Name = name, ParameterType = type, IsList = isList };

    private static string[] Reasons(GraphPipelineConfiguration graph) => [.. SettingBindingRules.Problems(graph).Select(problem => problem.Reason)];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheKeyColumnsOfADistinctBoundToAListOfTextHaveNoProblem()
    {
        var component = Distinct();
        component.SettingBindings = [Bind("distinct", "Keys")];

        Reasons(Graph(component, Parameter("Keys", "String", isList: true))).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheKeyColumnsOfADistinctAreRefusedASingleValueParameterAndAListOfAnotherType()
    {
        var single = Distinct();
        single.SettingBindings = [Bind("distinct", "One")];
        var numbers = Distinct();
        numbers.SettingBindings = [Bind("distinct", "Numbers")];

        Reasons(Graph(single, Parameter("One", "String", isList: false))).ShouldHaveSingleItem().ShouldContain("takes a list parameter");
        Reasons(Graph(numbers, Parameter("Numbers", "Int32", isList: true))).ShouldHaveSingleItem().ShouldContain("declared as Int32");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheRunPutsTheSuppliedKeyColumnsOnACopyOfTheStageAndLeavesTheStoredStageAlone()
    {
        var stored = Distinct();
        stored.SettingBindings = [Bind("distinct", "Keys")];
        var storedStage = stored.Stages[0];
        var context = GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["keys"] = new List<object> { "City", "StateProvince" } });

        var applied = BoundSettings.Apply(stored, context);

        applied.IsSuccess.ShouldBeTrue(applied.CurrentMessage);
        var stage = applied.Value!.Stages.ShouldHaveSingleItem();
        stage.ShouldNotBeSameAs(storedStage);
        stage.GroupByFields.OrderBy(field => field.Ordinal).Select(field => field.FieldName).ShouldBe(["City", "StateProvince"]);
        stored.Stages[0].ShouldBeSameAs(storedStage);
        storedStage.GroupByFields.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStageListParameterTheRunWasGivenNoValueForOrOfTheWrongShapeFailsTheRunNamingIt()
    {
        var stored = Distinct();
        stored.SettingBindings = [Bind("distinct", "Keys")];

        BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?>())).IsFailure.ShouldBeTrue();
        BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Keys"] = "City" })).IsFailure.ShouldBeTrue();
        BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Keys"] = new List<object>() })).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheFieldsOfASortBoundToAListOfTextHaveNoProblemAndASingleValueParameterIsRefused()
    {
        var bound = Sort();
        bound.SettingBindings = [Bind("sort", "Order")];
        var single = Sort();
        single.SettingBindings = [Bind("sort", "One")];

        Reasons(Graph(bound, Parameter("Order", "String", isList: true))).ShouldBeEmpty();
        Reasons(Graph(single, Parameter("One", "String", isList: false))).ShouldHaveSingleItem().ShouldContain("takes a list parameter");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheRunPutsTheSuppliedSortFieldsWithTheirDirectionsOnACopyOfTheStage()
    {
        var stored = Sort(("Id", "Ascending"));
        stored.SettingBindings = [Bind("sort", "Order")];
        var storedStage = stored.Stages[0];
        var context = GraphTestKit.ContextWith(
            Graph(stored), new Dictionary<string, object?> { ["order"] = new List<object> { "City asc", "State Province DESC", "Id descending" } });

        var applied = BoundSettings.Apply(stored, context);

        applied.IsSuccess.ShouldBeTrue(applied.CurrentMessage);
        var fields = applied.Value!.Stages.ShouldHaveSingleItem().SortFields.OrderBy(field => field.Ordinal).ToList();
        fields.Select(field => field.FieldName).ShouldBe(["City", "State Province", "Id"]);
        fields.Select(field => field.Direction).ShouldBe(["Ascending", "Descending", "Descending"]);
        storedStage.SortFields.ShouldHaveSingleItem().FieldName.ShouldBe("Id");
        stored.Stages[0].ShouldBeSameAs(storedStage);
    }

    [Theory]
    [InlineData("City")]
    [InlineData("City up")]
    [InlineData("asc")]
    [InlineData(" desc")]
    public void ASortItemWithoutAFieldAndAnExplicitDirectionIsRefusedNamingTheParameter(string item)
    {
        var stored = Sort();
        stored.SettingBindings = [Bind("sort", "Order")];

        var applied = BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Order"] = new List<object> { item } }));

        applied.IsFailure.ShouldBeTrue();
    }
}
