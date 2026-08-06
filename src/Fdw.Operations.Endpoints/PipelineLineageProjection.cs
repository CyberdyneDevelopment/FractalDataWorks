using System;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Projects a composed <see cref="PipelineConfiguration"/> aggregate (header → <see cref="EtlPipelineConfiguration"/>
/// KIND body → <see cref="IEtlPipelineTypedConfiguration"/> ENGINE body) onto the flat
/// <see cref="PipelineLineageRecord"/> the lineage graph builder consumes.
/// </summary>
/// <remarks>
/// Why: the previous mechanism read <c>pipe.Pipeline</c> as a flat single-table row, which structurally
/// cannot see linkage columns (SourceDataSet/DestinationDataSet/SourceConnectionName/
/// DestinationConnectionName/IsEnabled) that live two levels down on the engine body. This projection
/// dot-walks the SAME composed aggregate <see cref="PipelineServiceConfigurationProvider"/> already
/// builds via <c>Get(id)</c> — no re-implementation of the 3-table join, and no <c>is BatchCopy...</c>
/// branch: the engine is read polymorphically through <see cref="IEtlPipelineTypedConfiguration"/>.
/// </remarks>
internal static class PipelineLineageProjection
{
    /// <summary>
    /// Projects one composed pipeline aggregate to a <see cref="PipelineLineageRecord"/>. A pipeline
    /// whose kind body or engine body is genuinely absent renders NODE-ONLY — Name/Id/ServiceOptionType
    /// set, linkage left null — with a Warning naming the gap. NO FALLBACKS: linkage is never fabricated.
    /// </summary>
    /// <param name="aggregate">The fully composed pipeline aggregate (header + kind body + engine body).</param>
    /// <param name="logger">Logger for verbose composition/linkage tracing.</param>
    public static PipelineLineageRecord From(PipelineConfiguration aggregate, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var record = new PipelineLineageRecord
        {
            Id = aggregate.Id,
            Name = aggregate.Name,
            ServiceOptionType = aggregate.ServiceOptionType ?? string.Empty
        };

        if (aggregate.Configuration is not EtlPipelineConfiguration kindBody ||
            kindBody.Configuration is not IEtlPipelineTypedConfiguration engine)
        {
            ApiEndpointLog.PipelineNodeOnlyNoBody(logger, aggregate.Name);
            return record;
        }

        ApiEndpointLog.PipelineAggregateComposed(logger, aggregate.Name, kindBody.ServiceOptionType ?? string.Empty);

        record.SourceDataSet = engine.SourceDataSet;
        record.DestinationDataSet = engine.DestinationDataSet;
        record.IsEnabled = engine.IsEnabled;
        record.SourceConnectionName = engine.SourceConnectionName;
        record.DestinationConnectionName = engine.DestinationConnectionName;

        ApiEndpointLog.PipelineLinkageExtracted(
            logger, aggregate.Name, engine.SourceDataSet, engine.DestinationDataSet,
            engine.SourceConnectionName, engine.DestinationConnectionName);

        return record;
    }
}
