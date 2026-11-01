using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Builds the engine's pipeline from a stored graph (FDW-1396, FDW-2041): the top-level components as one flow graph, and for each loop the
/// components that name it as their parent as a flow graph of its own, run again each round.
/// </summary>
/// <remarks>
/// <para>
/// A component that moves rows is built into a flow component; an action or a loop is built into an engine step and run as a component with no
/// nodes, so it waits for its stage like any other. A component whose settings name a value an earlier stage calculates is built when its
/// stage starts, with the variables as that stage found them (FDW-1958).
/// </para>
/// <para>
/// It refuses what it cannot run and never repairs it: a component of a kind nothing builds, a component whose settings its kind refuses, a name used
/// twice. Whether the graph as a whole is sound is the shape check's to say, before a run reaches this.
/// </para>
/// </remarks>
public static class GraphFlowBuilder
{
    /// <summary>Builds the pipeline.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>The pipeline, or the failure that names the first component that cannot be built.</returns>
    public static IGenericResult<FlowPipeline> Build(GraphPipelineConfiguration graph, FlowBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(context);
        if (graph.Components.Count == 0)
        {
            return GenericResult<FlowPipeline>.Failure(GraphLog.GraphEmpty(context.Logger, graph.Name));
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in graph.Components)
        {
            if (!seen.Add(component.Name))
            {
                return GenericResult<FlowPipeline>.Failure(GraphLog.ComponentNameRepeated(context.Logger, graph.Name, component.Name));
            }
        }

        var top = BuildLevel(graph, null, graph.Name, context);
        return top.IsFailure || top.Value is null
            ? top.ToNewResult<FlowPipeline>()
            : GenericResult<FlowPipeline>.Success(new FlowPipeline(graph.Name, [top.Value]));
    }

    // One level: the components that share a parent, wired by the links between them, as a flow graph the engine runs until every component has finished.
    private static IGenericResult<IFlowStep> BuildLevel(GraphPipelineConfiguration graph, string? parent, string name, FlowBuildContext context)
    {
        var members = graph.Components.Where(component => string.Equals(component.ParentComponent, parent, StringComparison.OrdinalIgnoreCase)).ToList();
        if (members.Count == 0)
        {
            return GenericResult<IFlowStep>.Failure(parent is null
                ? GraphLog.GraphEmpty(context.Logger, graph.Name)
                : GraphLog.ComponentInvalid(context.Logger, parent, graph.Components.First(c => string.Equals(c.Name, parent, StringComparison.OrdinalIgnoreCase)).ComponentType, "has no components in its body, so there is nothing to run"));
        }

        var inLevel = new HashSet<string>(members.Select(member => member.Name), StringComparer.OrdinalIgnoreCase);
        var links = graph.Links.Where(link => inLevel.Contains(link.FromComponent) && inLevel.Contains(link.ToComponent)).ToList();
        var flowContext = context.ForLinks(links);
        var built = new List<IFlowComponent>();
        foreach (var stored in members)
        {
            var one = BuildComponent(graph, stored, context, flowContext, links);
            if (one.IsFailure || one.Value is null)
            {
                return one.ToNewResult<IFlowStep>();
            }

            built.Add(one.Value);
        }

        var flow = new FlowGraph(
            built,
            [.. links.Select(link => new FlowLink(link.FromComponent, link.FromOutput, link.ToComponent, link.ToInput))],
            [.. graph.DroppedOutputs.Where(output => inLevel.Contains(output.ComponentName)).Select(output => new FlowDroppedOutput(output.ComponentName, output.OutputName))],
            ComponentStages.Of(members));
        return GenericResult<IFlowStep>.Success(new FlowStageStep(name, flow, context.Logger));
    }

    private static IGenericResult<IFlowComponent> BuildComponent(
        GraphPipelineConfiguration graph,
        PipelineComponentConfiguration stored,
        FlowBuildContext context,
        FlowBuildContext flowContext,
        IReadOnlyList<PipelineLinkConfiguration> links)
    {
        var kind = ComponentTypes.ByName(stored.ComponentType);
        if (ReferenceEquals(kind, ComponentTypes.NotFound))
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.ComponentKindNotRunnable(context.Logger, stored.Name, stored.ComponentType));
        }

        return kind.IsAction ? BuildAction(graph, stored, kind, context) : BuildRows(stored, context, flowContext, links);
    }

    // An action or a loop: its settings against its kind, the body of a loop built first, the settings a run supplies applied, then the engine step.
    private static IGenericResult<IFlowComponent> BuildAction(
        GraphPipelineConfiguration graph, PipelineComponentConfiguration stored, IComponentType kind, FlowBuildContext context)
    {
        var builder = FlowStepBuilders.ByName(stored.ComponentType);
        if (ReferenceEquals(builder, FlowStepBuilders.NotFound))
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.StepKindNotRunnable(context.Logger, stored.Name, stored.ComponentType));
        }

        var settings = ComponentSettings.Problems(stored);
        if (settings.Count > 0)
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.StepInvalid(context.Logger, stored.Name, stored.ComponentType, settings[0].Reason));
        }

        IReadOnlyList<IFlowStep> body = [];
        if (kind.HoldsComponents)
        {
            var level = BuildLevel(graph, stored.Name, $"{stored.Name} body", context);
            if (level.IsFailure || level.Value is null)
            {
                return level.ToNewResult<IFlowComponent>();
            }

            body = [level.Value];
        }

        // Why bound here: one place gives every action the values its bound settings take from the run's parameters (FDW-1722).
        var bound = BoundSettings.Apply(stored, context);
        if (bound.IsFailure || bound.Value is null)
        {
            return bound.ToNewResult<IFlowComponent>();
        }

        // Why the kind check again: a variable name chosen by the run could not be judged when the graph was saved; it is judged now, with its real name.
        if (stored.SettingBindings.Count > 0 && GraphComponentValidator.ComponentProblems(graph, bound.Value, context.Logger) is { Count: > 0 } problems)
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.StepInvalid(context.Logger, stored.Name, stored.ComponentType, problems[0].Reason));
        }

        var step = builder.Build(bound.Value, body, context);
        return step.IsFailure || step.Value is null
            ? step.ToNewResult<IFlowComponent>()
            : GenericResult<IFlowComponent>.Success(new FlowActionComponent(step.Value));
    }

    // A component that moves rows. Why built when its stage starts, for one that names a value a component of a lower stage calculates: a source that names its
    // physical path or name as {{Name}} reads it from the variables as the stages before it left them (FDW-1958); so does a script that names $(Name)
    // (FDW-1970), and a mapping whose constant names a value (FDW-2119). Every other component is built now, so what is wrong with it is found before any row moves.
    private static IGenericResult<IFlowComponent> BuildRows(
        PipelineComponentConfiguration stored, FlowBuildContext context, FlowBuildContext flowContext, IReadOnlyList<PipelineLinkConfiguration> links)
    {
        if (ReferenceEquals(FlowComponentBuilders.ByName(stored.ComponentType), FlowComponentBuilders.NotFound))
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.ComponentKindNotRunnable(context.Logger, stored.Name, stored.ComponentType));
        }

        var checkedSettings = ComponentSettings.Check(stored, context.Logger);
        if (checkedSettings.IsFailure)
        {
            return checkedSettings.ToNewResult<IFlowComponent>();
        }

        if (!ParameterSettings.LateOf(stored).Any(setting => ParameterSettings.HoldsParameter(setting.Text)) && !ScriptTokens.Holds(stored.ScriptText)
            && ParameterSettings.MappingConstants(stored).Count == 0 && !NamesCalculatedValue(stored, context) && !ReadsFilesFromVariable(stored, context))
        {
            return Assemble(stored, context, flowContext, null);
        }

        var (inputs, outputs) = ComponentShapes.Of(stored, links);
        return GenericResult<IFlowComponent>.Success(new FlowLazyComponent(
            stored.Name, inputs, outputs, ComponentShapes.ErrorOutputsOf(stored, links), variables => Assemble(stored, context, flowContext, variables.Snapshot())));
    }

    // Why a tool's own text is read here (FDW-1984): its {{Name}} references are resolved from the run's parameters, so a name that is not one of them can only be a
    // variable an earlier stage calculates, and the tool has to be built when its stage starts, with the variables as that stage found them.
    internal static bool NamesCalculatedValue(PipelineComponentConfiguration stored, FlowBuildContext context) =>
        stored.Stages.Any(stage => PipelineRunParameterReferences.Names(stage.FilterExpression)
            .Concat(stage.Checks.SelectMany(check => PipelineRunParameterReferences.Names(check.Expression)))
            .Any(name => !context.Parameters.ContainsKey(name)));

    // Why a list of files that is not a run parameter is read when the stage starts (FDW-3194): it can only be a variable, such as the batch a round of a for each
    // took, and a variable has its value only then.
    internal static bool ReadsFilesFromVariable(PipelineComponentConfiguration stored, FlowBuildContext context) =>
        stored.FilesList is { } list && !context.Parameters.ContainsKey(list);

    private static IGenericResult<IFlowComponent> Assemble(
        PipelineComponentConfiguration stored, FlowBuildContext context, FlowBuildContext flowContext, IReadOnlyDictionary<string, object?>? runValues)
    {
        var builder = FlowComponentBuilders.ByName(stored.ComponentType);

        // Why rendered here: one place gives every source and destination its run parameters, so no builder reads them itself.
        var rendered = ComponentParameters.Render(stored, context);
        if (rendered.IsFailure || rendered.Value is null)
        {
            return rendered.ToNewResult<IFlowComponent>();
        }

        var current = rendered.Value;
        if (runValues is not null)
        {
            var late = ComponentParameters.RenderLate(current, runValues, context.Logger);
            if (late.IsFailure || late.Value is null)
            {
                return late.ToNewResult<IFlowComponent>();
            }

            current = late.Value;
        }

        return builder.Build(current, runValues is null ? flowContext : flowContext.WithValues(runValues));
    }
}
