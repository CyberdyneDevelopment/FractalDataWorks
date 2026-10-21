using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for reading a graph the API states into the stored configuration and writing it back (FDW-1396): what a save reads is
/// what a read writes, and what cannot be placed is refused naming the component and the setting.
/// </summary>
public sealed class GraphPayloadMappingTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static GraphPayload Sample()
    {
        var source = new GraphComponentPayload { Name = "src", Kind = "DataSetSource" };
        source.Settings["dataSet"] = Json("\"Orders\"");
        source.Settings["batchSize"] = Json("500");
        source.Layout = new GraphLayoutPayload { X = 10, Y = 20 };

        var map = new GraphComponentPayload { Name = "map", Kind = "Filter" };
        map.Settings["filterExpression"] = Json("\"Key >= 0\"");
        map.Settings["mappings"] = Json("[{\"input\":\"input\",\"target\":\"Key\",\"source\":\"Id\"},{\"input\":\"input\",\"target\":\"Source\",\"constant\":{\"type\":\"String\",\"value\":\"orders\"}}]");

        var split = new GraphComponentPayload { Name = "split", Kind = "ConditionalSplit" };
        split.Settings["outputs"] = Json("[{\"name\":\"big\",\"filter\":\"Amount > 100\"}]");
        split.Settings["routing"] = Json("\"FirstMatch\"");
        split.Settings["unmatchedOutput"] = Json("\"rest\"");

        return new GraphPayload
        {
            Name = "OrdersFlow",
            IsEnabled = true,
            LinkCapacity = 4,
            Components = [source, map, split],
            Links =
            [
                new GraphLinkPayload { From = new GraphNodePayload { Component = "src", Node = "output" }, To = new GraphNodePayload { Component = "map", Node = "input" } },
                new GraphLinkPayload { From = new GraphNodePayload { Component = "map", Node = "output" }, To = new GraphNodePayload { Component = "split", Node = "input" } },
            ],
            Dropped = [new GraphNodePayload { Component = "split", Node = "big" }, new GraphNodePayload { Component = "split", Node = "rest" }],
        };
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphIsReadIntoTheStoredConfigurationWithEachSettingInItsPlace()
    {
        var read = GraphPayloadReader.Read(Sample(), System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var graph = read.Value!;
        graph.Implementation.ShouldBe("Graph");
        graph.LinkCapacity.ShouldBe(4);
        var source = graph.Components.Single(c => c.Name == "src");
        source.DataSetName.ShouldBe("Orders");
        source.BatchSize.ShouldBe(500);
        source.LayoutX.ShouldBe(10);
        var map = graph.Components.Single(c => c.Name == "map");
        map.Fields.Count.ShouldBe(2);
        map.Fields.Single(f => f.TargetField == "Source").ConstantType.ShouldBe("String");
        var split = graph.Components.Single(c => c.Name == "split");
        split.SplitOutputs.Single().OutputName.ShouldBe("big");
        split.Routing.ShouldBe("FirstMatch");
        split.UnmatchedOutput.ShouldBe("rest");
        graph.Links.Count.ShouldBe(2);
        graph.DroppedOutputs.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AComponentsStageIsReadAndWrittenBack()
    {
        var payload = Sample();
        payload.Components[0].Settings["stage"] = Json("1");
        payload.Components[1].Settings["stage"] = Json("2");

        var read = GraphPayloadReader.Read(payload, System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.Components.Single(c => c.Name == "src").Stage.ShouldBe(1);
        read.Value.Components.Single(c => c.Name == "map").Stage.ShouldBe(2);
        GraphPayloadWriter.Write(read.Value).Components.Single(c => c.Name == "map").Settings["stage"].ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AParametersDefaultIsKeptWhenAGraphIsReadAndWrittenBack()
    {
        var payload = Sample();
        payload.Parameters.Add(new GraphParameterPayload { Name = "File", DataType = "String", DefaultValue = "orders.csv" });
        payload.Parameters.Add(new GraphParameterPayload { Name = "Which", DataType = "String", IsRequired = true });

        var read = GraphPayloadReader.Read(payload, System.Guid.NewGuid());
        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.Parameters.Single(parameter => parameter.Name == "File").DefaultText.ShouldBe("orders.csv");
        read.Value.Parameters.Single(parameter => parameter.Name == "Which").DefaultText.ShouldBeNull();

        var written = GraphPayloadWriter.Write(read.Value);

        written.Parameters.Single(parameter => parameter.Name == "File").DefaultValue.ShouldBe("orders.csv");
        written.Parameters.Single(parameter => parameter.Name == "Which").DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphThatIsReadAndWrittenBackStatesTheSameSettings()
    {
        var read = GraphPayloadReader.Read(Sample(), System.Guid.NewGuid());
        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);

        var written = GraphPayloadWriter.Write(read.Value!);

        written.Name.ShouldBe("OrdersFlow");
        written.Components.Select(c => c.Name).ShouldBe(["src", "map", "split"]);
        var source = written.Components.Single(c => c.Name == "src");
        source.Settings["dataSet"].ShouldBe("Orders");
        source.Settings["batchSize"].ShouldBe(500);
        source.Layout!.X.ShouldBe(10);
        var split = written.Components.Single(c => c.Name == "split");
        ((List<GraphSplitOutputPayload>)split.Settings["outputs"]!).Single().Filter.ShouldBe("Amount > 100");
        written.Links.Count.ShouldBe(2);
        written.Dropped.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphThatIsReadPassesTheShapeCheck()
    {
        var read = GraphPayloadReader.Read(Sample(), System.Guid.NewGuid());
        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);

        var judged = GraphShapeValidator.Validate(read.Value!, NullLogger.Instance);

        judged.IsSuccess.ShouldBeTrue(judged.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphThatDoesNotStateIsEnabledOrLinkCapacityIsRefused()
    {
        var noEnabled = Sample();
        noEnabled.IsEnabled = null;
        var noCapacity = Sample();
        noCapacity.LinkCapacity = null;

        GraphPayloadReader.Read(noEnabled, System.Guid.NewGuid()).IsFailure.ShouldBeTrue();
        GraphPayloadReader.Read(noCapacity, System.Guid.NewGuid()).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASettingOfTheWrongTypeIsRefusedNamingTheComponentAndTheSetting()
    {
        var graph = Sample();
        graph.Components[0].Settings["batchSize"] = Json("\"many\"");

        var read = GraphPayloadReader.Read(graph, System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("src");
        read.CurrentMessage.ShouldContain("batchSize");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASettingTheKindDoesNotListIsRefused()
    {
        var graph = Sample();
        graph.Components[0].Settings["joinKind"] = Json("\"Inner\"");

        var read = GraphPayloadReader.Read(graph, System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("joinKind");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AMappingThatStatesBothASourceAndAConstantIsRefused()
    {
        var graph = Sample();
        graph.Components[1].Settings["mappings"] = Json("[{\"input\":\"input\",\"target\":\"Key\",\"source\":\"Id\",\"constant\":{\"type\":\"String\",\"value\":\"x\"}}]");

        var read = GraphPayloadReader.Read(graph, System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("Key");
    }

    private static GraphPayload WithTool(string kind, params (string Setting, string Json)[] settings)
    {
        var graph = Sample();
        var tool = new GraphComponentPayload { Name = "keep", Kind = kind };
        foreach (var (setting, json) in settings)
        {
            tool.Settings[setting] = Json(json);
        }

        graph.Components.Add(tool);
        return graph;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValidateStageIsAcceptedAsAGraphComponentWithItsChecks()
    {
        var read = GraphPayloadReader.Read(
            WithTool("Validate", ("onFail", "\"Reject\""), ("checks", "[{\"name\":\"Adult\",\"expression\":\"Age >= 18\"}]")),
            System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var stage = read.Value!.Components.Single(c => c.Name == "keep").Stages.Single();
        stage.OperationType.ShouldBe("Validate");
        stage.OnFail.ShouldBe("Reject");
        stage.Checks.Single().Name.ShouldBe("Adult");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AToolIsReadAsTheStageOfItsComponentAndWrittenBackAsItsSettings()
    {
        var read = GraphPayloadReader.Read(WithTool("Filter", ("filterExpression", "\"Amount > 100\"")), System.Guid.NewGuid());
        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);

        var stage = read.Value!.Components.Single(c => c.Name == "keep").Stages.Single();
        stage.OperationType.ShouldBe("Filter");
        stage.FilterExpression.ShouldBe("Amount > 100");
        var written = GraphPayloadWriter.Write(read.Value!).Components.Single(c => c.Name == "keep");
        ((System.Text.Json.JsonElement)written.Settings["filterExpression"]!).GetString().ShouldBe("Amount > 100");
    }

    // Notify is not here: since FDW-2041 it is an action component of a graph, like the others of the one list.
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Branch")]
    [InlineData("StartPipeline")]
    [InlineData("Union")]
    public void AStageKindThatWorksOnTheRunItselfIsRefusedAsAGraphComponent(string kind)
    {
        var read = GraphPayloadReader.Read(WithTool(kind), System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("keep");
    }

    private static GraphPayload WithActions()
    {
        var graph = Sample();
        graph.Variables = [new GraphVariablePayload { Name = "Attempt", DataType = "Int32", Value = "0" }];
        var loop = new GraphComponentPayload { Name = "again", Kind = "DoWhile" };
        loop.Settings["condition"] = Json("\"Attempt < 3\"");
        loop.Settings["maximum"] = Json("5");
        var set = new GraphComponentPayload { Name = "advance", Kind = "SetVariable", Parent = "again" };
        set.Settings["variable"] = Json("\"Attempt\"");
        set.Settings["valueType"] = Json("\"Int32\"");
        set.Settings["value"] = Json("\"5\"");
        var start = new GraphComponentPayload { Name = "start", Kind = "ExecutePipeline" };
        start.Settings["pipeline"] = Json("\"Child\"");
        start.Settings["awaitCompletion"] = Json("true");
        start.Settings["failOnChildFailure"] = Json("true");
        start.Settings["parameters"] = Json("[{\"name\":\"Year\",\"value\":\"{{Attempt}}\"}]");
        graph.Components = [.. graph.Components, loop, set, start];
        return graph;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ActionsLoopsAndVariablesAreReadWithTheirParentAndPassTheCheck()
    {
        var read = GraphPayloadReader.Read(WithActions(), System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var graph = read.Value!;
        graph.Variables.Single().ValueText.ShouldBe("0");
        graph.Components.Select(component => $"{component.Name}/{component.ParentComponent}").ShouldBe(
            ["src/", "map/", "split/", "again/", "advance/again", "start/"]);
        graph.Components.Single(component => component.Name == "again").MaxIterations.ShouldBe(5);
        var parameter = graph.Components.Single(component => component.Name == "start").Parameters.Single();
        (parameter.ParameterName, parameter.ValueText).ShouldBe(("Year", "{{Attempt}}"));
        GraphShapeValidator.Validate(graph, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ActionsAreWrittenBackWithTheirParent()
    {
        var read = GraphPayloadReader.Read(WithActions(), System.Guid.NewGuid());
        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);

        var written = GraphPayloadWriter.Write(read.Value!);

        written.Components.Select(component => component.Name).ShouldBe(["src", "map", "split", "again", "advance", "start"]);
        written.Components.Single(component => component.Name == "advance").Parent.ShouldBe("again");
        written.Components.Single(component => component.Name == "again").Settings["maximum"].ShouldBe(5);
        ((List<GraphComponentParameterPayload>)written.Components.Single(component => component.Name == "start").Settings["parameters"]!).Single().Value.ShouldBe("{{Attempt}}");
        written.Variables.Single().DataType.ShouldBe("Int32");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnActionOfAKindNotInTheCatalogueOrWithASettingTheKindDoesNotTakeIsRefusedNamingTheComponent()
    {
        var unknown = WithActions();
        unknown.Components[3].Kind = "Teleport";
        var foreign = WithActions();
        foreign.Components[3].Settings["pipeline"] = Json("\"Child\"");

        var unknownRead = GraphPayloadReader.Read(unknown, System.Guid.NewGuid());
        var foreignRead = GraphPayloadReader.Read(foreign, System.Guid.NewGuid());

        unknownRead.IsFailure.ShouldBeTrue();
        unknownRead.CurrentMessage.ShouldNotBeNull().ShouldContain("again");
        foreignRead.IsFailure.ShouldBeTrue();
        foreignRead.CurrentMessage.ShouldNotBeNull().ShouldContain("pipeline");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnActionSettingOfTheWrongTypeIsRefusedNamingTheComponentAndTheSetting()
    {
        var graph = WithActions();
        graph.Components[3].Settings["maximum"] = Json("\"many\"");

        var read = GraphPayloadReader.Read(graph, System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("again");
        read.CurrentMessage.ShouldContain("maximum");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADraftWithNoNameIsReadAndTheNameIsAProblemButASaveIsRefused()
    {
        var graph = Sample();
        graph.Name = string.Empty;
        var problems = new List<GraphProblem>();

        var draft = GraphPayloadReader.ReadDraft(graph, System.Guid.NewGuid(), problems);
        var save = GraphPayloadReader.Read(graph, System.Guid.NewGuid());

        draft.IsSuccess.ShouldBeTrue(draft.CurrentMessage);
        problems.ShouldHaveSingleItem().Path.ShouldBe(["name"]);
        save.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADraftWithASortFieldRowStillBlankKeepsTheRestOfTheGraphAndNamesTheSortSettingAsTheProblem()
    {
        var graph = WithTool("Sort", ("sort", "[{\"field\":\"\",\"direction\":\"Ascending\"}]"), ("batchSize", "100"));
        var problems = new List<GraphProblem>();

        var draft = GraphPayloadReader.ReadDraft(graph, System.Guid.NewGuid(), problems);
        var save = GraphPayloadReader.Read(graph, System.Guid.NewGuid());

        draft.IsSuccess.ShouldBeTrue(draft.CurrentMessage);
        var sort = draft.Value!.Components.Single(c => c.Name == "keep");
        sort.Stages.ShouldBeEmpty();
        sort.BatchSize.ShouldBe(100);
        var problem = problems.ShouldHaveSingleItem();
        problem.Path.ShouldBe(["components", "keep", "settings", "sort"]);
        save.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADraftWithASettingTheKindDoesNotTakeIsReadAndTheSettingIsAProblem()
    {
        var graph = WithTool("Filter", ("filterExpression", "\"Amount > 1\""), ("loadKind", "\"Insert\""));
        var problems = new List<GraphProblem>();

        var draft = GraphPayloadReader.ReadDraft(graph, System.Guid.NewGuid(), problems);

        draft.IsSuccess.ShouldBeTrue(draft.CurrentMessage);
        problems.ShouldHaveSingleItem().Path.ShouldBe(["components", "keep", "settings", "loadKind"]);
    }

    private static GraphPayload WithDestination(params (string Name, string Json)[] settings)
    {
        var source = new GraphComponentPayload { Name = "src", Kind = "DataSetSource" };
        source.Settings["dataSet"] = Json("\"Orders\"");
        source.Settings["batchSize"] = Json("500");
        var load = new GraphComponentPayload { Name = "load", Kind = "DataSetDestination" };
        load.Settings["dataSet"] = Json("\"OrdersHistory\"");
        load.Settings["loadKind"] = Json("\"BulkUpsert\"");
        load.Settings["truncateFirst"] = Json("false");
        load.Settings["keyColumns"] = Json("[\"Id\"]");
        load.Settings["writeColumns"] = Json("[\"Id\",\"Amount\"]");
        foreach (var (name, json) in settings)
        {
            load.Settings[name] = Json(json);
        }

        return new GraphPayload
        {
            Name = "OrdersHistoryFlow",
            IsEnabled = true,
            LinkCapacity = 4,
            Components = [source, load],
            Links = [new GraphLinkPayload { From = new GraphNodePayload { Component = "src", Node = "output" }, To = new GraphNodePayload { Component = "load", Node = "input" } }],
        };
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADestinationsVersioningSoftDeleteAndValueSourcesAreReadAndWrittenBack()
    {
        var graph = WithDestination(
            ("effectiveFromColumn", "\"ValidFrom\""),
            ("effectiveToColumn", "\"ValidTo\""),
            ("softDeleteMode", "\"AbsentFromSource\""),
            ("deletedFlagColumn", "\"IsGone\""),
            ("valueSources", "[{\"column\":\"Number\",\"source\":{\"kind\":\"NextSequenceValue\",\"sequenceName\":\"OrderNumbers\",\"orderBy\":[\"Id\"]}},{\"column\":\"Origin\",\"source\":{\"kind\":\"Constant\",\"value\":\"orders\",\"valueType\":\"String\"}}]"));

        var read = GraphPayloadReader.Read(graph, System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var load = read.Value!.Components.Single(c => c.Name == "load");
        load.EffectiveFromColumn.ShouldBe("ValidFrom");
        load.SoftDeleteMode.ShouldBe("AbsentFromSource");
        load.ValueSources.Select(v => v.ColumnName).ShouldBe(["Number", "Origin"]);
        load.ValueSources[0].OrderColumns.Single().ColumnName.ShouldBe("Id");
        GraphShapeValidator.Problems(read.Value!, NullLogger.Instance).ShouldBeEmpty();

        var written = GraphPayloadWriter.Write(read.Value!).Components.Single(c => c.Name == "load");
        written.Settings["effectiveToColumn"].ShouldBe("ValidTo");
        var sources = (List<PipelineLoadColumnSource>)written.Settings["valueSources"]!;
        sources.Select(v => v.Column).ShouldBe(["Number", "Origin"]);
        sources[0].Source!.OrderBy.ShouldBe(["Id"]);
        sources[1].Source!.ValueType.ShouldBe("String");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADestinationWhoseVersionHasOneEndIsAProblemOnTheSettingWhenTheGraphIsChecked()
    {
        var read = GraphPayloadReader.Read(WithDestination(("effectiveFromColumn", "\"ValidFrom\"")), System.Guid.NewGuid());
        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);

        var problems = GraphShapeValidator.Problems(read.Value!, NullLogger.Instance);

        problems.ShouldHaveSingleItem().Path.ShouldBe(["components", "load", "settings", "effectiveFromColumn"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValueSourceWithNoSourceIsRefusedNamingTheColumn()
    {
        var read = GraphPayloadReader.Read(WithDestination(("valueSources", "[{\"column\":\"Origin\"}]")), System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldContain("Origin");
    }

    private static GraphPayload WithMaps(string kind, string maps)
    {
        var graph = Sample();
        var map = new GraphComponentPayload { Name = "tidy", Kind = kind };
        if (kind == "Filter")
        {
            map.Settings["filterExpression"] = Json("\"Qty >= 0\"");
        }

        map.Settings[kind == "Lookup" ? "bring" : "mappings"] = Json(maps);
        graph.Components.Add(map);
        return graph;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AMapsChainIsStoredWithTheIdsTheLookupFound()
    {
        var id = System.Guid.Parse("55555555-5555-5555-5555-555555555555");
        var graph = WithMaps("Filter", "[{\"input\":\"input\",\"target\":\"Place\",\"source\":\"Place\",\"transformations\":[{\"transformation\":\"Trim\",\"ordinal\":1}]}]");
        var ids = new Dictionary<string, System.Guid>(System.StringComparer.OrdinalIgnoreCase) { ["Trim"] = id };

        var withIds = GraphPayloadReader.Read(graph, System.Guid.NewGuid(), ids);
        var without = GraphPayloadReader.Read(graph, System.Guid.NewGuid());

        withIds.IsSuccess.ShouldBeTrue(withIds.CurrentMessage);
        withIds.Value!.Components.Single(c => c.Name == "tidy").Fields.Single().TransformationChain.Single().TransformationId.ShouldBe(id);
        without.IsFailure.ShouldBeTrue();
        without.CurrentMessage.ShouldContain("Trim");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AMapsTypeDefaultRequiredFlagAndChainAreStoredAndWrittenBack()
    {
        var id = System.Guid.Parse("55555555-5555-5555-5555-555555555555");
        var graph = WithMaps("Filter", "[{\"input\":\"input\",\"target\":\"Qty\",\"source\":\"Quantity\",\"targetType\":\"Int32\",\"defaultValue\":\"0\",\"isRequired\":true,\"transformations\":[{\"transformation\":\"Trim\",\"ordinal\":1}]}]");
        var ids = new Dictionary<string, System.Guid>(System.StringComparer.OrdinalIgnoreCase) { ["Trim"] = id };

        var read = GraphPayloadReader.Read(graph, System.Guid.NewGuid(), ids);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var field = read.Value!.Components.Single(c => c.Name == "tidy").Fields.Single();
        field.TargetType.ShouldBe("Int32");
        field.DefaultText.ShouldBe("0");
        field.IsRequired.ShouldBeTrue();
        var written = ((System.Collections.IEnumerable)GraphPayloadWriter.Write(read.Value).Components.Single(c => c.Name == "tidy").Settings["mappings"]!).Cast<GraphFieldMapPayload>().Single();
        written.TargetType.ShouldBe("Int32");
        written.DefaultValue.ShouldBe("0");
        written.IsRequired.ShouldBe(true);
        written.Transformations!.Single().TransformationId.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AMapsTypeThatIsNotADataTypeIsRefusedNamingIt()
    {
        var read = GraphPayloadReader.Read(WithMaps("Filter", "[{\"input\":\"input\",\"target\":\"Qty\",\"source\":\"Quantity\",\"targetType\":\"money\"}]"), System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldContain("money");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADataTypeIsNamedExactlyAndTheFieldMapSpellingsAreRefused()
    {
        var spelling = GraphPayloadReader.Read(WithMaps("Filter", "[{\"input\":\"input\",\"target\":\"Qty\",\"source\":\"Quantity\",\"targetType\":\"int\"}]"), System.Guid.NewGuid());
        var exact = GraphPayloadReader.Read(WithMaps("Filter", "[{\"input\":\"input\",\"target\":\"Qty\",\"source\":\"Quantity\",\"targetType\":\"Int64\"}]"), System.Guid.NewGuid());

        spelling.IsFailure.ShouldBeTrue();
        spelling.CurrentMessage.ShouldContain("int");
        exact.IsSuccess.ShouldBeTrue(exact.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASettingThatDoesNotTakeConversionsRefusesAMappingThatStatesOne()
    {
        var read = GraphPayloadReader.Read(WithMaps("Lookup", "[{\"target\":\"Qty\",\"source\":\"Quantity\",\"defaultValue\":\"0\"}]"), System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldContain("Qty");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async System.Threading.Tasks.Task AGraphWhoseMappingsStateNoChainLooksUpNothing()
    {
        var found = await GraphChainLookup.Resolve(new StageChainLookup(new Moq.Mock<Fdw.Data.Transformations.Configuration.ITransformationConfigurationProvider>().Object, NullLogger.Instance), Sample(), TestContext.Current.CancellationToken);

        found.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASettingThatTakesItsValueFromAParameterIsStoredAsABindingAndWrittenBackAsOne()
    {
        var source = new GraphComponentPayload { Name = "src", Kind = "DataSetSource" };
        source.Settings["dataSet"] = Json("{\"parameter\":\"Which\"}");
        source.Settings["batchSize"] = Json("{\"parameter\":\"Size\"}");
        var loop = new GraphComponentPayload { Name = "loop", Kind = "ForLoop" };
        loop.Settings["variable"] = Json("\"i\"");
        loop.Settings["start"] = Json("1");
        loop.Settings["end"] = Json("{\"parameter\":\"Last\"}");
        var payload = new GraphPayload
        {
            Name = "Bound", IsEnabled = true, LinkCapacity = 4,
            Components = [source, loop],
        };

        var read = GraphPayloadReader.Read(payload, System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var stored = read.Value!.Components.Single(component => component.Name == "src");
        stored.DataSetName.ShouldBeNull();
        stored.BatchSize.ShouldBeNull();
        stored.SettingBindings.Select(binding => $"{binding.SettingName}={binding.ParameterName}").OrderBy(text => text).ShouldBe(["batchSize=Size", "dataSet=Which"]);
        read.Value.Components.Single(component => component.Name == "loop").SettingBindings.Single().ParameterName.ShouldBe("Last");

        var written = GraphPayloadWriter.Write(read.Value);
        var writtenSource = written.Components.Single(component => component.Name == "src");
        ((Dictionary<string, object?>)writtenSource.Settings["dataSet"]!)["parameter"].ShouldBe("Which");
        ((Dictionary<string, object?>)written.Components.Single(component => component.Name == "loop").Settings["end"]!)["parameter"].ShouldBe("Last");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ASettingThatIsNeitherAValueNorAParameterObjectIsRefusedNamingBothWays()
    {
        var source = new GraphComponentPayload { Name = "src", Kind = "DataSetSource" };
        source.Settings["dataSet"] = Json("\"Orders\"");
        source.Settings["batchSize"] = Json("{\"name\":\"Size\"}");
        var payload = new GraphPayload
        {
            Name = "Bad", IsEnabled = true, LinkCapacity = 4,
            Components = [source],
        };

        var read = GraphPayloadReader.Read(payload, System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("batchSize");
        read.CurrentMessage.ShouldContain("parameter");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("[{\"target\":\"Qty\",\"source\":\"Quantity\"}]", "without naming its input")]
    [InlineData("[{\"input\":\"output\",\"target\":\"Qty\",\"source\":\"Quantity\"}]", "does not have")]
    [InlineData("[{\"input\":\"input\",\"target\":\"Qty\",\"source\":\"A\"},{\"input\":\"input\",\"target\":\"qty\",\"source\":\"B\"}]", "twice")]
    [InlineData("[{\"input\":\"input\",\"target\":\"Qty\",\"source\":\"A\",\"targetType\":\"Int32\",\"defaultValue\":\"many\"}]", "is not a value of Int32")]
    public void AnInputMappingIsRefusedWhenItNamesNoInputAnInputTheKindLacksATargetTwiceOrADefaultThatIsNotOfTheType(string mappings, string reason)
    {
        var read = GraphPayloadReader.Read(WithMaps("Filter", mappings), System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain(reason);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheMapComponentKindIsGone()
    {
        var graph = Sample();
        graph.Components[1].Kind = "Map";

        var read = GraphPayloadReader.Read(graph, System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("not a component kind");
    }
}
