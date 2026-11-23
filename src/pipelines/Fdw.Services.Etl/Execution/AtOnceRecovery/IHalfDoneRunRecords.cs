using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>The records of a run the recovery of half-done runs reads and closes (FDW-6986): the run's row, and its execution items.</summary>
internal interface IHalfDoneRunRecords
{
    /// <summary>Reads a run's record.</summary>
    /// <param name="tenantId">The tenant the run belongs to.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The row, or a null value when the run has none; a failure when it cannot be read.</returns>
    Task<IGenericResult<HalfDoneRunRow?>> Read(Guid tenantId, Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>Closes a half-done run: its row and its execution items end Interrupted, with the reason on them.</summary>
    /// <param name="tenantId">The tenant the run belongs to.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="reason">Why the run ended (the text is the same whichever write comes last).</param>
    /// <param name="cancellationToken">Cancels the close.</param>
    /// <returns>Success, or the failure that left the run open.</returns>
    Task<IGenericResult> Close(Guid tenantId, Guid executionId, string reason, CancellationToken cancellationToken = default);
}
