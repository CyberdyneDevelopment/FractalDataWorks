using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Planning;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging;

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
    private const string StepBuilder = "step builder";
    private const string ComponentBuilder = "component builder";

    /// <summary>
    /// Lists every way the component types and the collected builders fail to match (FDW-6133, FDW-6240): a ComponentTypes option says what a component is
    /// and its builder says how its step is made, joined by the name. Each component type has exactly one builder, a step builder for an action and a
    /// component builder otherwise, and each builder names a component type.
    /// </summary>
    /// <param name="types">The component types.</param>
    /// <param name="steps">The builders of the action and loop types.</param>
    /// <param name="components">The builders of the types that move rows.</param>
    /// <param name="logger">The logger each mismatch is written to.</param>
    /// <returns>One ComponentTypeHasNoBuilder, ComponentTypeHasTwoBuilders or BuilderNamesNoComponentType for each mismatch; empty when they match.</returns>
    public static IReadOnlyList<IGenericMessage> TypeBuilderMismatches(
        IEnumerable<IComponentType> types, IEnumerable<IFlowStepBuilder> steps, IEnumerable<IFlowComponentBuilder> components, ILogger logger)
    {
        List<IComponentType> typeList = [.. types];
        List<(string Name, string Collection)> builders =
            [.. steps.Select(step => (step.Name, StepBuilder)), .. components.Select(component => (component.Name, ComponentBuilder))];
        var typeNames = new HashSet<string>(typeList.Select(type => type.Name), StringComparer.OrdinalIgnoreCase);
        var messages = new List<IGenericMessage>();
        foreach (var type in typeList)
        {
            var named = builders.Where(builder => string.Equals(builder.Name, type.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            var wanted = type.IsAction ? StepBuilder : ComponentBuilder;
            if (named.Count > 1)
            {
                messages.Add(GraphLog.ComponentTypeHasTwoBuilders(logger, type.Name, named.Count));
            }
            else if (!named.Exists(builder => string.Equals(builder.Collection, wanted, StringComparison.Ordinal)))
            {
                messages.Add(GraphLog.ComponentTypeHasNoBuilder(logger, type.Name, wanted));
            }
        }

        messages.AddRange(builders.Where(builder => !typeNames.Contains(builder.Name))
            .Select(builder => GraphLog.BuilderNamesNoComponentType(logger, builder.Name, builder.Collection)));
        return messages;
    }

    /// <summary>Builds the pipeline.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>The pipeline, or the failure that names the first component that cannot be built.</returns>
    public static IGenericResult<FlowPipeline> Build(IGraphPipelineConfiguration graph, FlowBuildContext context) =>
        Build(graph, context, null, GraphPushdownPlan.None, null);

    /// <summary>
    /// Builds the pipeline of a graph that is read and not loaded (FDW-5291): the same pipeline, with one component added at the top level that takes the
    /// rows of the terminal's output.
    /// </summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="context">What the run builds with.</param>
    /// <param name="terminal">Where the rows end.</param>
    /// <returns>The pipeline, or the failure that names the first component that cannot be built.</returns>
    public static IGenericResult<FlowPipeline> BuildRead(IGraphPipelineConfiguration graph, FlowBuildContext context, GraphReadTerminal terminal) =>
        Build(graph, context, terminal, GraphPushdownPlan.None, null);

    /// <summary>
    /// Builds the pipeline of a graph that is read and not loaded, with the parts the planner pushed into their stores (FDW-6969): each pushed sub-graph is
    /// one component that runs its query, under its top component's name, and the components below its top are left out with the lines between them.
    /// </summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="context">What the run builds with.</param>
    /// <param name="terminal">Where the rows end.</param>
    /// <param name="plan">The planner's answer for the graph.</param>
    /// <returns>The pipeline, or the failure that names the first component that cannot be built.</returns>
    public static IGenericResult<FlowPipeline> BuildRead(
        IGraphPipelineConfiguration graph, FlowBuildContext context, GraphReadTerminal terminal, GraphPushdownPlan plan) =>
        Build(graph, context, terminal, plan, null);

    /// <summary>
    /// Builds the pipeline of a graph that is read and not loaded, with its pushed parts, and with every read the plan names as feeding a join, lookup or
    /// union across data stores let run by the federation strategy's read gate (FDW-6970).
    /// </summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="context">What the run builds with.</param>
    /// <param name="terminal">Where the rows end.</param>
    /// <param name="plan">The planner's answer for the graph.</param>
    /// <param name="gate">The gate the federated reads wait at.</param>
    /// <returns>The pipeline, or the failure that names the first component that cannot be built.</returns>
    public static IGenericResult<FlowPipeline> BuildRead(
        IGraphPipelineConfiguration graph, FlowBuildContext context, GraphReadTerminal terminal, GraphPushdownPlan plan, FlowReadGate gate) =>
        Build(graph, context, terminal, plan, gate);

    private static IGenericResult<FlowPipeline> Build(
        IGraphPipelineConfiguration graph, FlowBuildContext context, GraphReadTerminal? terminal, GraphPushdownPlan plan, FlowReadGate? gate)
    {
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

        if (terminal is not null && !seen.Add(GraphEnd.ReaderName))
        {
            return GenericResult<FlowPipeline>.Failure(GraphLog.ComponentNameRepeated(context.Logger, graph.Name, GraphEnd.ReaderName));
        }

        var unmet = UnmetNeeds(graph, context);
        if (unmet.Count > 0)
        {
            return GenericResult<FlowPipeline>.Failure(unmet);
        }

        var top = BuildLevel(graph, null, graph.Name, context, terminal, plan, gate);
        return top.IsFailure || top.Value is null
            ? top.ToNewResult<FlowPipeline>()
            : GenericResult<FlowPipeline>.Success(new FlowPipeline(graph.Name, [top.Value]));
    }

    // Why every component is judged here and not only as it is built (FDW-6132): a component built when its stage starts would otherwise find a missing
    // contribution halfway through the run; this way a host that lacks one refuses the graph before any row moves, naming every gap.
    private static List<IGenericMessage> UnmetNeeds(IGraphPipelineConfiguration graph, FlowBuildContext context) =>
        [.. graph.Components
            .Select(component => context.NeedsMet(component, FlowBuildNeeds.Of(component)))
            .Where(judged => judged.IsFailure)
            .SelectMany(judged => judged.Messages)];

    // One level: the components that share a parent, wired by the links between them, as a flow graph the engine runs until every component has finished.
    private static IGenericResult<IFlowStep> BuildLevel(
        IGraphPipelineConfiguration graph, string? parent, string name, FlowBuildContext context, GraphReadTerminal? terminal = null, GraphPushdownPlan? plan = null, FlowReadGate? gate = null,
        IReadOnlySet<string>? only = null)
    {
        // Why a pushed sub-graph's lower components are left out here (FDW-6969): its query reads what they would have, and its top stands in for them all.
        var members = graph.Components
            .Where(component => string.Equals(component.ParentComponent, parent, StringComparison.OrdinalIgnoreCase) && plan?.Removes(component.Name) != true
                && (only is null || only.Contains(component.Name)))
            .ToList();
        if (members.Count == 0)
        {
            return GenericResult<IFlowStep>.Failure(parent is null
                ? GraphLog.GraphEmpty(context.Logger, graph.Name)
                : GraphLog.ComponentInvalid(context.Logger, parent, graph.Components.First(c => string.Equals(c.Name, parent, StringComparison.OrdinalIgnoreCase)).ComponentType, "has no components in its body, so there is nothing to run"));
        }

        var inLevel = new HashSet<string>(members.Select(member => member.Name), StringComparer.OrdinalIgnoreCase);
        var levelLinks = graph.Links.Where(link => inLevel.Contains(link.FromComponent) && inLevel.Contains(link.ToComponent)).ToList();
        var links = CompletionLines.Rows(levelLinks, graph.Components);
        var flowContext = context.ForLinks([.. links]);
        var built = new List<IFlowComponent>();
        foreach (var stored in members)
        {
            if (plan?.ToppedBy(stored.Name) is { } pushed)
            {
                built.Add(Gated(pushed.Build(context.Gateway, context.Logger), plan, gate));
                continue;
            }

            var one = BuildComponent(graph, stored, context, flowContext, links, levelLinks);
            if (one.IsFailure || one.Value is null)
            {
                return one.ToNewResult<IFlowStep>();
            }

            built.Add(Gated(one.Value, plan, gate));
        }

        var flow = new FlowGraph(
            WithReadEnd(built, terminal),
            WithReadLinks(links, terminal),
            [.. graph.DroppedOutputs.Where(output => inLevel.Contains(output.ComponentName)).Select(output => new FlowDroppedOutput(output.ComponentName, output.OutputName))],
            ComponentStages.Of(members),
            [.. CompletionLines.Completions(levelLinks, graph.Components).Select(CompletionLines.ToCompletion)]);
        return GenericResult<IFlowStep>.Success(new FlowStageStep(name, flow, context.Logger));
    }

    // A read the plan names as feeding a join, lookup or union across data stores waits at the federation strategy's gate (FDW-6970); every other
    // component runs as it was built.
    private static IFlowComponent Gated(IFlowComponent component, GraphPushdownPlan? plan, FlowReadGate? gate) =>
        gate is not null && plan?.FederatedRead(component.Name) is { } read ? new FlowGatedRead(component, gate, read.BuildSide) : component;

    // Why the read's component is added here and nowhere in the stored graph: a graph data set holds no destination, and a read still needs one end for its rows.
    private static List<IFlowComponent> WithReadEnd(List<IFlowComponent> built, GraphReadTerminal? terminal) =>
        terminal is null ? built : [.. built, new FlowDestination(GraphEnd.ReaderName, terminal.Write)];

    private static List<FlowLink> WithReadLinks(IReadOnlyList<PipelineLinkConfiguration> links, GraphReadTerminal? terminal)
    {
        List<FlowLink> stored = [.. links.Select(link => new FlowLink(link.FromComponent, link.FromOutput, link.ToComponent, link.ToInput))];
        return terminal is null ? stored : [.. stored, terminal.End.ToReader()];
    }

    private static IGenericResult<IFlowComponent> BuildComponent(
        IGraphPipelineConfiguration graph,
        PipelineComponentConfiguration stored,
        FlowBuildContext context,
        FlowBuildContext flowContext,
        IReadOnlyList<PipelineLinkConfiguration> links,
        IReadOnlyList<PipelineLinkConfiguration> levelLinks)
    {
        var kind = ComponentTypes.ByName(stored.ComponentType);
        if (ReferenceEquals(kind, ComponentTypes.NotFound))
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.ComponentKindNotRunnable(context.Logger, stored.Name, stored.ComponentType));
        }

        return kind.IsAction ? BuildAction(graph, stored, kind, context, levelLinks) : BuildRows(graph, stored, context, flowContext, links);
    }

    // An action or a loop: its settings against its kind, the body of a loop built first, the settings a run supplies applied, then the engine step.
    private static IGenericResult<IFlowComponent> BuildAction(
        IGraphPipelineConfiguration graph, PipelineComponentConfiguration stored, IComponentType kind, FlowBuildContext context, IReadOnlyList<PipelineLinkConfiguration> levelLinks)
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

        var body = BuildBody(graph, stored, kind, context);
        if (body.IsFailure || body.Value is null)
        {
            return body.ToNewResult<IFlowComponent>();
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

        var step = builder.Build(bound.Value, body.Value, context);
        if (step.IsFailure || step.Value is null)
        {
            return step.ToNewResult<IFlowComponent>();
        }

        // Why the lines are given to the component and not read from the step (FDW-6134): the lines are the graph's and a step knows only its settings; a gate reads them as values,
        // and any other component runs only when one of the outputs that lead to it fired.
        var signals = CompletionLines.SignalsInto(levelLinks, graph.Components, stored.Name);
        if (step.Value is IFlowSignalReader reader)
        {
            var read = reader.ReadSignals(signals);
            if (read.IsFailure)
            {
                return read.ToNewResult<IFlowComponent>();
            }
        }

        return GenericResult<IFlowComponent>.Success(new FlowActionComponent(step.Value, signals, context.Logger));
    }

    // The body of a loop, built first (FDW-6134): the components it holds, and for a while loop the decisions of its test built a second time, because the loop judges its test before each round
    // and so runs them first, in a scope of their own. An action that holds nothing has no body.
    private static IGenericResult<IReadOnlyList<IFlowStep>> BuildBody(IGraphPipelineConfiguration graph, PipelineComponentConfiguration stored, IComponentType kind, FlowBuildContext context)
    {
        if (!kind.HoldsComponents)
        {
            return GenericResult<IReadOnlyList<IFlowStep>>.Success([]);
        }

        var level = BuildLevel(graph, stored.Name, $"{stored.Name} body", context);
        if (level.IsFailure || level.Value is null)
        {
            return level.ToNewResult<IReadOnlyList<IFlowStep>>();
        }

        if (kind is WhileComponentType && DecisionTests.TestOutputOf(stored) is { } testOutput && FlowFired.Parse(testOutput) is { } test)
        {
            var decisions = BuildLevel(graph, stored.Name, $"{stored.Name} test", context, only: DecisionTests.ConeOf(graph, stored.Name, test.Component));
            return decisions.IsFailure || decisions.Value is null
                ? decisions.ToNewResult<IReadOnlyList<IFlowStep>>()
                : GenericResult<IReadOnlyList<IFlowStep>>.Success([level.Value, new FlowLoopTest(decisions.Value)]);
        }

        return GenericResult<IReadOnlyList<IFlowStep>>.Success([level.Value]);
    }

    // A component that moves rows. Why built when its stage starts, for one that names a value a component of a lower stage calculates: a source that names its
    // physical path or name as {{Name}} reads it from the variables as the stages before it left them (FDW-1958); so does a script that names $(Name)
    // (FDW-1970), and a mapping whose constant names a value (FDW-2119). Every other component is built now, so what is wrong with it is found before any row moves.
    private static IGenericResult<IFlowComponent> BuildRows(
        IGraphPipelineConfiguration graph, PipelineComponentConfiguration stored, FlowBuildContext context, FlowBuildContext flowContext, IReadOnlyList<PipelineLinkConfiguration> links)
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

        var (inputs, outputs) = ComponentShapes.Of(stored, links, graph.Components);
        return GenericResult<IFlowComponent>.Success(new FlowLazyComponent(
            stored.Name, inputs, outputs, ComponentShapes.ErrorOutputsOf(stored, links, graph.Components), variables => Assemble(stored, context, flowContext, variables.Snapshot())));
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
