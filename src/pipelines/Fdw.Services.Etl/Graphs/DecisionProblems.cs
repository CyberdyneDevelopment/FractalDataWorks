using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The rules a stored graph's decisions and loop tests must keep (FDW-6134): a decision's output is read by a line or by a loop's test, a gate is fed by enough decisions for its logic,
/// a switch states its outcomes soundly, decisions do not feed one another in a circle, and a loop states its test once, as a condition or as the output of a decision inside it. Each
/// refusal is a coded message (FLOW) that is logged and is the reason of the problem.
/// </summary>
public static class DecisionProblems
{
    /// <summary>Lists every rule the graph's decisions and loop tests break.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="logger">The logger each refusal is written to.</param>
    /// <returns>The problems, each with its path; empty when they are sound.</returns>
    public static IReadOnlyList<GraphProblem> Of(IGraphPipelineConfiguration graph, ILogger logger)
    {
        var problems = new List<GraphProblem>();
        foreach (var component in graph.Components)
        {
            var kind = ComponentTypes.ByName(component.ComponentType);
            if (kind is IDecisionComponentType decision)
            {
                problems.AddRange(OutputProblems(graph, component, decision, logger));
                if (kind is GateComponentType)
                {
                    problems.AddRange(GateProblems(graph, component, logger));
                }
                else if (kind is SwitchComponentType)
                {
                    problems.AddRange(SwitchProblems(component, logger));
                }
            }
            else if (kind is WhileComponentType or DoWhileComponentType)
            {
                problems.AddRange(LoopTestProblems(graph, component, logger));
            }
        }

        problems.AddRange(CycleProblems(graph, logger));
        return problems;
    }

    private static GraphProblem At(PipelineComponentConfiguration component, string reason) => new(["components", component.Name], reason);

    // An output nothing reads: no line leaves it, and no loop takes it as its test.
    private static IEnumerable<GraphProblem> OutputProblems(IGraphPipelineConfiguration graph, PipelineComponentConfiguration component, IDecisionComponentType kind, ILogger logger)
    {
        foreach (var output in DecisionTests.OutputsOf(component))
        {
            var led = graph.Links.Any(link => string.Equals(link.FromComponent, component.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(link.FromOutput, output, StringComparison.OrdinalIgnoreCase));
            var tested = graph.Components.Any(loop => DecisionTests.TestOutputOf(loop) is { } text && FlowFired.Parse(text) is { } signal
                && string.Equals(signal.Component, component.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(signal.Output, output, StringComparison.OrdinalIgnoreCase));
            if (!led && !tested)
            {
                yield return At(component, FlowLog.ComponentOutputUnused(logger, component.Name, kind.Name, output).Message);
            }
        }
    }

    private static IEnumerable<GraphProblem> GateProblems(IGraphPipelineConfiguration graph, PipelineComponentConfiguration gate, ILogger logger)
    {
        var inputs = graph.Links.Count(link => string.Equals(link.ToComponent, gate.Name, StringComparison.OrdinalIgnoreCase) && CompletionLines.IsSignal(link, graph.Components));
        if (inputs == 0)
        {
            yield return At(gate, FlowLog.GateInputUnfed(logger, gate.Name, "no line leads into it").Message);
        }

        if (DecisionTests.StatedText(gate, GateComponentType.LogicSetting) is not { } text)
        {
            yield break;
        }

        var logic = GateLogics.ByName(text);
        if (ReferenceEquals(logic, GateLogics.NotFound))
        {
            yield return At(gate, FlowLog.GateLogicInvalid(logger, gate.Name, text, $"is not one of {string.Join(", ", GateLogics.All().Select(known => known.Name))}").Message);
        }
        else if (inputs > 0 && (inputs < logic.MinimumInputs || (logic.MaximumInputs is { } most && inputs > most)))
        {
            var takes = logic.MaximumInputs == logic.MinimumInputs ? $"exactly {logic.MinimumInputs}" : $"at least {logic.MinimumInputs}";
            yield return At(gate, FlowLog.GateLogicInvalid(logger, gate.Name, text, $"takes {takes} input line(s), and the gate has {inputs}").Message);
        }
    }

    private static IEnumerable<GraphProblem> SwitchProblems(PipelineComponentConfiguration component, ILogger logger)
    {
        if (DecisionTests.StatedText(component, SwitchComponentType.OutcomesSetting) is not { } text)
        {
            yield break;
        }

        var outcomes = SwitchComponentType.OutcomesOf(text);
        if (outcomes.Count == 0)
        {
            yield return At(component, FlowLog.SwitchOutcomeMissing(logger, component.Name, "it names no outcome").Message);
            yield break;
        }

        foreach (var repeated in outcomes.GroupBy(outcome => outcome, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            yield return At(component, FlowLog.SwitchOutcomeMissing(logger, component.Name, $"the outcome '{repeated.Key}' is named more than once").Message);
        }

        if (DecisionTests.StatedText(component, SwitchComponentType.OtherwiseSetting) is { } otherwise
            && string.Equals(otherwise, bool.TrueString, StringComparison.OrdinalIgnoreCase)
            && outcomes.Any(outcome => string.Equals(outcome, SwitchComponentType.Otherwise, StringComparison.OrdinalIgnoreCase)))
        {
            yield return At(component, FlowLog.SwitchOutcomeMissing(logger, component.Name, $"an outcome is named '{SwitchComponentType.Otherwise}', which is the output of a value no outcome names").Message);
        }
    }

    private static IEnumerable<GraphProblem> LoopTestProblems(IGraphPipelineConfiguration graph, PipelineComponentConfiguration loop, ILogger logger)
    {
        var testOutput = DecisionTests.TestOutputOf(loop);
        var condition = !string.IsNullOrWhiteSpace(loop.ConditionExpression);
        if (condition == (testOutput is not null))
        {
            yield return At(loop, FlowLog.LoopTestAmbiguous(logger, loop.Name, condition ? "both" : "neither").Message);
            yield break;
        }

        if (testOutput is null)
        {
            yield break;
        }

        var signal = FlowFired.Parse(testOutput);
        var decision = signal is null ? null : graph.Components.FirstOrDefault(component => string.Equals(component.Name, signal.Component, StringComparison.OrdinalIgnoreCase));
        if (signal is null)
        {
            yield return At(loop, FlowLog.LoopTestNotInBody(logger, loop.Name, testOutput, "is not written component.output").Message);
        }
        else if (decision is null || !string.Equals(decision.ParentComponent, loop.Name, StringComparison.OrdinalIgnoreCase))
        {
            yield return At(loop, FlowLog.LoopTestNotInBody(logger, loop.Name, testOutput, $"names '{signal.Component}', which is not a component inside the loop").Message);
        }
        else if (!DecisionTests.OutputsOf(decision).Contains(signal.Output, StringComparer.OrdinalIgnoreCase))
        {
            yield return At(loop, FlowLog.LoopTestNotInBody(logger, loop.Name, testOutput, $"names '{signal.Component}', which is not a decision with the output '{signal.Output}'").Message);
        }
    }

    // Decisions that feed one another in a circle: found by taking away every decision no other decision it still depends on holds back, until none can go.
    private static IEnumerable<GraphProblem> CycleProblems(IGraphPipelineConfiguration graph, ILogger logger)
    {
        var decisions = graph.Components.Where(component => ComponentTypes.ByName(component.ComponentType) is IDecisionComponentType).Select(component => component.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var feeds = graph.Links
            .Where(link => decisions.Contains(link.FromComponent) && decisions.Contains(link.ToComponent) && CompletionLines.IsSignal(link, graph.Components))
            .Select(link => (link.FromComponent, link.ToComponent))
            .ToList();
        var remaining = new HashSet<string>(decisions, StringComparer.OrdinalIgnoreCase);
        bool removed;
        do
        {
            var ready = remaining.Where(name => !feeds.Any(line => string.Equals(line.ToComponent, name, StringComparison.OrdinalIgnoreCase) && remaining.Contains(line.FromComponent))).ToList();
            removed = ready.Count > 0;
            remaining.ExceptWith(ready);
        }
        while (removed);

        if (remaining.Count > 0)
        {
            var names = string.Join(", ", remaining.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).Select(name => $"'{name}'"));
            yield return new GraphProblem(["components", remaining.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).First()], FlowLog.DecisionCycle(logger, names).Message);
        }
    }
}
