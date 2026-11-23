using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using System;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// The pieces a run gives the person it is for (FDW-6140): each text a step writes, each step's start and end, each step skipped. They are kept in order with a sequence that only grows, so a client that dropped asks
/// again from the last sequence it received and nothing is lost or repeated; only the person (and the host's audience) reads them.
/// </summary>
public interface IRunStream
{
    /// <summary>Writes a piece for the person the run acts as.</summary>
    /// <param name="identity">Who the run is; a run that acts as no person has nobody to follow it and writes nothing.</param>
    /// <param name="kind">One of <see cref="RunStreamKinds"/>.</param>
    /// <param name="step">The step the piece is about, or <see langword="null"/>.</param>
    /// <param name="text">The text, or how the step ended, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the failure that says the piece was not written.</returns>
    Task<IGenericResult> Write(FlowRunIdentity identity, string kind, string? step, string? text, CancellationToken cancellationToken = default);

    /// <summary>Reads the pieces of a run after a sequence.</summary>
    /// <param name="tenantId">The caller's tenant.</param>
    /// <param name="person">The person who asks.</param>
    /// <param name="claimRootId">The first run of the chain, whose pieces are read.</param>
    /// <param name="afterSequence">The last sequence the client has; the pieces after it are answered.</param>
    /// <param name="take">The most pieces to answer, at most the setting <c>RunStreamPageSize</c> allows.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The pieces in order (none when there are none after the sequence), or the failure: the pieces are not the caller's, or could not be read.</returns>
    Task<IGenericResult<IReadOnlyList<RunStreamPiece>>> ReadAfter(
        Guid tenantId, Guid person, Guid claimRootId, long afterSequence, int? take, CancellationToken cancellationToken = default);
}
