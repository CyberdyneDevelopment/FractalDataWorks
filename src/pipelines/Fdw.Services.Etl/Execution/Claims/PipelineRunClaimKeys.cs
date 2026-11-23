using System;
using System.Globalization;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// Builds the keys a pipeline run claims (FDW-6629). The one place a key's text is made, so the claim and the hold that looks a key up build it the same way.
/// </summary>
/// <remarks>
/// A stated key always begins with the tenant's id and a colon, so no template can leave the tenant out and two tenants never meet on one text. A write key is the connection, the path and the
/// container, joined by slashes; the database compares it without regard to case. A cap key names the pipeline. A key made from a part that is missing is a coded failure: nothing completes it.
/// </remarks>
public static class PipelineRunClaimKeys
{
    /// <summary>Builds the key of a table or file a run writes.</summary>
    /// <param name="connection">The connection that holds it.</param>
    /// <param name="path">The path in the data store.</param>
    /// <param name="container">The table or file.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The key, or the failure naming the part that is missing.</returns>
    public static IGenericResult<PipelineRunClaimKey> Write(string connection, string path, string container, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(connection))
            return GenericResult<PipelineRunClaimKey>.Failure(PipelineRunClaimLog.WriteKeyPartMissing(logger, "connection"));
        if (string.IsNullOrWhiteSpace(path))
            return GenericResult<PipelineRunClaimKey>.Failure(PipelineRunClaimLog.WriteKeyPartMissing(logger, "path"));
        if (string.IsNullOrWhiteSpace(container))
            return GenericResult<PipelineRunClaimKey>.Failure(PipelineRunClaimLog.WriteKeyPartMissing(logger, "container"));

        return GenericResult<PipelineRunClaimKey>.Success(new PipelineRunClaimKey(ClaimKeyTypes.ByName("WriteKey"), $"{connection}/{path}/{container}", null));
    }

    /// <summary>Builds the key of a table or file as the scheduler names it: the connection, the path and the container joined by slashes (FDW-6939).</summary>
    /// <param name="key">The key the scheduler works out for what the run writes.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The key, or the failure when it has no text.</returns>
    public static IGenericResult<PipelineRunClaimKey> WriteOf(string key, ILogger logger)
        => string.IsNullOrWhiteSpace(key)
            ? GenericResult<PipelineRunClaimKey>.Failure(PipelineRunClaimLog.WriteKeyPartMissing(logger, "key"))
            : GenericResult<PipelineRunClaimKey>.Success(new PipelineRunClaimKey(ClaimKeyTypes.ByName("WriteKey"), key, null));

    /// <summary>Builds the one key of a run whose writes cannot be told (FDW-6939); it conflicts with every other claim in the operational database.</summary>
    /// <returns>The key.</returns>
    public static PipelineRunClaimKey Exclusive()
        => new(ClaimKeyTypes.ByName("Exclusive"), "*", null);

    /// <summary>Builds a key the pipeline states, always beginning with the tenant's id.</summary>
    /// <param name="tenantId">The tenant the run acts for.</param>
    /// <param name="expandedKey">The pipeline's key template, expanded for this run.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The key, or the failure naming what is missing.</returns>
    public static IGenericResult<PipelineRunClaimKey> Stated(Guid tenantId, string expandedKey, ILogger logger)
    {
        if (tenantId == Guid.Empty)
            return GenericResult<PipelineRunClaimKey>.Failure(PipelineRunClaimLog.TenantMissing(logger));
        if (string.IsNullOrWhiteSpace(expandedKey))
            return GenericResult<PipelineRunClaimKey>.Failure(PipelineRunClaimLog.StatedKeyTextMissing(logger));

        return GenericResult<PipelineRunClaimKey>.Success(new PipelineRunClaimKey(ClaimKeyTypes.ByName("StatedKey"), $"{tenantId.ToString("D", CultureInfo.InvariantCulture)}:{expandedKey}", null));
    }

    /// <summary>Builds the cap key of a pipeline.</summary>
    /// <param name="pipelineId">The pipeline.</param>
    /// <param name="cap">How many of its runs may be claimed at once; at least 1.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The key, or the failure naming the cap.</returns>
    public static IGenericResult<PipelineRunClaimKey> PipelineCap(Guid pipelineId, int cap, ILogger logger)
        => cap < 1
            ? GenericResult<PipelineRunClaimKey>.Failure(PipelineRunClaimLog.CapNotPositive(logger, cap))
            : GenericResult<PipelineRunClaimKey>.Success(new PipelineRunClaimKey(ClaimKeyTypes.ByName("PipelineCap"), pipelineId.ToString("D", CultureInfo.InvariantCulture), cap));
}
