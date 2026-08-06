using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Lineage;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Executes a calculation entity's ordered steps over the registered calculation operations.
/// </summary>
/// <remarks>
/// <para>
/// This is the mechanism that turns a configured <see cref="ICalculationEntity"/> into a value:
/// walk <see cref="ICalculationEntity.Steps"/> in <c>Ordinal</c> order, bind each step's operands
/// (an input alias, a prior step's output alias, or a literal) to the operation's declared
/// parameter names, invoke the operation, and publish the step's result under its output alias so
/// later steps can reference it.
/// </para>
/// <para>
/// Why the weak <see cref="IGenericConfiguration"/> element type: it matches
/// <see cref="ICalculationEntity.Steps"/> exactly, so a caller can pass the entity's steps straight
/// through. The implementation narrows each element to the concrete step configuration and fails
/// loud if an element is not one — it never skips an element it cannot interpret.
/// </para>
/// <para>
/// Every unresolvable condition — unknown operation, missing input alias, forward or unknown step
/// reference, absent literal, missing required parameter, duplicate output alias — is returned as a
/// failure. Nothing is defaulted, coerced, or skipped: a calculation that cannot be evaluated
/// exactly as configured stops rather than producing a number nobody can trace.
/// </para>
/// <para>
/// Execution always emits its per-step derivation to the supplied
/// <see cref="ICalculationTraceRecorder"/> — each operation, each operand's source and resolved
/// value, and every intermediate. The recorder is a required parameter rather than an optional one
/// so traceability is a property of the calculation rather than of how a caller happened to invoke
/// it: a figure that can be produced but not explained is unusable anywhere it has to be defended.
/// The binding only exists inside this loop, so a caller cannot reconstruct it afterwards without
/// re-running the calculation.
/// </para>
/// </remarks>
public interface ICalculationStepExecutor
{
    /// <summary>
    /// Executes the supplied steps in ordinal order, recording each step's derivation, and returns
    /// the value produced by the highest-ordinal step.
    /// </summary>
    /// <param name="steps">
    /// The entity's steps. Each element must be a concrete calculation step configuration.
    /// </param>
    /// <param name="inputs">The already-resolved calculation inputs, keyed by their input alias.</param>
    /// <param name="recorder">
    /// Receives each step's derivation as it executes. The caller owns it, so after a failure it
    /// still holds every step up to and including the one that failed.
    /// </param>
    /// <param name="cancellationToken">A token to cancel execution.</param>
    /// <returns>
    /// A result carrying the value produced by the highest-ordinal step, or a failure describing
    /// the first step that could not be evaluated. A failure carries no value; the partial
    /// derivation is read from <paramref name="recorder"/>.
    /// </returns>
    Task<IGenericResult<object?>> Execute(
        IReadOnlyList<IGenericConfiguration> steps,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationTraceRecorder recorder,
        CancellationToken cancellationToken = default);
}
