using System;
using System.Linq;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Projects a configured pipeline onto the <see cref="PipelineLineageRecord"/> the lineage graph
/// builder consumes.
/// </summary>
/// <remarks>
/// Why: what a pipeline reads and writes is held in the settings of its components, so it is read the one way the whole
/// workspace reads it (<see cref="GraphPipelineUses"/>), on a pipeline read whole. Nothing here knows an engine by name.
/// </remarks>
internal static class PipelineLineageProjection
{
    /// <summary>
    /// Projects one configured pipeline to a <see cref="PipelineLineageRecord"/>. A pipeline that is not a graph renders
    /// NODE-ONLY: Name, Id and Implementation set, no uses, with a Warning naming the gap. NO FALLBACKS: linkage is never fabricated.
    /// </summary>
    /// <param name="configuration">The configured pipeline, as the Pipeline domain hands it back, read whole.</param>
    /// <param name="logger">Logger for verbose composition/linkage tracing.</param>
    public static PipelineLineageRecord From(IPipelineImplementationConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var record = new PipelineLineageRecord
        {
            Id = configuration.Id,
            Name = configuration.Name,
            Implementation = configuration.Implementation
        };

        if (configuration is not GraphPipelineConfiguration graph)
        {
            ApiEndpointLog.PipelineNodeOnlyNoBody(logger, configuration.Name);
            return record;
        }

        ApiEndpointLog.PipelineAggregateComposed(logger, configuration.Name, configuration.Implementation);

        record.IsEnabled = graph.IsEnabled;
        record.Uses = GraphPipelineUses.Of(graph);

        ApiEndpointLog.PipelineLinkageExtracted(
            logger, configuration.Name, record.DataSetsRead.Count(), record.ContainersRead.Count(),
            record.DataSetsWritten.Count(), record.ContainersWritten.Count());

        return record;
    }
}
