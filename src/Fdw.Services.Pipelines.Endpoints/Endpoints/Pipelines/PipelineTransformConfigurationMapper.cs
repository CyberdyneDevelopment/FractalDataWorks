using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Maps <see cref="CreatePipelineTransformRequest"/> specs onto typed <see cref="PipelineTransformConfiguration"/>
/// aggregates for the create/update pipeline endpoints (FDW-556).
/// </summary>
/// <remarks>
/// Why: dispatches per-option mapping via <c>TransformTypes.ByName(spec.OperationType).MapSpecToConfiguration</c>
/// — never a switch/if-else on <see cref="ITransformOperationSpec.OperationType"/>. Each option validates
/// its own required parameters and fails loud (structured MessageLogging error, non-success result) when
/// they are absent, so a param-less combine op can never silently no-op at create/update time — mirroring
/// the runtime fail-loud already enforced by <c>TransformTypeBase.TransformBatch</c>. This is domain mapping
/// logic and therefore belongs in FDW (DI-ownership rule), not duplicated per application.
/// </remarks>
public static class PipelineTransformConfigurationMapper
{
    /// <summary>
    /// Maps every transform spec in <paramref name="specs"/> onto a typed <see cref="PipelineTransformConfiguration"/>,
    /// stopping at (and returning) the first failure.
    /// </summary>
    /// <param name="specs">The ordered transform-operation specs from the request.</param>
    /// <param name="logger">The logger used for the log-and-return MessageLogging failure.</param>
    /// <returns>Success with one configuration per spec, or the first mapping failure.</returns>
    public static IGenericResult<List<PipelineTransformConfiguration>> Map(
        IEnumerable<ITransformOperationSpec> specs,
        ILogger logger)
    {
        var results = new List<PipelineTransformConfiguration>();

        foreach (var spec in specs)
        {
            if (TransformTypes.ByName(spec.OperationType) is not TransformTypeBase option || option == TransformTypes.NotFound)
            {
                return GenericResult<List<PipelineTransformConfiguration>>.Failure(
                    EtlLog.UnknownTransformType(logger, spec.OperationType));
            }

            var config = new PipelineTransformConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = spec.Name,
                OperationType = spec.OperationType,
                ExecutionOrder = spec.ExecutionOrder,
                IsEnabled = true,
            };

            var mapResult = option.MapSpecToConfiguration(spec, config, logger);
            if (!mapResult.IsSuccess)
            {
                return mapResult.ToNewResult<List<PipelineTransformConfiguration>>();
            }

            results.Add(config);
        }

        return GenericResult<List<PipelineTransformConfiguration>>.Success(results);
    }
}
