using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The one method that judges the pipeline type a graph states (FDW-6718): the create, the update and the draft all ask it, so no route has its own copy.
/// </summary>
/// <remarks>
/// Judged: the type is stated and is a registered pipeline type that saves a graph (its configuration is the graph pipeline configuration), and the row states every fact
/// the type requires (<see cref="PipelineConfigurationRequirements"/>, one message for each fact left out). The ceilings on the values are judged where the settings are,
/// at the save, not here.
/// </remarks>
public static class GraphPipelineTypeJudgment
{
    /// <summary>Judges the type a graph states and the values it states for it.</summary>
    /// <param name="graph">The graph as read from the request.</param>
    /// <param name="logger">The logger the messages are written to.</param>
    /// <returns>One message for each thing wrong; empty when the type is stated, registered and its requirements are stated.</returns>
    public static IReadOnlyList<IGenericMessage> Judge(GraphPipelineConfiguration graph, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(graph.Implementation))
            return [GraphImplementationLog.ImplementationNotStated(logger, "The request")];

        var type = PipelineTypes.ByName(graph.Implementation);
        if (ReferenceEquals(type, PipelineTypes.NotFound) || type.ConfigurationType != typeof(GraphPipelineConfiguration))
            return [GraphImplementationLog.ImplementationNotRegistered(logger, graph.Implementation, Offered())];

        var missing = PipelineConfigurationRequirements.Missing(type, graph, logger);

        // Why the rule is judged here and not by the requirement: a requirement says a fact is stated; whether the stated rule is one the engine offers is its own refusal, in the same judgment
        // (FDW-6986), and none stands in for a rule that is not.
        return graph.HalfDoneRule is { Length: > 0 } rule && ReferenceEquals(HalfDoneRules.ByName(rule), HalfDoneRules.NotFound)
            ? [.. missing, GraphImplementationLog.HalfDoneRuleNotAnOption(logger, rule, OfferedRules())]
            : missing;
    }

    /// <summary>The names of the pipeline types that save a graph, for a refusal.</summary>
    /// <returns>The names, joined.</returns>
    /// <summary>Gets the half-done rules that are options, comma separated.</summary>
    /// <returns>The names.</returns>
    public static string OfferedRules() => string.Join(", ", HalfDoneRules.All().Select(rule => rule.Name));

    public static string Offered() => string.Join(", ", PipelineTypes.All().Values
        .Where(type => type.ConfigurationType == typeof(GraphPipelineConfiguration))
        .Select(type => type.Name));
}
