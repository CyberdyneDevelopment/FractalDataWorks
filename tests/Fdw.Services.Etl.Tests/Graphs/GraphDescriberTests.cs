using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Describing;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for describing a graph draft (FDW-1397): the fields at each node follow from the data sets and the settings of each
/// component, a node that cannot be known says so, and a half-built graph is described as far as it goes.
/// </summary>
public sealed class GraphDescriberTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<GraphField>> DataSets =
        new Dictionary<string, IReadOnlyList<GraphField>>
        {
            ["Orders"] = [new GraphField("Id", "Int32"), new GraphField("Amount", "Decimal"), new GraphField("PersonId", "Int32")],
            ["People"] = [new GraphField("PersonId", "Int32"), new GraphField("Name", "String")],
        };

    private static DescribeContext Context() => new((name, _) =>
        Task.FromResult<IGenericResult<IReadOnlyList<GraphField>>>(
            DataSets.TryGetValue(name, out var fields)
                ? GenericResult<IReadOnlyList<GraphField>>.Success(fields)
                : GenericResult<IReadOnlyList<GraphField>>.Failure(new Fdw.Messages.GenericMessage($"no data set '{name}'"))));

    private static async Task<IReadOnlyList<GraphNodeFields>> Describe(GraphPipelineConfiguration graph)
    {
        var result = await GraphDescriber.Describe(graph, Context(), NullLogger.Instance, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        return result.Value!;
    }

    private static IReadOnlyList<GraphField>? At(IReadOnlyList<GraphNodeFields> nodes, string component, string node, bool input) =>
        nodes.Single(n => n.Component == component && n.Node == node && n.IsInput == input).Fields;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ASourceWhoseDataSetIsNamedByAParameterHasUnknownFieldsUntilTheRunChoosesIt()
    {
        var graph = Graph([DataSetSource("src", "{{Which}}")], []);

        var nodes = await Describe(graph);

        At(nodes, "src", "output", false).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASortWhoseStageCouldNotBeReadYetStillPassesItsInputFieldsOn()
    {
        var sort = new PipelineComponentConfiguration { Name = "order", ComponentType = "Sort", BatchSize = 10 };
        var graph = Graph([DataSetSource("src"), sort], [Link("src", "order")], Dropped("order"));

        var nodes = await Describe(graph);

        At(nodes, "order", "output", false)!.Select(f => f.Name).ShouldBe(["Id", "Amount", "PersonId"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASourceShowsTheFieldsOfItsDataSetAndAMappedInputShowsItsTargetsTyped()
    {
        var map = Mapped("map", ("Key", "Id"), ("Total", "Amount"));
        map.Fields.Add(new PipelineComponentFieldConfiguration
        {
            SettingName = "mappings", InputName = "input", Ordinal = 2, TargetField = "Origin", ConstantType = "String", ConstantText = "orders",
        });
        var graph = Graph([DataSetSource("src"), map], [Link("src", "map")], Dropped("map"));

        var nodes = await Describe(graph);

        At(nodes, "src", "output", false)!.Select(f => f.Name).ShouldBe(["Id", "Amount", "PersonId"]);
        At(nodes, "map", "input", true)!.Count.ShouldBe(3);
        var output = At(nodes, "map", "output", false)!;
        output.Select(f => (f.Name, f.Type)).ShouldBe([("Key", "Int32"), ("Total", "Decimal"), ("Origin", "String")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AComponentWhoseMappingsComeFromAParameterHasUnknownOutputFieldsUntilTheRunChoosesThem()
    {
        var map = Mapped("map");
        map.SettingBindings = [new PipelineComponentSettingBindingConfiguration { SettingName = "mappings", ParameterName = "Pairs" }];
        var graph = Graph([DataSetSource("src"), map], [Link("src", "map")], Dropped("map"));

        var nodes = await Describe(graph);

        At(nodes, "map", "input", true)!.Count.ShouldBe(3);
        At(nodes, "map", "output", false).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AJoinShowsTheLeftFieldsAndTheRightFieldsWithThePrefix()
    {
        var join = new PipelineComponentConfiguration
        {
            Name = "join", ComponentType = "MergeJoin", JoinKind = "Inner", RightPrefix = "p_", BatchSize = 10,
        };
        var graph = Graph(
            [DataSetSource("orders", "Orders"), DataSetSource("people", "People"), join],
            [Link("orders", "join", input: "left"), Link("people", "join", input: "right")],
            Dropped("join"));

        var nodes = await Describe(graph);

        At(nodes, "join", "output", false)!.Select(f => f.Name)
            .ShouldBe(["Id", "Amount", "PersonId", "p_PersonId", "p_Name"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASplitShowsTheInputFieldsAtEveryOutputItNamesTheUnmatchedOneIncluded()
    {
        var split = new PipelineComponentConfiguration { Name = "split", ComponentType = "ConditionalSplit", Routing = "FirstMatch", UnmatchedOutput = "rest" };
        split.SplitOutputs.Add(new PipelineComponentSplitOutputConfiguration { Ordinal = 0, OutputName = "big", FilterExpression = "Amount > 100" });
        var graph = Graph([DataSetSource("src"), split], [Link("src", "split")]);

        var nodes = await Describe(graph);

        At(nodes, "split", "big", false)!.Count.ShouldBe(3);
        At(nodes, "split", "rest", false)!.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFilterShowsItsInputFieldsAndAnAggregateShowsItsGroupAndAggregateFields()
    {
        var aggregate = new PipelineComponentConfiguration { Name = "total", ComponentType = "Aggregate", BatchSize = 10 };
        aggregate.Stages.Add(new PipelineTransformConfiguration
        {
            Name = "total",
            OperationType = "Aggregate",
            IsEnabled = true,
            GroupByFields = [new PipelineTransformGroupByFieldConfiguration { FieldName = "PersonId", Ordinal = 1 }],
            Aggregations = [new PipelineTransformAggregationConfiguration { SourceField = "Amount", AggregateFunction = "Sum", OutputField = "Spent", ExecutionOrder = 1 }],
        });
        var graph = Graph(
            [DataSetSource("src"), FilterStage("keep", "Id > 0"), aggregate],
            [Link("src", "keep"), Link("keep", "total")],
            Dropped("total"));

        var nodes = await Describe(graph);

        At(nodes, "keep", "output", false)!.Select(f => f.Name).ShouldBe(["Id", "Amount", "PersonId"]);
        At(nodes, "total", "output", false)!.Select(f => f.Name).ShouldBe(["PersonId", "Spent"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AValidateStageKeepsItsInputFieldsAndItsErrorOutputAddsTheComponentAndTheReason()
    {
        var validate = new PipelineComponentConfiguration { Name = "check", ComponentType = "Validate", ErrorOutput = "bad" };
        validate.Stages.Add(new PipelineTransformConfiguration { Name = "check", OperationType = "Validate", OnFail = "Reject", IsEnabled = true });
        var graph = Graph([DataSetSource("src"), validate], [Link("src", "check")], Dropped("check"), Dropped("check", "bad"));

        var nodes = await Describe(graph);

        At(nodes, "check", "output", false)!.Select(f => f.Name).ShouldBe(["Id", "Amount", "PersonId"]);
        At(nodes, "check", "bad", false)!.Select(f => f.Name).ShouldBe(["Id", "Amount", "PersonId", "ErrorComponent", "ErrorReason"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AContainerSourceAndEverythingFedByItHaveNoKnownFieldsNotAnEmptyList()
    {
        var container = new PipelineComponentConfiguration
        {
            Name = "file", ComponentType = "ContainerSource", DataStoreName = "Files", ContainerName = "orders.csv", BatchSize = 100,
        };
        var split = new PipelineComponentConfiguration { Name = "split", ComponentType = "ConditionalSplit", Routing = "FirstMatch", UnmatchedOutput = "rest" };
        var graph = Graph([container, split], [Link("file", "split")]);

        var nodes = await Describe(graph);

        At(nodes, "file", "output", false).ShouldBeNull();
        At(nodes, "split", "input", true).ShouldBeNull();
        At(nodes, "split", "rest", false).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANodeNothingFeedsHasNoFieldsYetAndTheGraphIsStillDescribed()
    {
        var graph = Graph([Mapped("map", ("A", "B"))], []);

        var nodes = await Describe(graph);

        At(nodes, "map", "input", true).ShouldBeNull();
        At(nodes, "map", "output", false)!.Single().Name.ShouldBe("A");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADataSetThatCannotBeReadFailsTheDescriptionNamingIt()
    {
        var graph = Graph([DataSetSource("src", "Missing")], []);

        var result = await GraphDescriber.Describe(graph, Context(), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ACalculateToolAddsItsFieldsAndADistinctToolKeepsItsInputFields()
    {
        var calculate = new PipelineComponentConfiguration { Name = "calc", ComponentType = "Calculate" };
        calculate.Stages.Add(new PipelineTransformConfiguration
        {
            Name = "calc", OperationType = "Calculate", IsEnabled = true,
            Calculations = [new PipelineTransformCalculationConfiguration { OutputField = "Double", Expression = "Amount * 2", ExecutionOrder = 1 }],
        });
        var distinct = new PipelineComponentConfiguration { Name = "once", ComponentType = "Distinct", BatchSize = 10 };
        distinct.Stages.Add(new PipelineTransformConfiguration
        {
            Name = "once", OperationType = "Distinct", IsEnabled = true,
            GroupByFields = [new PipelineTransformGroupByFieldConfiguration { FieldName = "PersonId", Ordinal = 1 }],
        });
        var graph = Graph(
            [DataSetSource("src"), calculate, distinct],
            [Link("src", "calc"), Link("calc", "once")],
            Dropped("once"));

        var nodes = await Describe(graph);

        At(nodes, "calc", "output", false)!.Select(f => f.Name).ShouldBe(["Id", "Amount", "PersonId", "Double"]);
        At(nodes, "once", "output", false)!.Select(f => f.Name).ShouldBe(["Id", "Amount", "PersonId", "Double"]);
    }

    // ── FDW-2156: a field says which field of which data set it came from ──

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFieldOfASourceCarriesTheDataSetAndFieldItWasReadFrom()
    {
        var nodes = await Describe(Graph([DataSetSource("src")], []));

        At(nodes, "src", "output", false)!.ShouldAllBe(field => field.Origin != null);
        At(nodes, "src", "output", false)!.Single(field => field.Name == "Amount").Origin.ShouldBe(new GraphFieldOrigin("Orders", "Amount"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AMapRenameKeepsTheOriginOfItsSourceAndAConstantHasNone()
    {
        var map = Mapped("map", ("Key", "Id"));
        map.Fields.Add(new PipelineComponentFieldConfiguration
        {
            SettingName = "mappings", InputName = "input", Ordinal = 2, TargetField = "Origin", ConstantType = "String", ConstantText = "orders",
        });

        var nodes = await Describe(Graph([DataSetSource("src"), map], [Link("src", "map")], Dropped("map")));

        var fields = At(nodes, "map", "output", false)!;
        fields.Single(field => field.Name == "Key").Origin.ShouldBe(new GraphFieldOrigin("Orders", "Id"));
        fields.Single(field => field.Name == "Origin").Origin.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APassThroughToolKeepsTheOriginOfEveryField()
    {
        var sort = new PipelineComponentConfiguration { Name = "order", ComponentType = "Sort", BatchSize = 10 };

        var nodes = await Describe(Graph([DataSetSource("src"), sort], [Link("src", "order")], Dropped("order")));

        At(nodes, "order", "output", false)!.ShouldAllBe(field => field.Origin != null);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnAggregateKeepsTheOriginOfItsGroupByFieldAndGivesItsAggregateNone()
    {
        var aggregate = new PipelineComponentConfiguration { Name = "total", ComponentType = "Aggregate", BatchSize = 10 };
        aggregate.Stages.Add(new PipelineTransformConfiguration
        {
            Name = "total",
            OperationType = "Aggregate",
            IsEnabled = true,
            GroupByFields = [new PipelineTransformGroupByFieldConfiguration { FieldName = "PersonId", Ordinal = 1 }],
            Aggregations = [new PipelineTransformAggregationConfiguration { SourceField = "Amount", AggregateFunction = "Sum", OutputField = "Spent", ExecutionOrder = 1 }],
        });

        var nodes = await Describe(Graph([DataSetSource("src"), aggregate], [Link("src", "total")], Dropped("total")));

        var fields = At(nodes, "total", "output", false)!;
        fields.Single(field => field.Name == "PersonId").Origin.ShouldBe(new GraphFieldOrigin("Orders", "PersonId"));
        fields.Single(field => field.Name == "Spent").Origin.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AJoinKeepsTheOriginOfBothSidesAndThePrefixedFieldNamesTheRightFieldItCameFrom()
    {
        var join = new PipelineComponentConfiguration
        {
            Name = "join", ComponentType = "MergeJoin", JoinKind = "Inner", RightPrefix = "p_", BatchSize = 10,
        };

        var nodes = await Describe(Graph(
            [DataSetSource("orders", "Orders"), DataSetSource("people", "People"), join],
            [Link("orders", "join", input: "left"), Link("people", "join", input: "right")],
            Dropped("join")));

        var fields = At(nodes, "join", "output", false)!;
        fields.Single(field => field.Name == "Amount").Origin.ShouldBe(new GraphFieldOrigin("Orders", "Amount"));
        fields.Single(field => field.Name == "p_Name").Origin.ShouldBe(new GraphFieldOrigin("People", "Name"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AUnionKeepsAnOriginOnlyWhenEveryInputAgreesOnIt()
    {
        var union = new PipelineComponentConfiguration { Name = "all", ComponentType = "UnionAll", BatchSize = 10 };

        var same = await Describe(Graph(
            [DataSetSource("a", "Orders"), DataSetSource("b", "Orders"), union],
            [Link("a", "all", input: "in1"), Link("b", "all", input: "in2")],
            Dropped("all")));
        var different = await Describe(Graph(
            [DataSetSource("a", "Orders"), DataSetSource("b", "People"), union],
            [Link("a", "all", input: "in1"), Link("b", "all", input: "in2")],
            Dropped("all")));

        At(same, "all", "output", false)!.Single(field => field.Name == "Id").Origin.ShouldBe(new GraphFieldOrigin("Orders", "Id"));
        At(different, "all", "output", false)!.ShouldAllBe(field => field.Origin == null);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnInputNodeStatesWhatArrivesAndWhatTheComponentSeesAfterItsMapping()
    {
        var map = Mapped("map", ("Key", "Id"));

        var nodes = await Describe(Graph([DataSetSource("src"), map], [Link("src", "map")], Dropped("map")));

        var input = nodes.Single(node => node.Component == "map" && node.IsInput);
        input.Fields!.Select(field => field.Name).ShouldBe(["Id", "Amount", "PersonId"]);
        var seen = input.Seen.ShouldNotBeNull().ShouldHaveSingleItem();
        seen.Name.ShouldBe("Key");
        seen.Origin.ShouldBe(new GraphFieldOrigin("Orders", "Id"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnInputWithNoMappingSeesWhatArrivesAndAnOutputStatesNothingSeen()
    {
        var nodes = await Describe(Graph([DataSetSource("src"), Mapped("map")], [Link("src", "map")], Dropped("map")));

        var input = nodes.Single(node => node.Component == "map" && node.IsInput);
        input.Seen.ShouldBe(input.Fields);
        nodes.Single(node => node.Component == "map" && !node.IsInput).Seen.ShouldBeNull();
    }
}
