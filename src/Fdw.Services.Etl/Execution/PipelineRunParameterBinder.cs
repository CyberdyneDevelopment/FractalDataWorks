using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The run parameters of one run (FDW-1109): the typed values the trigger or schedule supplied, the stored watermark offered
/// as <c>LastSuccessfulRun</c>, and the watermark row the run will move forward when it succeeds.
/// </summary>
public sealed class PipelineRunParameterBinder
{
    private PipelineRunParameterBinder(IReadOnlyDictionary<string, object?> values, EtlPipelineWatermarkRecord? watermark)
    {
        Values = values;
        Watermark = watermark;
    }

    /// <summary>Gets the typed values by name (case-insensitive), including <c>LastSuccessfulRun</c> when there is one.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; }

    /// <summary>Gets the stored watermark row, or <see langword="null"/> when the pipeline has no successful run on record or the run states no tenant.</summary>
    public EtlPipelineWatermarkRecord? Watermark { get; }

    /// <summary>Binds the parameters of a run.</summary>
    /// <param name="gateway">The data gateway.</param>
    /// <param name="operationalStore">The operational store the host names.</param>
    /// <param name="configuration">The pipeline.</param>
    /// <param name="options">The execution options, carrying the supplied parameters and the tenant.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The bound parameters, or a failure saying what is wrong with the supplied ones or the watermark read.</returns>
    public static async Task<IGenericResult<PipelineRunParameterBinder>> Bind(
        IDataGateway gateway,
        string? operationalStore,
        BatchCopyPipelineConfiguration configuration,
        PipelineExecutionOptions options,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var verified = PipelineRunParameterValues.Verify(configuration.RunParameters, options.Parameters, logger);
        if (verified.IsFailure)
            return verified.ToNewResult<PipelineRunParameterBinder>();

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (options.Parameters is not null)
        {
            foreach (var pair in options.Parameters)
                values[pair.Key] = pair.Value;
        }

        EtlPipelineWatermarkRecord? watermark = null;
        if (options.TenantId is { } tenantId)
        {
            var read = await PipelineWatermarks.Read(
                gateway, operationalStore, configuration.Id, tenantId, configuration.Name, logger, cancellationToken).ConfigureAwait(false);
            if (read.IsFailure)
                return read.ToNewResult<PipelineRunParameterBinder>();
            watermark = read.Value;
            if (watermark is not null && !values.ContainsKey(PipelineRunParameterValues.LastSuccessfulRun))
                values[PipelineRunParameterValues.LastSuccessfulRun] = watermark.LastSuccessfulRun;
        }
        else
        {
            PipelineRunLog.WatermarkNotTracked(logger, configuration.Name);
        }

        PipelineRunLog.RunParametersBound(logger, configuration.Name, values.Count, watermark is not null);
        return GenericResult<PipelineRunParameterBinder>.Success(new PipelineRunParameterBinder(values, watermark));
    }
}
