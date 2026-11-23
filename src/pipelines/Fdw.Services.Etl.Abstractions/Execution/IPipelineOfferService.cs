using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Lists, adds and removes the offers of a pipeline (FDW-6135), in the caller's tenant. Every refusal is a coded message, as the hold service's are: a pipeline that does not exist, an offer
/// that names both a person and a role, one that already stands, one that does not exist.
/// </summary>
public interface IPipelineOfferService
{
    /// <summary>Lists the offers of a pipeline.</summary>
    /// <param name="pipelineId">The pipeline.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The offers; or the refusal when the pipeline does not exist.</returns>
    Task<IGenericResult<IReadOnlyList<PipelineOffer>>> List(Guid pipelineId, CancellationToken cancellationToken = default);

    /// <summary>Offers a pipeline to the whole tenant, to a person or to a role.</summary>
    /// <param name="pipelineId">The pipeline.</param>
    /// <param name="personId">The person, or <see langword="null"/>.</param>
    /// <param name="roleName">The role, or <see langword="null"/>; at most one of the person and the role.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The offer made; or the refusal.</returns>
    Task<IGenericResult<PipelineOffer>> Add(Guid pipelineId, Guid? personId, string? roleName, CancellationToken cancellationToken = default);

    /// <summary>Removes one offer.</summary>
    /// <param name="pipelineId">The pipeline the offer is of.</param>
    /// <param name="offerId">The offer.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the refusal when the offer does not exist.</returns>
    Task<IGenericResult> Remove(Guid pipelineId, Guid offerId, CancellationToken cancellationToken = default);

    /// <summary>Removes every offer of a pipeline, as its deletion does.</summary>
    /// <param name="pipelineId">The pipeline.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the failure of a removal.</returns>
    Task<IGenericResult> RemoveAll(Guid pipelineId, CancellationToken cancellationToken = default);
}
