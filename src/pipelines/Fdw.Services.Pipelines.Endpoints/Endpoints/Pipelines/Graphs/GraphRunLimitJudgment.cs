using System;
using Fdw.Results;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Judges the time and step limits a graph pipeline states against the ceilings in force, at the save (FDW-6718), by the one rule the start uses
/// (<see cref="PipelineRunLimitCeilings"/>), so a value no run could start with is refused when it is written and not at the first run.
/// </summary>
/// <remarks>
/// Only a pipeline whose type runs at once is judged: the ceilings are the settings of the at-once run mode, and a queued pipeline that states limits is never started by that
/// runner. A caller who acts in no tenant has no ceilings to be judged against, so a save that states limits is refused, never let through unjudged.
/// </remarks>
public static class GraphRunLimitJudgment
{
    /// <summary>Judges the limits a graph states.</summary>
    /// <param name="graph">The graph as read from the request.</param>
    /// <param name="settings">The effective settings the ceilings are read from.</param>
    /// <param name="tenantId">The tenant the caller acts in, or null when it acts in none.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>A success, or a failure for each value above its ceiling (PIPELINERUN-20841), for a ceiling that is not set (PIPELINERUN-20844), or for a caller with no tenant.</returns>
    public static IGenericResult Judge(GraphPipelineConfiguration graph, IEffectiveSettingsProvider settings, Guid? tenantId, ILogger logger)
    {
        // Why first (FDW-6134): how deep components nest is judged for every graph pipeline, whatever its type; the run limits below only for one that runs at once.
        var depth = GraphContainerDepthJudgment.Judge(graph, settings, tenantId, logger);
        if (depth.IsFailure)
            return depth;

        var type = PipelineTypes.ByName(graph.Implementation);
        if (ReferenceEquals(type, PipelineTypes.NotFound) || PipelineRunModes.ByName(type.RunModeName) is not AtOncePipelineRunMode)
            return GenericResult.Success();

        if (graph.MaxRunSeconds is null && graph.MaxSteps is null)
            return GenericResult.Success();

        if (tenantId is not { } tenant)
            return GenericResult.Failure(GraphImplementationLog.LimitsNeedATenant(logger, graph.Name));

        var ceilings = PipelineRunLimitCeilings.Read(settings, tenant, logger);
        if (ceilings.IsFailure || ceilings.Value is not { } inForce)
            return ceilings;

        return PipelineRunLimitCeilings.Judge(graph.Name, graph.MaxRunSeconds, graph.MaxSteps, inForce, logger);
    }
}
