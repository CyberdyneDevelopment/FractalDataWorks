using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// Makes the claim of a run in the operational database from what the scheduler works out for it (FDW-6939): the tables it writes, the Exclusive key of a run whose writes cannot be told, the cap of its
/// pipeline and the key its pipeline states. The same claim for a run carried out at once and for a queued run, so the two see each other.
/// </summary>
public static class PipelineRunClaimRequests
{
    private static readonly IReadOnlyDictionary<string, object?> NoParameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Makes the claim of one run.</summary>
    /// <param name="request">The run.</param>
    /// <param name="claim">What the scheduler works out for the run (<see cref="PipelineRunClaims.For"/>).</param>
    /// <param name="claimKeyTemplate">The pipeline's key template; null or empty when the pipeline states none.</param>
    /// <param name="executingHost">The host's application name, as the run's record states it.</param>
    /// <param name="executingSlot">The slot of the host's copy; null for a host that is not on slots.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The claim to make, or the coded failure.</returns>
    public static IGenericResult<PipelineRunClaimRequest> Of(
        PipelineExecutionRequest request, PipelineRunClaim claim, string? claimKeyTemplate, string executingHost, string? executingSlot, ILogger logger)
    {
        if (request.TenantId is not { } tenant)
            return GenericResult<PipelineRunClaimRequest>.Failure(PipelineRunClaimLog.RunHasNoTenant(logger, request.ExecutionId));

        var keys = new List<PipelineRunClaimKey>();
        foreach (var table in claim.WriteKeys)
        {
            var write = PipelineRunClaimKeys.WriteOf(table, logger);
            if (write.IsFailure || write.Value is null)
                return write.ToNewResult<PipelineRunClaimRequest>();

            keys.Add(write.Value);
        }

        if (claim.Alone)
            keys.Add(PipelineRunClaimKeys.Exclusive());

        if (claim.Cap is { } cap)
        {
            var capKey = PipelineRunClaimKeys.PipelineCap(request.PipelineId, cap.Limit, logger);
            if (capKey.IsFailure || capKey.Value is null)
                return capKey.ToNewResult<PipelineRunClaimRequest>();

            keys.Add(capKey.Value);
        }

        if (!string.IsNullOrEmpty(claimKeyTemplate))
        {
            var expanded = PipelineRunClaimKeyTemplate.Expand(claimKeyTemplate, request.Parameters is { } supplied ? supplied : NoParameters, logger);
            if (expanded.IsFailure || expanded.Value is null)
                return expanded.ToNewResult<PipelineRunClaimRequest>();

            var stated = PipelineRunClaimKeys.Stated(tenant, expanded.Value, logger);
            if (stated.IsFailure || stated.Value is null)
                return stated.ToNewResult<PipelineRunClaimRequest>();

            keys.Add(stated.Value);
        }

        return GenericResult<PipelineRunClaimRequest>.Success(new PipelineRunClaimRequest(tenant, request.ExecutionId, request.PipelineId, keys, executingHost, executingSlot));
    }
}
