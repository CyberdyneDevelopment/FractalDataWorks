using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// The pipelines a tenant and a person are offered (FDW-6135): the data a start of a pipeline a variable named is judged against. An offer names a pipeline for the whole tenant, for one person,
/// or for everyone who acts in a role; no offer means not offered. The offers are the pipeline's own configuration, read in the caller's tenant.
/// </summary>
public interface IPipelineOffers
{
    /// <summary>Says whether a pipeline is offered to a run.</summary>
    /// <param name="pipelineId">The pipeline.</param>
    /// <param name="personId">The person the run acts for, or <see langword="null"/> when it acts for none.</param>
    /// <param name="roles">The roles the run holds.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Whether an offer of the whole tenant, of the person or of one of the roles stands; or the failure of the read.</returns>
    Task<IGenericResult<bool>> IsOffered(Guid pipelineId, Guid? personId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default);
}
