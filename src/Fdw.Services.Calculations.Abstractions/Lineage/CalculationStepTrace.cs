using System.Collections.Generic;
using Fdw.Messages;

namespace Fdw.Services.Calculations.Abstractions.Lineage;

/// <summary>
/// Records one calculation step: which operation ran, what each operand resolved to, and what the
/// step published — or, when the step could not complete, how far it got and why it stopped.
/// </summary>
/// <remarks>
/// Read in ordinal order, a sequence of these is a complete derivation of the final value — every
/// intermediate is named by its output alias, and every operand says where it came from, so a
/// figure can be traced back through each step to the input records that produced it.
/// </remarks>
public sealed class CalculationStepTrace
{
    /// <summary>
    /// Gets the step's ordinal exactly as configured. This is provenance — what the configuration
    /// declared — not a sequence number the recorder assigned.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scope is one execution. Within a single execution the recorded ordinals are non-decreasing,
    /// because the executor sorts the steps by this value before running them. They are
    /// <b>not</b> guaranteed unique, contiguous, or one-based: nothing rejects two steps sharing an
    /// ordinal, so duplicates are already possible today.
    /// </para>
    /// <para>
    /// Read <see cref="ICalculationTraceRecorder.Steps"/> in list order for execution sequence, and
    /// never use this as a key across a recorder. A consumer that treats it as globally monotonic
    /// is relying on something the contract does not promise, and would break silently the first
    /// time a calculation is configured with repeated ordinals or a nested execution is recorded.
    /// </para>
    /// </remarks>
    public int Ordinal { get; init; }

    /// <summary>Gets the configured step name.</summary>
    public string StepName { get; init; } = string.Empty;

    /// <summary>Gets the calculation operation the step invoked.</summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>Gets the alias this step published its result under.</summary>
    public string OutputAlias { get; init; } = string.Empty;

    /// <summary>Gets the resolution record for each operand the step bound, in configured order.</summary>
    /// <remarks>
    /// On a failed step this holds the operands bound before the failure, which is what identifies
    /// the operand the step stopped on: it is the one after the last entry here.
    /// </remarks>
    public IReadOnlyList<CalculationOperandTrace> Operands { get; init; } = [];

    /// <summary>
    /// Gets the value the operation produced for this step;
    /// <see langword="null"/> when <see cref="Failure"/> is set and the step published nothing.
    /// </summary>
    public CalculationTraceValue? OutputValue { get; init; }

    /// <summary>
    /// Gets a value indicating whether this step ran to completion and published its output.
    /// </summary>
    /// <remarks>
    /// Why an explicit flag rather than inferring the outcome from <see cref="OutputValue"/> or
    /// <see cref="Failure"/>: an operation may legitimately produce a null value, and one that
    /// fails with a result code instead of a message leaves <see cref="Failure"/> unset — so
    /// neither absence is a reliable signal. This is the one field that says which happened.
    /// </remarks>
    public bool Completed { get; init; }

    /// <summary>
    /// Gets the message describing why this step could not complete; <see langword="null"/> on a
    /// step that completed, and on a failure that carried a result code rather than a message.
    /// </summary>
    /// <remarks>
    /// The returned result is the authority on why an execution stopped — it carries the code
    /// chain and root cause as well. This field duplicates the message onto the step so a trace
    /// that is persisted on its own still says what went wrong, not merely where.
    /// </remarks>
    public IGenericMessage? Failure { get; init; }
}
