using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Lineage;

/// <summary>
/// Collects the per-step derivation of a single calculation execution as it happens.
/// </summary>
/// <remarks>
/// <para>
/// Why the trace is written to a caller-owned recorder rather than returned in the result: a
/// failure result carries no value by framework invariant — every
/// <see cref="Fdw.Results.GenericResult{T}"/> failure factory sets the value to
/// <see langword="default"/> — so a trace that rode the return value would exist on exactly the
/// path where it matters least. A calculation that fails at step 7 of 12 has already produced six
/// defensible steps and one that shows precisely where it stopped, and that partial record is the
/// most valuable thing the execution can hand back.
/// </para>
/// <para>
/// Because the caller holds the recorder before execution starts, it still holds everything
/// recorded up to the failing step after the executor returns a failure. Success and failure are
/// read the same way, from the same object.
/// </para>
/// <para>
/// The recorder is per-execution state and must never be shared between concurrent executions —
/// the executor itself is registered as a singleton and deliberately holds no trace state of its
/// own. Implementations are not required to be thread-safe.
/// </para>
/// <para>
/// Passing one recorder to a nested or recursive execution is <b>not supported</b>. Each execution
/// numbers its steps by its own configuration, so two executions writing to one recorder produce a
/// flat list whose ordinals belong to different scopes — well-formed to look at and impossible to
/// read correctly. When nesting is introduced, a nested execution will record into its own child
/// recorder whose steps hang off the parent step, which is additive to this contract and leaves
/// the meaning of <see cref="Steps"/> unchanged.
/// </para>
/// </remarks>
public interface ICalculationTraceRecorder
{
    /// <summary>
    /// Gets the steps recorded so far, in the order they executed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This list's order is the authoritative execution sequence.
    /// <see cref="CalculationStepTrace.Ordinal"/> is the configured position and carries no
    /// recorder-wide guarantee — see its documentation before keying anything on it.
    /// </para>
    /// <para>
    /// After a failed execution the final entry is the step that failed, carrying its
    /// <see cref="CalculationStepTrace.Failure"/> and whatever operands it had bound before it
    /// stopped.
    /// </para>
    /// </remarks>
    IReadOnlyList<CalculationStepTrace> Steps { get; }

    /// <summary>
    /// Records one executed step.
    /// </summary>
    /// <param name="step">The step's derivation record, complete or failed.</param>
    void Record(CalculationStepTrace step);
}
