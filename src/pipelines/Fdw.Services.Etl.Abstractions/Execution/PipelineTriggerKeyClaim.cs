using System;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>What became of a scheduled fire's key (FDW-4317): claimed for a new run, or already held by the run an earlier call started.</summary>
/// <param name="IsNew">Whether the key was claimed now; false when an earlier call with the key already made a run.</param>
/// <param name="ExecutionId">The run the earlier call started; none when the key was claimed now.</param>
public sealed record PipelineTriggerKeyClaim(bool IsNew, Guid? ExecutionId)
{
    /// <summary>Gets the claim for a key nobody had.</summary>
    public static PipelineTriggerKeyClaim Claimed { get; } = new(true, null);

    /// <summary>Gets the claim for a key an earlier call used.</summary>
    /// <param name="executionId">The run the earlier call started.</param>
    /// <returns>The claim.</returns>
    public static PipelineTriggerKeyClaim Existing(Guid executionId) => new(false, executionId);
}
