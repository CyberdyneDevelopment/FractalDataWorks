using System;
using Fdw.Services.Etl;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Projects a configured pipeline onto the flat <see cref="PipelineLineageRecord"/> the lineage graph
/// builder consumes.
/// </summary>
/// <remarks>
/// Why: the previous mechanism read <c>pipe.Pipeline</c> as a flat single-table row, which structurally
/// cannot see the linkage columns (SourceDataSet/DestinationDataSet/SourceConnectionName/
/// DestinationConnectionName/IsEnabled) that live on the engine's own row. A read through the Pipeline
/// domain already dispatches to whichever implementation the row names, so this projection reads the
/// linkage off that implementation — polymorphically through
/// <see cref="IEtlPipelineImplementationConfiguration"/>, with no <c>is BatchCopy...</c> branch and no
/// re-implementation of the join.
/// </remarks>
internal static class PipelineLineageProjection
{
    /// <summary>
    /// Projects one configured pipeline to a <see cref="PipelineLineageRecord"/>. A pipeline whose
    /// implementation carries no ETL linkage renders NODE-ONLY — Name/Id/Implementation set, linkage
    /// left null — with a Warning naming the gap. NO FALLBACKS: linkage is never fabricated.
    /// </summary>
    /// <param name="configuration">The configured pipeline, as the Pipeline domain hands it back.</param>
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

        if (configuration is not IEtlPipelineImplementationConfiguration engine)
        {
            ApiEndpointLog.PipelineNodeOnlyNoBody(logger, configuration.Name);
            return record;
        }

        ApiEndpointLog.PipelineAggregateComposed(logger, configuration.Name, configuration.Implementation);

        record.SourceDataSet = engine.SourceDataSet;
        record.DestinationDataSet = engine.DestinationDataSet;
        record.IsEnabled = engine.IsEnabled;
        record.SourceConnectionName = engine.SourceConnectionName;
        record.DestinationConnectionName = engine.DestinationConnectionName;

        ApiEndpointLog.PipelineLinkageExtracted(
            logger, configuration.Name, engine.SourceDataSet, engine.DestinationDataSet,
            engine.SourceConnectionName, engine.DestinationConnectionName);

        return record;
    }
}
