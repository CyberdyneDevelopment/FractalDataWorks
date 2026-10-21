using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.Transformations;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Judges the parameters, variables and components of a stored graph (FDW-1403, FDW-1549, FDW-2041): each component's own settings against the
/// <c>ComponentTypes</c> catalogue, how loops nest, and that every variable a component names is one the graph or an enclosing loop declares.
/// </summary>
/// <remarks>
/// It reports; it never repairs. A component with a problem of its own is noted in <c>broken</c> so what follows from it is not
/// reported a second time. The order things run in is the stage of each component (FDW-2040): one that sets a variable runs before
/// one that reads it only when its stage is lower.
/// </remarks>
public static class GraphComponentValidator
{
    /// <summary>Lists what is wrong with the graph's variables and components.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="broken">Receives the names of the components that have a problem of their own.</param>
    /// <param name="logger">The logger a condition's refusal is written to.</param>
    /// <returns>The problems, each with its path.</returns>
    public static IReadOnlyList<GraphProblem> Problems(GraphPipelineConfiguration graph, HashSet<string> broken, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(broken);
        ArgumentNullException.ThrowIfNull(logger);
        var problems = new List<GraphProblem>();
        problems.AddRange(ParameterProblems(graph));
        problems.AddRange(VariableProblems(graph));
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in graph.Components)
        {
            if (!names.Add(component.Name))
            {
                problems.Add(new GraphProblem(["components", component.Name], $"graph pipeline '{graph.Name}' has more than one component named '{component.Name}'"));
                broken.Add(component.Name);
            }

            // Why the load options only when the settings are sound: a destination that misses a setting is reported for that first.
            var own = ComponentSettings.Problems(component);
            if (own.Count == 0)
            {
                own = DestinationOptions.Problems(component, logger);
            }

            if (own.Count == 0)
            {
                own = [.. MappingProblems(graph, component)];
            }

            if (own.Count > 0)
            {
                broken.Add(component.Name);
                problems.AddRange(own);
                continue;
            }

            var before = problems.Count;
            problems.AddRange(PlaceProblems(graph, component));
            problems.AddRange(KindProblems(graph, component, logger));
            if (problems.Count > before)
            {
                broken.Add(component.Name);
            }
        }

        problems.AddRange(SourceParameterProblems(graph));
        problems.AddRange(ScriptTokenProblems(graph));
        problems.AddRange(SettingBindingRules.Problems(graph));
        return problems;
    }

    // The mappings on a component's inputs (FDW-2013): each names an input the component has and something is linked to; a union all maps every
    // input or none; and a mapping a run supplies as a list can only be for a component with one input, since the list names no input.
    private static IEnumerable<GraphProblem> MappingProblems(GraphPipelineConfiguration graph, PipelineComponentConfiguration component)
    {
        var rows = component.Fields
            .Where(field => string.Equals(field.SettingName, ComponentTypeBase.MappingsSetting, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var inputs = ComponentShapes.Of(component, [.. graph.Links]).Inputs;
        var path = new[] { "components", component.Name, "settings", ComponentTypeBase.MappingsSetting };
        if (ParameterSettings.IsBound(component, ComponentTypeBase.MappingsSetting) && inputs.Count != 1)
        {
            yield return new GraphProblem(path, $"component '{component.Name}' takes its mappings from a run parameter, which names no input, but it has {inputs.Count} inputs");
        }

        var mapped = rows.Select(row => row.InputName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var name in mapped.Where(name => name is null || !inputs.Contains(name, StringComparer.OrdinalIgnoreCase)))
        {
            yield return new GraphProblem(path, $"component '{component.Name}' maps the input '{name}', which it does not have or nothing is linked to");
        }

        var unmapped = inputs.Where(input => !mapped.Contains(input, StringComparer.OrdinalIgnoreCase)).ToList();
        if (mapped.Count > 0 && unmapped.Count > 0 && string.Equals(component.ComponentType, "UnionAll", StringComparison.OrdinalIgnoreCase))
        {
            yield return new GraphProblem(path, $"union all '{component.Name}' maps every input or none, but {string.Join(", ", unmapped.Select(input => $"'{input}'"))} is not mapped");
        }
    }

    // A source or destination may name what it reads or writes as a run parameter (FDW-1689), or as a variable the pipeline declares (FDW-1942);
    // so may a value of a SQL source's script parameters. The name must be a declared parameter, or a declared variable that no component changes: the
    // settings are read when the run starts, so a variable a component sets would be rendered with its starting value and the component ignored. The
    // exception is a setting that is read when its stage starts (FDW-1958, the physical path and name of a source): it may also name a variable
    // that a component of a lower stage sets.
    private static IEnumerable<GraphProblem> SourceParameterProblems(GraphPipelineConfiguration graph)
    {
        var declared = new HashSet<string>(graph.Parameters.Select(parameter => parameter.Name), StringComparer.OrdinalIgnoreCase);
        var assigned = Building.GraphScope.AssignedVariables(graph);
        var fixedVariables = new HashSet<string>(
            graph.Variables.Select(variable => variable.Name).Where(name => !assigned.Contains(name)), StringComparer.OrdinalIgnoreCase);
        foreach (var component in graph.Components)
        {
            var texts = ParameterSettings.Of(component).Select(found => (found.Setting, found.Text, Late: false))
                .Concat(ParameterSettings.LateOf(component).Select(found => (found.Setting, found.Text, Late: true)))
                .Concat(component.Fields
                    .Where(field => string.Equals(field.SettingName, Building.ScriptParameters.Setting, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(field.ConstantText))
                    .Select(field => (Setting: Building.ScriptParameters.Setting, Text: field.ConstantText!, Late: false)))
                .Concat(ParameterSettings.MappingConstants(component).Select(field => (Setting: ComponentTypeBase.MappingsSetting, Text: field.ConstantText!, Late: true)));
            foreach (var (setting, text, late) in texts)
            {
                foreach (var name in VariableText.Names(text).Where(name => !declared.Contains(name) && !fixedVariables.Contains(name) && !(late && RunFacts.IsFact(name))))
                {
                    var isVariable = assigned.Contains(name) && graph.Variables.Any(variable => string.Equals(variable.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (late && isVariable && SetBefore(graph, name, component))
                    {
                        continue;
                    }

                    yield return new GraphProblem(["components", component.Name, "settings", setting], SourceParameterReason(component.Name, setting, name, isVariable, late));
                }
            }
        }
    }

    // A SQL source's script may name $(Name) (FDW-1970): the name is a declared parameter or variable, and a variable a component sets is set by a component
    // of a lower stage than the source's own, because the script is rendered when the source's stage starts.
    private static IEnumerable<GraphProblem> ScriptTokenProblems(GraphPipelineConfiguration graph)
    {
        var parameters = new HashSet<string>(graph.Parameters.Select(parameter => parameter.Name).Concat(RunFacts.Names), StringComparer.OrdinalIgnoreCase);
        var variables = new HashSet<string>(graph.Variables.Select(variable => variable.Name), StringComparer.OrdinalIgnoreCase);
        var assigned = Building.GraphScope.AssignedVariables(graph);
        foreach (var component in graph.Components.Where(component => ScriptTokens.Holds(component.ScriptText) && !IsAction(component)))
        {
            foreach (var name in ScriptTokens.Names(component.ScriptText!).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var path = new[] { "components", component.Name, "settings", "script" };
                if (!parameters.Contains(name) && !variables.Contains(name))
                {
                    yield return new GraphProblem(path, $"component '{component.Name}' names $({name}) in its script, but the pipeline declares no parameter or variable '{name}'");
                }
                else if (!parameters.Contains(name) && assigned.Contains(name) && !SetBefore(graph, name, component))
                {
                    yield return new GraphProblem(path, $"component '{component.Name}' names $({name}) in its script, but no component of a lower stage sets '{name}', so it would have only its starting value; give the component that sets '{name}' a lower stage than this one");
                }
            }
        }
    }

    private static string SourceParameterReason(string component, string setting, string name, bool isAssignedVariable, bool late) =>
        (isAssignedVariable, late) switch
        {
            (true, true) => $"component '{component}' names the variable '{name}' in its {setting} setting, but no component of a lower stage sets '{name}', so it would have only its starting value; give the component that sets '{name}' a lower stage than this one",
            (true, false) => $"component '{component}' names the variable '{name}' in its {setting} setting, but a component sets '{name}' while the run goes, and this setting is read when the run starts, so that component would be ignored; name a parameter or a variable no component sets (a source's physical path and name are read when its stage starts, so they may name a variable a component of a lower stage sets)",
            _ => $"component '{component}' names '{name}' in its {setting} setting, but the pipeline declares no parameter or variable '{name}'",
        };

    private static bool IsAction(PipelineComponentConfiguration component) => ComponentTypes.ByName(component.ComponentType).IsAction;

    // Whether a component that runs before the given one sets the variable: it names it as the variable it sets or the one that keeps its result.
    private static bool SetBefore(GraphPipelineConfiguration graph, string variable, PipelineComponentConfiguration reader) =>
        graph.Components.Any(other => !ReferenceEquals(other, reader)
            && (string.Equals(other.VariableName, variable, StringComparison.OrdinalIgnoreCase) || string.Equals(other.ResultVariable, variable, StringComparison.OrdinalIgnoreCase))
            && RunsBefore(graph, other, reader));

    /// <summary>Whether the first component has finished by the time the second starts: in the first place where their chains of loops differ, the first's stage is lower.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="first">The component that must have finished.</param>
    /// <param name="second">The component that starts after it.</param>
    /// <returns><see langword="true"/> when the first's stage is lower; a component inside a loop is never before the loop's own settings, nor after them.</returns>
    internal static bool RunsBefore(GraphPipelineConfiguration graph, PipelineComponentConfiguration first, PipelineComponentConfiguration second)
    {
        var a = Chain(graph, first);
        var b = Chain(graph, second);
        for (var index = 0; index < Math.Min(a.Count, b.Count); index++)
        {
            if (ReferenceEquals(a[index], b[index]))
            {
                continue;
            }

            return (a[index].Stage ?? 0) < (b[index].Stage ?? 0);
        }

        return false;
    }

    // The loops that hold the component, outermost first, then the component itself.
    private static List<PipelineComponentConfiguration> Chain(GraphPipelineConfiguration graph, PipelineComponentConfiguration component)
    {
        var chain = new List<PipelineComponentConfiguration> { component };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { component.Name };
        var current = component;
        while (current.ParentComponent is { } parentName && seen.Add(parentName))
        {
            var parent = graph.Components.FirstOrDefault(candidate => string.Equals(candidate.Name, parentName, StringComparison.OrdinalIgnoreCase));
            if (parent is null)
            {
                break;
            }

            chain.Insert(0, parent);
            current = parent;
        }

        return chain;
    }

    // A parameter has a name of its own among the parameters and the variables, a data type, and is not a name a run reserves.
    private static IEnumerable<GraphProblem> ParameterProblems(GraphPipelineConfiguration graph)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in graph.Parameters)
        {
            string[] path = ["parameters", parameter.Name];
            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                yield return new GraphProblem(["parameters"], $"graph pipeline '{graph.Name}' declares a parameter with no name");
            }
            else if (!names.Add(parameter.Name))
            {
                yield return new GraphProblem(path, $"graph pipeline '{graph.Name}' declares the parameter '{parameter.Name}' more than once");
            }
            else if (graph.Variables.Any(variable => string.Equals(variable.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)))
            {
                yield return new GraphProblem(path, $"'{parameter.Name}' is declared as both a parameter and a variable of graph pipeline '{graph.Name}'");
            }
            else if (RunFacts.IsFact(parameter.Name))
            {
                yield return new GraphProblem(path, $"'{parameter.Name}' is a name a run reserves, so a parameter cannot take it");
            }

            if (!GraphValues.TryRead(parameter.ParameterType, null, out _))
            {
                yield return new GraphProblem(path, $"parameter '{parameter.Name}' is declared as '{parameter.ParameterType}', which is not a data type");
            }

            foreach (var defaultProblem in DefaultProblems(parameter))
            {
                yield return defaultProblem;
            }
        }
    }

    // A parameter's default (FDW-1975) is the designer's text of the parameter's own type. A required parameter is always given, so it has none,
    // and a list has none yet.
    private static IEnumerable<GraphProblem> DefaultProblems(PipelineParameterConfiguration parameter)
    {
        if (parameter.DefaultText is not { } text)
        {
            yield break;
        }

        string[] path = ["parameters", parameter.Name, "defaultValue"];
        if (parameter.IsRequired)
        {
            yield return new GraphProblem(path, $"parameter '{parameter.Name}' is required and declares a default; a required parameter is always given, so make it optional or drop the default");
        }
        else if (parameter.IsList)
        {
            yield return new GraphProblem(path, $"parameter '{parameter.Name}' is a list and declares a default; a default is not supported for a list");
        }
        else if (!GraphValues.TryRead(parameter.ParameterType, text, out _))
        {
            yield return new GraphProblem(path, $"the default of parameter '{parameter.Name}' is not a value of the type '{parameter.ParameterType}'");
        }
    }

    private static IEnumerable<GraphProblem> VariableProblems(GraphPipelineConfiguration graph)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in graph.Variables)
        {
            if (!names.Add(variable.Name))
            {
                yield return new GraphProblem(["variables", variable.Name], $"graph pipeline '{graph.Name}' declares the variable '{variable.Name}' more than once");
            }
            else if (RunFacts.IsFact(variable.Name))
            {
                yield return new GraphProblem(["variables", variable.Name], $"'{variable.Name}' is a name a run reserves for a fact about the run, so a variable cannot take it");
            }
            else if (!GraphValues.TryRead(variable.DataType, variable.ValueText, out _))
            {
                yield return new GraphProblem(
                    ["variables", variable.Name],
                    $"variable '{variable.Name}' is declared as '{variable.DataType}', which is not a data type or cannot read the value '{variable.ValueText}'");
            }
        }
    }

    // Where the component sits: its parent exists and is a loop, the parents do not loop back, a component with a parent is not a loop's
    // body that holds nothing, and a loop has components in its body.
    private static IEnumerable<GraphProblem> PlaceProblems(GraphPipelineConfiguration graph, PipelineComponentConfiguration component) =>
        ParentProblems(graph, component).Concat(HoldingProblems(graph, component));

    private static IEnumerable<GraphProblem> ParentProblems(GraphPipelineConfiguration graph, PipelineComponentConfiguration component)
    {
        if (component.ParentComponent is not { } parentName)
        {
            yield break;
        }

        string[] path = ["components", component.Name, "parent"];
        var parent = graph.Components.FirstOrDefault(c => string.Equals(c.Name, parentName, StringComparison.OrdinalIgnoreCase));
        if (parent is null)
        {
            yield return new GraphProblem(path, $"component '{component.Name}' is in the body of '{parentName}', which is not a component of the graph");
        }
        else if (!ComponentTypes.ByName(parent.ComponentType).HoldsComponents)
        {
            yield return new GraphProblem(path, $"component '{component.Name}' is in the body of '{parentName}', which is a {parent.ComponentType} component and has no body");
        }
        else if (LoopsBack(graph, component))
        {
            yield return new GraphProblem(path, $"component '{component.Name}' is inside itself: its parents lead back to it");
        }
    }

    private static IEnumerable<GraphProblem> HoldingProblems(GraphPipelineConfiguration graph, PipelineComponentConfiguration component)
    {
        var kind = ComponentTypes.ByName(component.ComponentType);
        if (kind.HoldsComponents && !graph.Components.Any(child => string.Equals(child.ParentComponent, component.Name, StringComparison.OrdinalIgnoreCase)))
        {
            yield return new GraphProblem(["components", component.Name], $"component '{component.Name}' of kind '{kind.Name}' has no components in its body");
        }
    }

    private static bool LoopsBack(GraphPipelineConfiguration graph, PipelineComponentConfiguration component)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { component.Name };
        var current = component;
        while (current.ParentComponent is { } parentName)
        {
            if (!seen.Add(parentName))
            {
                return true;
            }

            var parent = graph.Components.FirstOrDefault(c => string.Equals(c.Name, parentName, StringComparison.OrdinalIgnoreCase));
            if (parent is null)
            {
                return false;
            }

            current = parent;
        }

        return false;
    }

    // What a kind's settings mean together, judged with the variables the component can see.
    private static IEnumerable<GraphProblem> KindProblems(GraphPipelineConfiguration graph, PipelineComponentConfiguration component, ILogger logger)
    {
        var scope = VariablesInScope(graph, component);
        string[] settings = ["components", component.Name, "settings"];
        GraphProblem Problem(string setting, string reason) => new([.. settings, setting], reason);

        return LoopProblems(component, scope, logger, Problem)
            .Concat(SetVariableProblems(component, scope, Problem))
            .Concat(ExecuteProblems(component, scope, Problem))
            .Concat(ExecuteSqlProblems(component, scope, Problem))
            .Concat(NotifyProblems(component, Problem))
            .Concat(ReadOnlyTargetProblems(graph, component, Problem));
    }

    private static IEnumerable<GraphProblem> LoopProblems(
        PipelineComponentConfiguration component, VariableScope scope, ILogger logger, Func<string, string, GraphProblem> problem)
    {
        if (component.ComponentType.Equals("ForLoop", StringComparison.OrdinalIgnoreCase) && component.StepValue == 0)
        {
            yield return problem("step", $"for loop '{component.Name}' steps by zero, which never reaches its end");
        }

        if (component.ComponentType.Equals("ForEach", StringComparison.OrdinalIgnoreCase) && !IsBound(component, "list") && (component.ListVariable is null || !scope.Contains(component.ListVariable)))
        {
            yield return problem("list", $"for each '{component.Name}' reads its list from '{component.ListVariable}', which is not a variable in scope");
        }

        if (!component.ComponentType.Equals("DoWhile", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        if (component.MaxIterations < 1)
        {
            yield return problem("maximum", $"do while '{component.Name}' may run its components at most {component.MaxIterations} times, which is not at least one");
        }

        if (component.ConditionExpression is { } condition && FlowCondition.FromExpression(condition, logger) is { IsFailure: true } unreadable)
        {
            yield return problem("condition", $"do while '{component.Name}' has a condition that cannot be read: {unreadable.CurrentMessage}");
        }
    }

    // No component changes a parameter the graph declared read-only: not a set variable component, and not a component that keeps its result in one.
    private static IEnumerable<GraphProblem> ReadOnlyTargetProblems(
        GraphPipelineConfiguration graph, PipelineComponentConfiguration component, Func<string, string, GraphProblem> problem)
    {
        bool IsReadOnly(string? name) =>
            name is not null && graph.Parameters.Any(parameter => parameter.IsReadOnly && string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase));

        if (component.ComponentType.Equals("SetVariable", StringComparison.OrdinalIgnoreCase) && IsReadOnly(component.VariableName))
        {
            yield return problem("variable", $"Set variable '{component.Name}' sets '{component.VariableName}', which is a read-only parameter");
        }

        var keepsResult = component.ComponentType.Equals("ExecutePipeline", StringComparison.OrdinalIgnoreCase) || component.ComponentType.Equals("ExecuteSql", StringComparison.OrdinalIgnoreCase);
        if (keepsResult && IsReadOnly(component.ResultVariable))
        {
            yield return problem("resultVariable", $"'{component.Name}' keeps its result in '{component.ResultVariable}', which is a read-only parameter");
        }
    }

    private static IEnumerable<GraphProblem> SetVariableProblems(
        PipelineComponentConfiguration component, VariableScope scope, Func<string, string, GraphProblem> problem)
    {
        if (!component.ComponentType.Equals("SetVariable", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        if (!IsBound(component, "variable") && (component.VariableName is null || !scope.Contains(component.VariableName)))
        {
            yield return problem("variable", $"Set variable '{component.Name}' sets '{component.VariableName}', which is not a variable in scope");
        }

        // Why a Set variable with a transformation is judged apart (FDW-1957, FDW-2042): its value is the input of the calculation, so it must be
        // given and may name only variables in scope; its type is the transformation's, so a stated one has no meaning.
        if (component.TransformationName is { } transformation)
        {
            foreach (var calculationProblem in CalculationProblems(component, transformation, scope, problem))
            {
                yield return calculationProblem;
            }

            yield break;
        }

        if (component.ConstantType is null)
        {
            yield return problem("valueType", $"Set variable '{component.Name}' gives no data type for its value, and names no transformation to give one");
        }
        else if (!GraphValues.TryRead(component.ConstantType, component.ConstantText, out _))
        {
            yield return problem("value", $"Set variable '{component.Name}' gives '{component.ConstantText}' as a value of the type '{component.ConstantType}', which cannot read it");
        }

        if (component.Parameters.Count > 0)
        {
            yield return problem("parameters", $"Set variable '{component.Name}' gives transformation parameters and names no transformation");
        }
    }

    private static IEnumerable<GraphProblem> CalculationProblems(
        PipelineComponentConfiguration component, string transformation, VariableScope scope, Func<string, string, GraphProblem> problem)
    {
        if (component.ConstantType is not null)
        {
            yield return problem("valueType", $"Set variable '{component.Name}' applies '{transformation}', which gives the type of the value; a data type is not given with it");
        }

        if (component.ConstantText is null)
        {
            yield return problem("value", $"Set variable '{component.Name}' applies '{transformation}' and gives no value to apply it to");
        }

        foreach (var name in VariableText.Names(component.ConstantText ?? string.Empty).Where(reference => !scope.Contains(reference)))
        {
            yield return problem("value", $"Set variable '{component.Name}' reads '{name}', which is not a variable in scope");
        }

        foreach (var transformationProblem in TransformationProblems(component, transformation, problem))
        {
            yield return transformationProblem;
        }

        foreach (var parameterProblem in ParameterProblems(component, scope, problem))
        {
            yield return parameterProblem;
        }
    }

    private static IEnumerable<GraphProblem> TransformationProblems(
        PipelineComponentConfiguration component, string transformation, Func<string, string, GraphProblem> problem)
    {
        if (TransformationTypes.ByName(transformation) is not FieldTransformationBase transformer)
        {
            yield return problem("transformation", $"Set variable '{component.Name}' applies '{transformation}', which is not a registered field transformation");
            yield break;
        }

        foreach (var required in transformer.ExpectedParameters.Where(definition => definition.IsRequired))
        {
            if (!component.Parameters.Any(parameter => string.Equals(parameter.ParameterName, required.Name, StringComparison.Ordinal)))
            {
                yield return problem("parameters", $"Set variable '{component.Name}' applies '{transformation}' without its required parameter '{required.Name}'");
            }
        }
    }

    private static IEnumerable<GraphProblem> ExecuteProblems(
        PipelineComponentConfiguration component, VariableScope scope, Func<string, string, GraphProblem> problem)
    {
        if (!component.ComponentType.Equals("ExecutePipeline", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        if (component.AwaitCompletion is true && component.FailOnChildFailure is null)
        {
            yield return problem("failOnChildFailure", $"execute pipeline '{component.Name}' waits for '{component.TargetPipeline}' but does not state whether it fails when that pipeline fails");
        }

        if (component.AwaitCompletion is false && (component.ResultVariable is not null || component.FailOnChildFailure is not null))
        {
            yield return problem("awaitCompletion", $"execute pipeline '{component.Name}' does not wait for '{component.TargetPipeline}', so it has no result to keep or to fail on");
        }

        if (component.ResultVariable is { } result && !scope.Contains(result))
        {
            yield return problem("resultVariable", $"execute pipeline '{component.Name}' keeps its result in '{result}', which is not a variable in scope");
        }

        if (component.RunPoint is { } runPoint)
        {
            var point = RunPoints.ByName(runPoint);
            if (ReferenceEquals(point, RunPoints.NotFound))
            {
                yield return problem("runPoint", $"execute pipeline '{component.Name}' starts at '{runPoint}', which is not a run point");
            }
            else if (point.FiresOnFailure && component.ParentComponent is not null)
            {
                yield return problem("runPoint", $"execute pipeline '{component.Name}' starts on failure but sits inside '{component.ParentComponent}'; a component that starts on failure sits at the top of the pipeline");
            }
        }

        foreach (var parameterProblem in ParameterProblems(component, scope, problem))
        {
            yield return parameterProblem;
        }
    }

    // Execute SQL: the variable that keeps the rows affected is one in scope, and the parameters are given once and name only
    // variables in scope.
    private static IEnumerable<GraphProblem> ExecuteSqlProblems(
        PipelineComponentConfiguration component, VariableScope scope, Func<string, string, GraphProblem> problem)
    {
        if (!component.ComponentType.Equals("ExecuteSql", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        if (component.ResultVariable is { } result && !scope.Contains(result))
        {
            yield return problem("resultVariable", $"execute SQL '{component.Name}' keeps the rows it affected in '{result}', which is not a variable in scope");
        }

        foreach (var name in ScriptTokens.Names(component.ScriptText ?? string.Empty).Where(token => !scope.Contains(token)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return problem("script", $"execute SQL '{component.Name}' names $({name}) in its script, which is not a parameter or variable in scope");
        }

        foreach (var parameterProblem in ParameterProblems(component, scope, problem))
        {
            yield return parameterProblem;
        }
    }

    // Notify: the run point is one the platform has, a component that fires on failure sits at the top of the pipeline (the pipeline
    // fires it when the run ends in failure, and it reaches only the components it holds directly), and the message names only variables
    // a run has.
    private static IEnumerable<GraphProblem> NotifyProblems(PipelineComponentConfiguration component, Func<string, string, GraphProblem> problem)
    {
        if (!component.ComponentType.Equals("Notify", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        if (component.RunPoint is { } runPoint)
        {
            var point = RunPoints.ByName(runPoint);
            if (ReferenceEquals(point, RunPoints.NotFound))
            {
                yield return problem("runPoint", $"notify '{component.Name}' fires at '{runPoint}', which is not a run point");
            }
            else if (point.FiresOnFailure && component.ParentComponent is not null)
            {
                yield return problem("runPoint", $"notify '{component.Name}' fires on failure but sits inside '{component.ParentComponent}'; a component that fires on failure sits at the top of the pipeline");
            }
        }

        if (component.NotificationMessage is { } message && NotifyTemplate.Unknown(message) is { Count: > 0 } unknown)
        {
            yield return problem("message", $"notify '{component.Name}' has a message that uses unknown variables [{string.Join(", ", unknown)}]; a run has [{NotifyTemplate.Reserved}]");
        }
    }

    private static IEnumerable<GraphProblem> ParameterProblems(
        PipelineComponentConfiguration component, VariableScope scope, Func<string, string, GraphProblem> problem)
    {
        var parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in component.Parameters)
        {
            if (!parameters.Add(parameter.ParameterName))
            {
                yield return problem("parameters", $"component '{component.Name}' gives the parameter '{parameter.ParameterName}' twice");
            }

            foreach (var name in VariableText.Names(parameter.ValueText).Where(reference => !scope.Contains(reference)))
            {
                yield return problem("parameters", $"the parameter '{parameter.ParameterName}' of '{component.Name}' names the variable '{name}', which is not in scope");
            }
        }
    }

    // The graph's variables and parameters and the counters or elements of every loop that holds the component.
    private static VariableScope VariablesInScope(GraphPipelineConfiguration graph, PipelineComponentConfiguration component)
    {
        var scope = new VariableScope();
        foreach (var name in graph.Variables.Select(variable => variable.Name).Concat(graph.Parameters.Select(parameter => parameter.Name)).Concat(RunFacts.Names))
        {
            scope.Add(name);
        }

        // Why the run's facts only for Execute pipeline with a run point: it is the one component whose parameter values may name them (FDW-2043).
        if (component.ComponentType.Equals("ExecutePipeline", StringComparison.OrdinalIgnoreCase) && component.RunPoint is not null)
        {
            foreach (var fact in NotifyVariables.All())
            {
                scope.Add(fact.Name);
            }
        }

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { component.Name };
        var current = component;
        while (current.ParentComponent is { } parentName && visited.Add(parentName))
        {
            var parent = graph.Components.FirstOrDefault(c => string.Equals(c.Name, parentName, StringComparison.OrdinalIgnoreCase));
            if (parent is null)
            {
                break;
            }

            var isLoop = parent.ComponentType.Equals("ForLoop", StringComparison.OrdinalIgnoreCase) || parent.ComponentType.Equals("ForEach", StringComparison.OrdinalIgnoreCase);
            if (isLoop && parent.VariableName is { } loopVariable)
            {
                scope.Add(loopVariable);
            }
            else if (isLoop && IsBound(parent, "variable"))
            {
                // Why open: the loop's variable is chosen by the run, so what is in scope inside it is known only when the run starts.
                scope.IsOpen = true;
            }

            current = parent;
        }

        return scope;
    }

    // Whether a component's setting takes its value from a run parameter; the checks that need the value wait for the run.
    private static bool IsBound(PipelineComponentConfiguration component, string setting) =>
        component.SettingBindings.Any(binding => string.Equals(binding.SettingName, setting, StringComparison.OrdinalIgnoreCase));

    /// <summary>Lists what a kind's settings mean together, judged with the component's real names: the check a run makes after it has put the values of its parameters on the component.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="component">The component as the run builds it, with its bound settings given their values.</param>
    /// <param name="logger">The logger a condition's refusal is written to.</param>
    /// <returns>The problems, each with its path.</returns>
    public static IReadOnlyList<GraphProblem> ComponentProblems(GraphPipelineConfiguration graph, PipelineComponentConfiguration component, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(logger);
        return [.. KindProblems(graph, component, logger)];
    }
}
