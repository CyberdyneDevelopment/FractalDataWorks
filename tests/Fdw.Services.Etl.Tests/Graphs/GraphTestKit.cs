using System;
using System.Collections.Generic;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.EtlGateway;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>Builds stored graphs and the context a builder runs in, for the graph tests.</summary>
internal static class GraphTestKit
{
    public static PipelineComponentConfiguration DataSetSource(string name, string dataSet = "Orders", int batchSize = 2) =>
        new() { Name = name, ComponentType = "DataSetSource", DataSetName = dataSet, BatchSize = batchSize };

    /// <summary>
    /// A Filter that keeps every row whose first mapped field is not negative (every row of the sample data sets), with the maps on its input (FDW-2013).
    /// </summary>
    public static PipelineComponentConfiguration Mapped(string name, params (string Target, string Source)[] maps)
    {
        var component = FilterStage(name, maps.Length > 0 ? $"{maps[0].Target} >= 0" : "Id >= 0");
        for (var i = 0; i < maps.Length; i++)
        {
            component.Fields.Add(new PipelineComponentFieldConfiguration
            {
                SettingName = "mappings", InputName = "input", Ordinal = i, TargetField = maps[i].Target, SourceField = maps[i].Source,
            });
        }

        return component;
    }

    public static PipelineComponentConfiguration FilterStage(string name, string expression)
    {
        var component = new PipelineComponentConfiguration { Name = name, ComponentType = "Filter" };
        component.Stages.Add(new PipelineTransformConfiguration { Name = name, OperationType = "Filter", FilterExpression = expression, IsEnabled = true });
        return component;
    }

    public static PipelineLinkConfiguration Link(string from, string to, string output = "output", string input = "input") =>
        new() { FromComponent = from, FromOutput = output, ToComponent = to, ToInput = input };

    /// <summary>A graph of the components, links and dropped outputs: one list (FDW-2041), every component top level and with no stage.</summary>
    public static GraphPipelineConfiguration Graph(
        IEnumerable<PipelineComponentConfiguration> components,
        IEnumerable<PipelineLinkConfiguration> links,
        params PipelineDroppedOutputConfiguration[] dropped) =>
        Steps(DataFlow("flow", components, links, dropped));

    /// <summary>
    /// A graph of the parts, as they are listed: each a <see cref="GraphPart"/> or a single component. A part that states an ordinal runs in the stage of
    /// that number; ordinal zero states no stage.
    /// </summary>
    public static GraphPipelineConfiguration Steps(params object[] parts)
    {
        var graph = new GraphPipelineConfiguration { Name = "Sample", LinkCapacity = 2, MaxParallelRuns = 1, OverflowBehavior = "Queue", IsEnabled = true };
        foreach (var listed in parts)
        {
            var part = listed switch
            {
                GraphPart whole => whole,
                PipelineComponentConfiguration single => new GraphPart([single], [], []),
                _ => throw new ArgumentException($"A graph is made of parts and components, not {listed.GetType().Name}.", nameof(parts)),
            };
            foreach (var component in part.Components)
            {
                graph.Components.Add(component);
            }

            foreach (var link in part.Links)
            {
                graph.Links.Add(link);
            }

            foreach (var output in part.Dropped)
            {
                graph.DroppedOutputs.Add(output);
            }
        }

        return graph;
    }

    /// <summary>
    /// Components with their links and dropped outputs, placed together: in the stage of the ordinal (none for zero) and in the body of the parent loop.
    /// A component names its own name, which is unique in the graph.
    /// </summary>
    public static GraphPart DataFlow(
        string name,
        IEnumerable<PipelineComponentConfiguration> components,
        IEnumerable<PipelineLinkConfiguration> links,
        PipelineDroppedOutputConfiguration[] dropped,
        int ordinal = 0,
        string? parent = null)
    {
        _ = name;
        var placed = new List<PipelineComponentConfiguration>();
        foreach (var component in components)
        {
            component.Stage ??= ordinal > 0 ? ordinal : null;
            component.ParentComponent ??= parent;
            placed.Add(component);
        }

        return new GraphPart(placed, [.. links], [.. dropped]);
    }

    /// <summary>A source and a map of its rows, so the rows it moves are counted; the components are named after the part.</summary>
    public static GraphPart SourceFlow(string name, int ordinal = 0, string? parent = null) =>
        DataFlow(name, [DataSetSource($"{name}-src"), Mapped($"{name}-map", ("Key", "Id"))], [Link($"{name}-src", $"{name}-map")], [Dropped($"{name}-map")], ordinal, parent);

    public static PipelineComponentConfiguration ForLoop(string name, string variable, int start, int end, int by = 1, int ordinal = 0, string? parent = null) =>
        Placed(
            new PipelineComponentConfiguration
            {
                Name = name, ComponentType = "ForLoop", VariableName = variable, StartValue = start, EndValue = end, StepValue = by,
            },
            ordinal,
            parent);

    public static PipelineComponentConfiguration SetVariable(string name, string variable, string type, string? value, int ordinal = 0, string? parent = null) =>
        Placed(
            new PipelineComponentConfiguration
            {
                Name = name, ComponentType = "SetVariable", VariableName = variable, ConstantType = type, ConstantText = value,
            },
            ordinal,
            parent);

    public static PipelineComponentConfiguration Notify(string name, string rule, string runPoint, string message, int ordinal = 0, string? parent = null) =>
        Placed(
            new PipelineComponentConfiguration
            {
                Name = name, ComponentType = "Notify", NotificationRuleName = rule, RunPoint = runPoint, NotificationMessage = message,
            },
            ordinal,
            parent);

    public static PipelineComponentConfiguration DoWhile(string name, string condition, int maximum, int ordinal = 0, string? parent = null) =>
        Placed(
            new PipelineComponentConfiguration
            {
                Name = name, ComponentType = "DoWhile", ConditionExpression = condition, MaxIterations = maximum,
            },
            ordinal,
            parent);

    /// <summary>Places a component in the stage of the ordinal (none for zero) and in the body of the parent loop.</summary>
    public static PipelineComponentConfiguration Placed(PipelineComponentConfiguration component, int ordinal = 0, string? parent = null)
    {
        component.Stage = ordinal > 0 ? ordinal : null;
        component.ParentComponent = parent;
        return component;
    }

    /// <summary>What a built pipeline runs against: the variables it declared and an executor that counts rows.</summary>
    public static FlowStepContext StepContext(FlowVariables variables, FlowRunTally tally) =>
        new(variables, tally, new FlowRunOptions { LinkCapacity = 2 }, null, null);

    public static PipelineDroppedOutputConfiguration Dropped(string component, string output = "output") =>
        new() { ComponentName = component, OutputName = output };

    public static FlowBuildContext Context(GraphPipelineConfiguration graph, PagedSourceGateway gateway, IRuleNotifier? notifier = null, GraphTestRun? test = null) =>
        Context(graph, gateway.Provider.ByName("Main"), null, notifier, test);

    public static FlowBuildContext Context(GraphPipelineConfiguration graph, ScriptingGateway gateway, GraphTestRun? test = null) =>
        Context(graph, new Mock<IDataGateway>().Object, gateway.Gateway, null, test);

    /// <summary>What a run started with these parameter values builds with.</summary>
    public static FlowBuildContext ContextWith(GraphPipelineConfiguration graph, IReadOnlyDictionary<string, object?> parameters, GraphTestRun? test = null) =>
        Context(graph, new Mock<IDataGateway>().Object, null, null, test, parameters);

    /// <summary>What a run builds with when its data gateway is the one given.</summary>
    public static FlowBuildContext ContextOn(GraphPipelineConfiguration graph, IDataGateway dataGateway) =>
        Context(graph, dataGateway, null, null, null, null);

    /// <summary>What a run builds with when its data gateway is the one given and it was started with the parameters given.</summary>
    public static FlowBuildContext ContextOn(GraphPipelineConfiguration graph, IDataGateway dataGateway, IReadOnlyDictionary<string, object?> parameters) =>
        Context(graph, dataGateway, null, null, null, parameters);

    private static FlowBuildContext Context(
        GraphPipelineConfiguration graph, IDataGateway dataGateway, IEtlGateway? etlGateway, IRuleNotifier? notifier = null, GraphTestRun? test = null,
        IReadOnlyDictionary<string, object?>? parameters = null) =>
        new(
            graph.Name,
            graph.Id,
            Guid.NewGuid(),
            dataGateway,
            new PipelineLoadExecutor(new LoadTargetResolver(null, null), null, null, NullLogger.Instance),
            null,
            DateTimeOffset.UtcNow,
            parameters ?? new Dictionary<string, object?>(),
            [],
            NullLogger.Instance,
            etlGateway,
            notifier,
            null,
            null,
            test);
}
