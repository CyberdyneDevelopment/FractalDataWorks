using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// What the settings that name a source or destination render from (FDW-1942): the run's parameters and the variables the pipeline declares
/// that no component changes.
/// </summary>
/// <remarks>
/// Why only a variable no step assigns: the data set, data store, path and container are read when the run starts, before any step has run.
/// A variable a step sets would be rendered with its starting value, silently ignoring the step, so a setting may not name one (the save
/// check refuses it). A variable no step sets has one value for the whole run, its starting value, and is the pipeline's way to say a
/// value once and use it in several components.
/// </remarks>
public static class GraphScope
{
    /// <summary>Lists the variables some step changes while the run goes: a Set variable target, a loop's variable, a step's result variable.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <returns>The names, compared without regard to case.</returns>
    public static IReadOnlySet<string> AssignedVariables(GraphPipelineConfiguration graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return graph.Components
            .SelectMany(component => new[] { component.VariableName, component.ResultVariable })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Makes the values a source or destination setting renders from: the run's parameters and the starting values of the variables no step changes.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="parameters">The run's typed values by parameter name.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The values by name, or the failure that names the variable whose starting value is not a value of its type.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> RenderValues(
        GraphPipelineConfiguration graph, IReadOnlyDictionary<string, object?> parameters, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(logger);
        var values = new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase);
        var assigned = AssignedVariables(graph);
        foreach (var variable in graph.Variables.Where(variable => !assigned.Contains(variable.Name)))
        {
            if (!GraphValues.TryRead(variable.DataType, variable.ValueText, out var value))
            {
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(GraphLog.VariableInvalid(
                    logger, graph.Name, variable.Name, $"'{variable.ValueText}' is not a value of the type '{variable.DataType}'"));
            }

            // Why TryAdd: a name declared as a parameter and as a variable is refused where the run declares them; the parameter is not replaced here.
            values.TryAdd(variable.Name, value);
        }

        return GenericResult<IReadOnlyDictionary<string, object?>>.Success(values);
    }
}
