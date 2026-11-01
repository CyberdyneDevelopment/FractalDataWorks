using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Declares the parameters and variables a graph pipeline states (FDW-1403, FDW-1549) in the root scope of a run: a parameter with
/// the value the run was started with, or blank when an optional one was left out; a variable with its typed starting value.
/// </summary>
public static class GraphVariables
{
    /// <summary>Declares the graph's parameters and variables.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="supplied">The typed values the run was started with, by parameter name; null when it was started with none.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The root scope, or the failure that names the parameter or variable that cannot be declared.</returns>
    public static IGenericResult<FlowVariables> Declare(
        GraphPipelineConfiguration graph, IReadOnlyDictionary<string, object?>? supplied, ILogger logger) =>
        Declare(graph, supplied, null, logger);

    /// <summary>
    /// Declares the parameters, the variables and the run facts a graph pipeline runs with (FDW-1982): <c>LastSuccessfulRun</c> and
    /// <c>RunStarted</c> are declared read-only by the engine. A fact the run has no value for is declared blank, so anything that names it
    /// fails saying the run was started without one; no time is invented.
    /// </summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="supplied">The run's typed parameter values by name, which may include <c>LastSuccessfulRun</c>.</param>
    /// <param name="runStarted">The instant the host stamped when it started the run; none when the run was not started by the host's queue.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The variables in scope when the run starts, or the failure naming what cannot be declared.</returns>
    public static IGenericResult<FlowVariables> Declare(
        GraphPipelineConfiguration graph, IReadOnlyDictionary<string, object?>? supplied, DateTimeOffset? runStarted, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(logger);
        var variables = new FlowVariables();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parameters = DeclareParameters(graph, supplied, variables, seen, logger);
        if (parameters.IsFailure)
        {
            return parameters.ToNewResult<FlowVariables>();
        }

        var facts = DeclareRunFacts(graph, supplied, runStarted, variables, seen, logger);
        return facts.IsFailure ? facts.ToNewResult<FlowVariables>() : DeclareVariables(graph, variables, seen, logger);
    }

    // The facts the engine states about the run. A name the graph already declared is refused: the fact would be hidden behind it.
    private static IGenericResult DeclareRunFacts(
        GraphPipelineConfiguration graph, IReadOnlyDictionary<string, object?>? supplied, DateTimeOffset? runStarted,
        FlowVariables variables, HashSet<string> seen, ILogger logger)
    {
        foreach (var fact in RunFacts.Names)
        {
            if (!seen.Add(fact))
            {
                return GenericResult.Failure(GraphLog.ParameterInvalid(logger, graph.Name, fact, "it is a name a run reserves for a fact about the run"));
            }
        }

        if (supplied is not null && supplied.TryGetValue(RunFacts.LastSuccessfulRun, out var last) && last is not null)
        {
            variables.DeclareParameter(RunFacts.LastSuccessfulRun, last, true);
        }
        else
        {
            variables.DeclareBlankParameter(RunFacts.LastSuccessfulRun, true);
        }

        if (runStarted is { } started)
        {
            variables.DeclareParameter(RunFacts.RunStarted, started, true);
        }
        else
        {
            variables.DeclareBlankParameter(RunFacts.RunStarted, true);
        }

        return GenericResult.Success();
    }

    private static IGenericResult DeclareParameters(
        GraphPipelineConfiguration graph, IReadOnlyDictionary<string, object?>? supplied, FlowVariables variables, HashSet<string> seen, ILogger logger)
    {
        foreach (var parameter in graph.Parameters)
        {
            if (!seen.Add(parameter.Name))
            {
                return GenericResult.Failure(GraphLog.ParameterInvalid(logger, graph.Name, parameter.Name, "it is declared more than once"));
            }

            if (supplied is not null && supplied.TryGetValue(parameter.Name, out var value))
            {
                variables.DeclareParameter(parameter.Name, value, parameter.IsReadOnly);
            }
            else
            {
                variables.DeclareBlankParameter(parameter.Name, parameter.IsReadOnly);
            }
        }

        return supplied is null ? GenericResult.Success() : RefuseUndeclared(graph, supplied, logger);
    }

    // Why refused: a value the run supplied that no parameter takes would be dropped without a word.
    private static IGenericResult RefuseUndeclared(GraphPipelineConfiguration graph, IReadOnlyDictionary<string, object?> supplied, ILogger logger)
    {
        foreach (var name in supplied.Keys)
        {
            if (RunFacts.IsFact(name))
            {
                continue;
            }

            if (!graph.Parameters.Any(parameter => string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return GenericResult.Failure(GraphLog.ParameterInvalid(logger, graph.Name, name, "the run supplied a value for it but the pipeline does not declare it"));
            }
        }

        return GenericResult.Success();
    }

    private static IGenericResult<FlowVariables> DeclareVariables(
        GraphPipelineConfiguration graph, FlowVariables variables, HashSet<string> seen, ILogger logger)
    {
        foreach (var variable in graph.Variables)
        {
            if (!seen.Add(variable.Name))
            {
                return GenericResult<FlowVariables>.Failure(
                    GraphLog.VariableInvalid(logger, graph.Name, variable.Name, "it is declared more than once"));
            }

            if (!GraphValues.TryRead(variable.DataType, variable.ValueText, out var value))
            {
                return GenericResult<FlowVariables>.Failure(GraphLog.VariableInvalid(
                    logger, graph.Name, variable.Name, $"'{variable.ValueText}' is not a value of the type '{variable.DataType}'"));
            }

            variables.Declare(variable.Name, value);
        }

        return GenericResult<FlowVariables>.Success(variables);
    }
}
