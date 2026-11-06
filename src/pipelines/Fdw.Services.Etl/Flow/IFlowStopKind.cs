using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>Why a level's flow was stopped, which decides how its records end (FDW-3880): one option of <see cref="FlowStopKinds"/>.</summary>
public interface IFlowStopKind : ITypeOption<int, IFlowStopKind>
{
    /// <summary>Ends the record of one stage of a level's flow.</summary>
    /// <param name="reporter">What writes the record's end.</param>
    /// <param name="record">The stage's record.</param>
    /// <param name="outcome">How the level's flow ended.</param>
    /// <param name="unfinished">Whether a component of the stage did not succeed in a flow that failed; a stage that finished before the failure or the stop is complete.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the record is ended, or left open for the host to close.</returns>
    Task EndRecord(IFlowReporter reporter, Guid record, IGenericResult outcome, bool unfinished, CancellationToken cancellationToken = default);
}
